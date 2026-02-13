using CrewService.Domain.Models.ContactTypes;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.Interfaces.Repositories;

public interface IAddressTypeRepository : IRepository<AddressType>
{
    Task<List<AddressType>> GetByClientCtrlNbrAsync(ControlNumber clientCtrlNbr);
    Task<List<AddressType>> GetByClientCtrlNbrAsync(ControlNumber clientCtrlNbr, int pageNumber, int pageSize);
}