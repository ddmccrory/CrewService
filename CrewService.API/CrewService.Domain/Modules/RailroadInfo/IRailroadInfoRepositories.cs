using CrewService.Domain.Interfaces.Repositories;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.Modules.RailroadInfo;

public interface IRailroadInformationRepository : IRepository<RailroadInformation>
{
    Task<IReadOnlyList<RailroadInformation>> GetByWorkAreaAsync(
        ControlNumber workAreaGroupCtrlNbr, CancellationToken ct = default);

    Task<IReadOnlyList<RailroadInformation>> GetPublishedByWorkAreaAsync(
        ControlNumber workAreaGroupCtrlNbr, CancellationToken ct = default);
}

public interface IRailroadInformationReadReceiptRepository : IRepository<RailroadInformationReadReceipt>
{
    Task<IReadOnlyList<RailroadInformationReadReceipt>> GetByInformationAsync(
        ControlNumber informationCtrlNbr, CancellationToken ct = default);

    Task<RailroadInformationReadReceipt?> GetByInformationAndEmployeeAsync(
        ControlNumber informationCtrlNbr, ControlNumber employeeCtrlNbr, CancellationToken ct = default);
}
