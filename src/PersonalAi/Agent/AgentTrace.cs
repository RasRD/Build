namespace PersonalAi.Agent;

sealed record AgentTraceStep(int Step, string Kind, string Detail);

sealed class AgentTrace
{
    readonly List<AgentTraceStep> _steps = [];

    public IReadOnlyList<AgentTraceStep> Steps => _steps;

    public void Add(string kind, string detail)
    {
        var step = new AgentTraceStep(_steps.Count + 1, kind, detail);
        _steps.Add(step);

        Console.WriteLine($"[{step.Step}] {step.Kind} -> {step.Detail}");
    }
}
