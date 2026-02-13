using CrewService.Domain.Models.ContactTypes;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.Interfaces.Repositories;

public interface IPhoneNumberTypeRepository : IRepository<PhoneNumberType>
{
    Task<List<PhoneNumberType>> GetByClientCtrlNbrAsync(ControlNumber clientCtrlNbr);
    Task<List<PhoneNumberType>> GetByClientCtrlNbrAsync(ControlNumber clientCtrlNbr, int pageNumber, int pageSize);
}