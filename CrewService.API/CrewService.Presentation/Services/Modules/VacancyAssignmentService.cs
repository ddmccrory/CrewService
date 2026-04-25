using CrewService.Application.VacancyAssignment;
using CrewService.Domain.ValueObjects;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;

namespace CrewService.Presentation.Services.Modules;

public class VacancyAssignmentService(IServiceProvider serviceProvider)
    : VacancyAssignmentSrvc.VacancyAssignmentSrvcBase
{
    public override async Task<VacancyResolutionRunResponse> TriggerResolution(
        TriggerResolutionRequest request, ServerCallContext context)
    {
        var engine = serviceProvider.GetRequiredService<VacancyResolutionEngine>();
        var run = await engine.ExecuteAsync(
            ControlNumber.Create(request.WorkAreaGroupCtrlNbr),
            ControlNumber.Create(request.ShiftInstanceCtrlNbr),
            ControlNumber.Create(request.CraftCtrlNbr),
            context.CancellationToken);

        return MapRun(run);
    }

    public override Task<GetResolutionRunsResponse> GetResolutionRuns(
        GetResolutionRunsRequest request, ServerCallContext context)
    {
        return Task.FromResult(new GetResolutionRunsResponse());
    }

    private static VacancyResolutionRunResponse MapRun(Domain.Modules.Dispatching.VacancyResolutionRun run)
    {
        var resp = new VacancyResolutionRunResponse
        {
            CtrlNbr = run.CtrlNbr.Value,
            Status = run.Status,
            SlotsEvaluated = run.SlotsEvaluated,
            SlotsFilled = run.SlotsFilled,
            StartedAt = Timestamp.FromDateTime(DateTime.SpecifyKind(run.StartedAtUtc, DateTimeKind.Utc)),
        };
        if (run.CompletedAtUtc.HasValue)
            resp.CompletedAt = Timestamp.FromDateTime(DateTime.SpecifyKind(run.CompletedAtUtc.Value, DateTimeKind.Utc));
        return resp;
    }
}
