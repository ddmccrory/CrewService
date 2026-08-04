namespace CrewService.Application.Workflows.Effects;

public sealed class WorkflowEffectHandlerFactory(IEnumerable<IDatabaseWorkflowEffect> databaseEffects) : IWorkflowEffectHandlerFactory
{
    private readonly Dictionary<string, IDatabaseWorkflowEffect> _effectsByCode = databaseEffects
        .ToDictionary(e => e.EffectTypeCode, StringComparer.Ordinal);

    public IDatabaseWorkflowEffect Resolve(string effectTypeCode)
    {
        if (!_effectsByCode.TryGetValue(effectTypeCode, out var handler))
            throw new InvalidOperationException($"No workflow DB effect handler registered for '{effectTypeCode}'.");

        return handler;
    }
}
