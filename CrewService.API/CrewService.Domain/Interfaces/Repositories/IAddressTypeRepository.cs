using CrewService.Domain.Models.ContactTypes;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.Interfaces.Repositories;

public interface IAddressTypeRepository
{
    // Query methods
    Task<List<AddressType>> GetAllAsync();
    Task<List<AddressType>> GetAllAsync(long clientCtrlNbr);
    Task<List<AddressType>> GetAllAsync(long clientCtrlNbr, int pageNumber, int pageSize);
    Task<AddressType?> GetByIdAsync(ControlNumber ctrlNbr);
    Task<AddressType?> GetByCtrlNbrAsync(long ctrlNbr);
    Task<List<AddressType>> GetByClientCtrlNbrAsync(long clientCtrlNbr);

    // Async methods (for gRPC services - auto-save)
    Task AddAsync(AddressType addressType);
    Task UpdateAsync(AddressType addressType);
    Task DeleteAsync(ControlNumber ctrlNbr);

    // Sync methods (for UoW - no auto-save)
    void Add(AddressType addressType);
    void Update(AddressType addressType);
    void Remove(AddressType addressType);
}