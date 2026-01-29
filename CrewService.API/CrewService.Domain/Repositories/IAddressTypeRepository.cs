using CrewService.Domain.Models.ContactTypes;

namespace CrewService.Domain.Repositories;

public interface IAddressTypeRepository
{
    Task<List<AddressType>> GetAllAsync(long clientCtrlNbr);
    Task<List<AddressType>> GetAllAsync(long clientCtrlNbr, int pageNumber, int pageSize);
    Task<AddressType?> GetByCtrlNbrAsync(long ctrlNbr);
    void Add(AddressType type);
    void Update(AddressType type);
    void Remove(AddressType type);
}