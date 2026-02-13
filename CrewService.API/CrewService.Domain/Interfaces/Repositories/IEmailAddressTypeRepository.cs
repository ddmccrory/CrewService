using CrewService.Domain.Models.ContactTypes;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.Interfaces.Repositories;

public interface IEmailAddressTypeRepository : IRepository<EmailAddressType>
{
    Task<List<EmailAddressType>> GetByClientCtrlNbrAsync(ControlNumber clientCtrlNbr);
    Task<List<EmailAddressType>> GetByClientCtrlNbrAsync(ControlNumber clientCtrlNbr, int pageNumber, int pageSize);
}