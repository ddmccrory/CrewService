using CrewService.Domain.Models.Employees;

namespace CrewService.Domain.Interfaces.Repositories;

public interface IAddressRepository
{
    Task<List<Address>> GetAllByEmployeeAsync(long employeeCtrlNbr);
    Task<List<Address>> GetAllByEmployeeAsync(long employeeCtrlNbr, int pageNumber, int pageSize);
    Task<Address?> GetByCtrlNbrAsync(long ctrlNbr);
    void Add(Address address);
    void Update(Address address);
    void Remove(Address address);
}