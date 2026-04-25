using CrewService.Domain.Interfaces;
using CrewService.Domain.Modules.RailroadInfo;
using CrewService.Domain.ValueObjects;

namespace CrewService.Application.RailroadInfo;

public sealed class RailroadInfoService(IOrchestrationUnitOfWorkFactory uowFactory)
{
    public async Task<RailroadInformation> CreateAsync(
        long workAreaGroupCtrlNbr, string informationType, string subject, string body)
    {
        var info = RailroadInformation.Create(workAreaGroupCtrlNbr, informationType, subject, body);
        await using var uow = await uowFactory.CreateAsync();
        uow.RailroadInformation.Add(info);
        await uow.CommitAsync();
        return info;
    }

    public async Task<RailroadInformation> GetAsync(ControlNumber ctrlNbr)
    {
        await using var uow = await uowFactory.CreateAsync();
        return await uow.RailroadInformation.GetByCtrlNbrAsync(ctrlNbr)
            ?? throw new KeyNotFoundException($"Railroad information {ctrlNbr} not found.");
    }

    public async Task<IReadOnlyList<RailroadInformation>> GetByWorkAreaAsync(
        ControlNumber workAreaGroupCtrlNbr, bool publishedOnly, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync();
        return publishedOnly
            ? await uow.RailroadInformation.GetPublishedByWorkAreaAsync(workAreaGroupCtrlNbr, ct)
            : await uow.RailroadInformation.GetByWorkAreaAsync(workAreaGroupCtrlNbr, ct);
    }

    public async Task<RailroadInformation> PublishAsync(ControlNumber ctrlNbr)
    {
        await using var uow = await uowFactory.CreateAsync();
        var info = await uow.RailroadInformation.GetByCtrlNbrAsync(ctrlNbr)
            ?? throw new KeyNotFoundException($"Railroad information {ctrlNbr} not found.");
        info.Publish();
        uow.RailroadInformation.Update(info);
        await uow.CommitAsync();
        return info;
    }

    public async Task<RailroadInformation> CloseAsync(ControlNumber ctrlNbr)
    {
        await using var uow = await uowFactory.CreateAsync();
        var info = await uow.RailroadInformation.GetByCtrlNbrAsync(ctrlNbr)
            ?? throw new KeyNotFoundException($"Railroad information {ctrlNbr} not found.");
        info.Close();
        uow.RailroadInformation.Update(info);
        await uow.CommitAsync();
        return info;
    }

    public async Task<RailroadInformation> CancelAsync(ControlNumber ctrlNbr)
    {
        await using var uow = await uowFactory.CreateAsync();
        var info = await uow.RailroadInformation.GetByCtrlNbrAsync(ctrlNbr)
            ?? throw new KeyNotFoundException($"Railroad information {ctrlNbr} not found.");
        info.Cancel();
        uow.RailroadInformation.Update(info);
        await uow.CommitAsync();
        return info;
    }

    public async Task<RailroadInformationReadReceipt> AcknowledgeReadAsync(
        ControlNumber informationCtrlNbr, ControlNumber employeeCtrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync();
        var existing = await uow.RailroadInformationReadReceipts
            .GetByInformationAndEmployeeAsync(informationCtrlNbr, employeeCtrlNbr, ct);
        if (existing is not null)
            return existing;

        var receipt = RailroadInformationReadReceipt.Create(informationCtrlNbr, employeeCtrlNbr);
        uow.RailroadInformationReadReceipts.Add(receipt);
        await uow.CommitAsync();
        return receipt;
    }

    public async Task<IReadOnlyList<RailroadInformationReadReceipt>> GetReadReceiptsAsync(
        ControlNumber informationCtrlNbr, CancellationToken ct = default)
    {
        await using var uow = await uowFactory.CreateAsync();
        return await uow.RailroadInformationReadReceipts.GetByInformationAsync(informationCtrlNbr, ct);
    }
}
