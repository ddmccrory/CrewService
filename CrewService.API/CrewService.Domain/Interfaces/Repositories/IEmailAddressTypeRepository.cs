using CrewService.Domain.Models.ContactTypes;
using CrewService.Domain.ValueObjects;

namespace CrewService.Domain.Interfaces.Repositories;

public interface IEmailAddressTypeRepository
{
    Task<List<EmailAddressType>> GetAllAsync();
    Task<List<EmailAddressType>> GetAllAsync(long clientCtrlNbr);
    Task<List<EmailAddressType>> GetAllAsync(long clientCtrlNbr, int pageNumber, int pageSize);
    Task<EmailAddressType?> GetByIdAsync(ControlNumber ctrlNbr);
    Task<EmailAddressType?> GetByCtrlNbrAsync(long ctrlNbr);
    Task<List<EmailAddressType>> GetByClientCtrlNbrAsync(long clientCtrlNbr);

    Task AddAsync(EmailAddressType emailAddressType);
    Task UpdateAsync(EmailAddressType emailAddressType);
    Task DeleteAsync(ControlNumber ctrlNbr);

    void Add(EmailAddressType emailAddressType);
    void Update(EmailAddressType emailAddressType);
    void Remove(EmailAddressType emailAddressType);
}