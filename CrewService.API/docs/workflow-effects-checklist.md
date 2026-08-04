# Workflow Effect Implementation Checklist

Use this checklist for every new or modified workflow effect.

## Required architecture rules

1. DB effects must implement `IDatabaseWorkflowEffect`.
2. DB effects must use the active `IOrchestrationUnitOfWork` from `WorkflowEffectExecutionContext`.
3. DB effects must not inject `IOrchestrationUnitOfWorkFactory`.
4. DB effects must not call `IOrchestrationUnitOfWorkFactory.CreateAsync(...)`.
5. External side effects (SMTP, HTTP/webhooks, queues) must be emitted as post-commit work items.
6. Post-commit work items must be dispatched through `IWorkflowPostCommitDispatcher`.
7. Effect failures must flow back to workflow runtime so execution history transitions from `Running` to a terminal status.

## PR review checks

- [ ] Effect uses shared workflow runner (`IWorkflowEffectRunner`) path.
- [ ] DB writes occur in the active workflow UoW only.
- [ ] No nested UoW creation in effect handler.
- [ ] External I/O is post-commit only.
- [ ] Architecture tests pass (`WorkflowEffectArchitectureTests`).
- [ ] Workflow runtime history still records `Succeeded`/`Failed`/`Skipped` terminal status.
