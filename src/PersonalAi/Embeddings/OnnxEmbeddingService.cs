using System.Text;
using System.Text.Json;
using FastBertTokenizer;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

namespace PersonalAi.Embeddings;

/// <summary>
/// Runs distiluse-base-multilingual-cased-v2 locally: DistilBERT (ONNX) -> attention-mask-weighted
/// mean pooling -> the model's trained Dense(768,512)+Tanh projection (weights read directly from
/// the sentence-transformers repo's 2_Dense/model.safetensors, since the pre-converted ONNX graph
/// only contains the base transformer). This reproduces the official 512-dim embedding space.
/// </summary>
sealed class OnnxEmbeddingService : IEmbeddingService, IDisposable
{
    const int MaxTokens = 256;

    readonly InferenceSession _session;
    readonly BertTokenizer _tokenizer;
    readonly string _inputIdsName;
    readonly string _attentionMaskName;
    readonly string? _tokenTypeIdsName;
    readonly string _outputName;
    readonly float[] _denseWeight; // flat, row-major [outFeatures, inFeatures]
    readonly float[] _denseBias;   // [outFeatures]
    readonly int _denseInFeatures;
    readonly int _denseOutFeatures;

    public OnnxEmbeddingService(string modelDirectory)
    {
        _session = new InferenceSession(Path.Combine(modelDirectory, "model.onnx"));

        _tokenizer = new BertTokenizer();
        _tokenizer.LoadVocabularyAsync(Path.Combine(modelDirectory, "vocab.txt"), convertInputToLowercase: false)
            .GetAwaiter().GetResult();

        (_inputIdsName, _attentionMaskName, _tokenTypeIdsName) = ResolveInputNames(_session);
        _outputName = ResolveOutputName(_session);

        Console.WriteLine(
            $"  ONNX inputs: {string.Join(", ", _session.InputMetadata.Select(kv => $"{kv.Key}{FormatShape(kv.Value.Dimensions)}"))}");
        Console.WriteLine(
            $"  ONNX output used: {_outputName}{FormatShape(_session.OutputMetadata[_outputName].Dimensions)}");

        (_denseWeight, _denseBias, _denseInFeatures, _denseOutFeatures) = LoadDenseLayer(
            Path.Combine(modelDirectory, "2_Dense", "model.safetensors"),
            Path.Combine(modelDirectory, "2_Dense", "config.json"));

        Console.WriteLine($"  Dense projection: {_denseInFeatures} -> {_denseOutFeatures} (+ Tanh)");
    }

    public Task<float[]> EmbedAsync(string text)
    {
        var (inputIds, attentionMask, tokenTypeIds) = _tokenizer.Encode(text, MaxTokens);
        var seqLen = inputIds.Length;

        var inputIdsTensor = new DenseTensor<long>(new[] { 1, seqLen });
        var attentionMaskTensor = new DenseTensor<long>(new[] { 1, seqLen });
        inputIds.Span.CopyTo(inputIdsTensor.Buffer.Span);
        attentionMask.Span.CopyTo(attentionMaskTensor.Buffer.Span);

        var inputs = new List<NamedOnnxValue>
        {
            NamedOnnxValue.CreateFromTensor(_inputIdsName, inputIdsTensor),
            NamedOnnxValue.CreateFromTensor(_attentionMaskName, attentionMaskTensor),
        };

        if (_tokenTypeIdsName is not null)
        {
            var tokenTypeTensor = new DenseTensor<long>(new[] { 1, seqLen });
            tokenTypeIds.Span.CopyTo(tokenTypeTensor.Buffer.Span);
            inputs.Add(NamedOnnxValue.CreateFromTensor(_tokenTypeIdsName, tokenTypeTensor));
        }

        using var results = _session.Run(inputs);
        var hiddenStates = results.First(r => r.Name == _outputName).AsTensor<float>(); // [1, seqLen, hiddenSize]
        var hiddenSize = hiddenStates.Dimensions[2];

        var pooled = new float[hiddenSize];
        var maskSum = 0f;
        var mask = attentionMask.Span;
        for (var t = 0; t < seqLen; t++)
        {
            var m = mask[t];
            if (m == 0)
                continue;
            maskSum += m;
            for (var h = 0; h < hiddenSize; h++)
                pooled[h] += hiddenStates[0, t, h] * m;
        }
        if (maskSum > 0)
            for (var h = 0; h < hiddenSize; h++)
                pooled[h] /= maskSum;

        var projected = new float[_denseOutFeatures];
        for (var o = 0; o < _denseOutFeatures; o++)
        {
            var sum = _denseBias[o];
            var rowOffset = o * _denseInFeatures;
            for (var i = 0; i < _denseInFeatures; i++)
                sum += _denseWeight[rowOffset + i] * pooled[i];
            projected[o] = MathF.Tanh(sum);
        }

        Normalize(projected);
        return Task.FromResult(projected);
    }

