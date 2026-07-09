using CrewService.Domain.Interfaces;
using CrewService.Domain.Modules.RailroadInfo;
using CrewService.Domain.ValueObjects;

namespace CrewService.Application.RailroadInfo;

public sealed class RailroadInfoService(IOrchestrationUnitOfWorkFactory uowFactory)
{
    public async Task<RailroadInformation> CreateAsync(
        long workAreaGroupCtrlNbr, string informationType, string subject, string body,
        CancellationToken ct = default)
    {
        var info = RailroadInformation.Create(workAreaGroupCtrlNbr, informationType, subject, body);
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        uow.RailroadInformation.Add(info);
        await uow.CommitAsync(ct);
        return info;
    }

    public async Task<RailroadInformation> GetAsync(ControlNumber ctrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        return await uow.RailroadInformation.GetByCtrlNbrAsync(ctrlNbr, ct)
            ?? throw new KeyNotFoundException($"Railroad information {ctrlNbr} not found.");
    }

    public async Task<IReadOnlyList<RailroadInformation>> GetByWorkAreaAsync(
        ControlNumber workAreaGroupCtrlNbr, bool publishedOnly, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        return publishedOnly
            ? await uow.RailroadInformation.GetPublishedByWorkAreaAsync(workAreaGroupCtrlNbr, ct)
            : await uow.RailroadInformation.GetByWorkAreaAsync(workAreaGroupCtrlNbr, ct);
    }

    public async Task<RailroadInformation> PublishAsync(ControlNumber ctrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var info = await uow.RailroadInformation.GetByCtrlNbrAsync(ctrlNbr, ct)
            ?? throw new KeyNotFoundException($"Railroad information {ctrlNbr} not found.");
        info.Publish();
        uow.RailroadInformation.Update(info);
        await uow.CommitAsync(ct);
        return info;
    }

    public async Task<RailroadInformation> CloseAsync(ControlNumber ctrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var info = await uow.RailroadInformation.GetByCtrlNbrAsync(ctrlNbr, ct)
            ?? throw new KeyNotFoundException($"Railroad information {ctrlNbr} not found.");
        info.Close();
        uow.RailroadInformation.Update(info);
        await uow.CommitAsync(ct);
        return info;
    }

    public async Task<RailroadInformation> CancelAsync(ControlNumber ctrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var info = await uow.RailroadInformation.GetByCtrlNbrAsync(ctrlNbr, ct)
            ?? throw new KeyNotFoundException($"Railroad information {ctrlNbr} not found.");
        info.Cancel();
        uow.RailroadInformation.Update(info);
        await uow.CommitAsync(ct);
        return info;
    }

    public async Task<RailroadInformationReadReceipt> AcknowledgeReadAsync(
        ControlNumber informationCtrlNbr, ControlNumber employeeCtrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        var existing = await uow.RailroadInformationReadReceipts
            .GetByInformationAndEmployeeAsync(informationCtrlNbr, employeeCtrlNbr, ct);
        if (existing is not null)
            return existing;

        var receipt = RailroadInformationReadReceipt.Create(informationCtrlNbr, employeeCtrlNbr);
        uow.RailroadInformationReadReceipts.Add(receipt);
        await uow.CommitAsync(ct);
        return receipt;
    }

    public async Task<IReadOnlyList<RailroadInformationReadReceipt>> GetReadReceiptsAsync(
        ControlNumber informationCtrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync(cancellationToken: ct);
        return await uow.RailroadInformationReadReceipts.GetByInformationAsync(informationCtrlNbr, ct);
    }
}
