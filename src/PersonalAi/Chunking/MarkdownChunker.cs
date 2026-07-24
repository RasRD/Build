namespace PersonalAi.Chunking;

static class MarkdownChunker
{
    public static IEnumerable<string> Chunk(string text, int maxChars = 500)
    {
        var paragraphs = text.Split("\n\n", StringSplitOptions.RemoveEmptyEntries);
        var buffer = new System.Text.StringBuilder();

        foreach (var paragraph in paragraphs)
        {
            if (buffer.Length + paragraph.Length > maxChars && buffer.Length > 0)
            {
                yield return buffer.ToString().Trim();
                buffer.Clear();
            }
            buffer.Append(paragraph).Append("\n\n");
        }

        if (buffer.Length > 0)
            yield return buffer.ToString().Trim();
    }
}
