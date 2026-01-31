using CrewService.Domain.Models.ContactTypes;

namespace CrewService.Domain.Interfaces.Repositories;

public interface IEmailAddressTypeRepository
{
    Task<List<EmailAddressType>> GetAllAsync(long clientCtrlNbr);
    Task<List<EmailAddressType>> GetAllAsync(long clientCtrlNbr, int pageNumber, int pageSize);
    Task<EmailAddressType?> GetByCtrlNbrAsync(long ctrlNbr);
    void Add(EmailAddressType type);
    void Update(EmailAddressType type);
    void Remove(EmailAddressType type);
}