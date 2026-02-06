using CrewService.Domain.Models.ContactTypes;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.Interfaces.Repositories;

public interface IPhoneNumberTypeRepository
{
    Task<List<PhoneNumberType>> GetAllAsync();
    Task<List<PhoneNumberType>> GetAllAsync(long clientCtrlNbr);
    Task<List<PhoneNumberType>> GetAllAsync(long clientCtrlNbr, int pageNumber, int pageSize);
    Task<PhoneNumberType?> GetByIdAsync(ControlNumber ctrlNbr);
    Task<PhoneNumberType?> GetByCtrlNbrAsync(long ctrlNbr);
    Task<List<PhoneNumberType>> GetByClientCtrlNbrAsync(long clientCtrlNbr);

    Task AddAsync(PhoneNumberType phoneNumberType);
    Task UpdateAsync(PhoneNumberType phoneNumberType);
    Task DeleteAsync(ControlNumber ctrlNbr);

    void Add(PhoneNumberType phoneNumberType);
    void Update(PhoneNumberType phoneNumberType);
    void Remove(PhoneNumberType phoneNumberType);
}