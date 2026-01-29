using CrewService.Domain.Models.ContactTypes;

namespace CrewService.Domain.Repositories;

public interface IPhoneNumberTypeRepository
{
    Task<List<PhoneNumberType>> GetAllAsync(long clientCtrlNbr);
    Task<List<PhoneNumberType>> GetAllAsync(long clientCtrlNbr, int pageNumber, int pageSize);
    Task<PhoneNumberType?> GetByCtrlNbrAsync(long ctrlNbr);
    void Add(PhoneNumberType type);
    void Update(PhoneNumberType type);
    void Remove(PhoneNumberType type);
}