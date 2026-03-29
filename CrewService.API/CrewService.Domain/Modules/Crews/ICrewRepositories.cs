using CrewService.Domain.Interfaces.Repositories;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.Modules.Crews;

public interface ICrewRepository : IRepository<Crew>
{
    Task<List<Crew>> GetByHomeGroupAsync(ControlNumber homeGroupCtrlNbr);
    Task<List<Crew>> GetByTypeAsync(string crewType);
}

public interface ICrewPositionRepository : IRepository<CrewPosition>
{
    Task<List<CrewPosition>> GetByCrewAsync(ControlNumber crewCtrlNbr);
}

public interface ICrewIncumbencyRepository : IRepository<CrewIncumbency>
{
    Task<List<CrewIncumbency>> GetByCrewPositionAsync(ControlNumber crewPositionCtrlNbr);
    Task<List<CrewIncumbency>> GetActiveByEmployeeAsync(ControlNumber employeeCtrlNbr, DateTime asOfUtc);
}

public interface ICrewAttachmentTemplateRepository : IRepository<CrewAttachmentTemplate>
{
    Task<List<CrewAttachmentTemplate>> GetByAssignmentGroupAsync(ControlNumber assignmentGroupCtrlNbr);
}

public interface ICrewAttachmentInstanceRepository : IRepository<CrewAttachmentInstance>
{
    Task<List<CrewAttachmentInstance>> GetByWorkInstanceAsync(ControlNumber workInstanceCtrlNbr);
}

public interface IReliefCoverageRuleRepository : IRepository<ReliefCoverageRule>
{
    Task<List<ReliefCoverageRule>> GetByReliefCrewAsync(ControlNumber reliefCrewCtrlNbr);
}
