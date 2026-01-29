using CrewService.Domain.Models.Parents;

namespace CrewService.Domain.Repositories;

public interface IParentRepository
{
    Task<List<Parent>> GetAllAsync();

    Task<Parent?> GetByCtrlNbrAsync(long parentCtrlNbr);

    void Add(Parent parent);

    void Update(Parent parent);

    void Remove(Parent parent);
}
