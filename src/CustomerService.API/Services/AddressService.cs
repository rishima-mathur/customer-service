using CustomerService.API.Data;
using CustomerService.API.Models;

namespace CustomerService.API.Services;

public class AddressService
{
    private readonly AddressRepository _repo;

    public AddressService(AddressRepository repo)
    {
        _repo = repo;
    }

    public Task<IEnumerable<Address>> GetByCustomerIdAsync(int customerId) => _repo.GetByCustomerIdAsync(customerId);
    public Task<Address?> GetByIdAsync(int id) => _repo.GetByIdAsync(id);
    public Task<int> CreateAsync(Address address) => _repo.CreateAsync(address);
    public Task<int> UpdateAsync(Address address) => _repo.UpdateAsync(address);
    public Task<int> DeleteAsync(int id) => _repo.DeleteAsync(id);
}