    public void Dispose() => _session.Dispose();

    static (string InputIds, string AttentionMask, string? TokenTypeIds) ResolveInputNames(InferenceSession session)
    {
        var names = session.InputMetadata.Keys.ToList();
        var inputIds = names.FirstOrDefault(n => n.Contains("input_ids", StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException($"Could not find 'input_ids' among ONNX inputs: {string.Join(", ", names)}");
        var attentionMask = names.FirstOrDefault(n => n.Contains("attention_mask", StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException($"Could not find 'attention_mask' among ONNX inputs: {string.Join(", ", names)}");
        var tokenTypeIds = names.FirstOrDefault(n => n.Contains("token_type_ids", StringComparison.OrdinalIgnoreCase));
        return (inputIds, attentionMask, tokenTypeIds);
    }

    static string ResolveOutputName(InferenceSession session)
    {
        var names = session.OutputMetadata.Keys.ToList();
        return names.FirstOrDefault(n => n.Contains("last_hidden_state", StringComparison.OrdinalIgnoreCase))
            ?? names.First();
    }

    static string FormatShape(int[] dimensions) => $"[{string.Join(",", dimensions)}]";

    static (float[] Weight, float[] Bias, int InFeatures, int OutFeatures) LoadDenseLayer(
        string safetensorsPath, string configPath)
    {
        using var config = JsonDocument.Parse(File.ReadAllText(configPath));
        var inFeatures = config.RootElement.GetProperty("in_features").GetInt32();
        var outFeatures = config.RootElement.GetProperty("out_features").GetInt32();
        var activation = config.RootElement.GetProperty("activation_function").GetString();
        if (activation is null || !activation.Contains("Tanh", StringComparison.OrdinalIgnoreCase))
            throw new NotSupportedException($"Expected a Tanh activation on the Dense layer, found '{activation}'.");

        var bytes = File.ReadAllBytes(safetensorsPath);
        var headerLen = BitConverter.ToInt64(bytes, 0);
        var header = JsonDocument.Parse(Encoding.UTF8.GetString(bytes, 8, (int)headerLen)).RootElement;
        var dataStart = 8 + headerLen;

        float[] ReadTensor(string name)
        {
            var meta = header.GetProperty(name);
            var dtype = meta.GetProperty("dtype").GetString();
            if (dtype != "F32")
                throw new NotSupportedException($"Expected F32 tensor '{name}', got '{dtype}'.");
            var offsets = meta.GetProperty("data_offsets");
            var start = offsets[0].GetInt64();
            var end = offsets[1].GetInt64();
            var result = new float[(end - start) / sizeof(float)];
            Buffer.BlockCopy(bytes, (int)(dataStart + start), result, 0, (int)(end - start));
            return result;
        }

        var weight = ReadTensor("linear.weight"); // [outFeatures, inFeatures], row-major
        var bias = ReadTensor("linear.bias");      // [outFeatures]
        return (weight, bias, inFeatures, outFeatures);
    }

    static void Normalize(float[] vector)
    {
        var normSquared = 0f;
        foreach (var value in vector)
            normSquared += value * value;

        if (normSquared == 0f)
            return;

        var norm = MathF.Sqrt(normSquared);
        for (var i = 0; i < vector.Length; i++)
            vector[i] /= norm;
    }
}
