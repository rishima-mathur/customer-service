using CustomerService.API.Data;
using CustomerService.API.Models;

namespace CustomerService.API.Services;

public class CustomerService
{
    private readonly CustomerRepository _repository;

    public CustomerService(CustomerRepository repository)
    {
        _repository = repository;
    }

    public Task<IEnumerable<Customer>> GetAllAsync() => _repository.GetAllAsync();
    public Task<Customer?> GetByIdAsync(int id) => _repository.GetByIdAsync(id);
    public Task<int> CreateAsync(Customer customer) => _repository.CreateAsync(customer);
    public Task<int> UpdateAsync(Customer customer) => _repository.UpdateAsync(customer);
    public Task<int> DeleteAsync(int id) => _repository.DeleteAsync(id);
}
