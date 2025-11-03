using CustomerService.API.Models;
using Microsoft.AspNetCore.Mvc;

namespace CustomerService.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class CustomersController : ControllerBase
{
    private readonly Services.CustomerService _service;
    private readonly ILogger<CustomersController> _logger;

    public CustomersController(Services.CustomerService service, ILogger<CustomersController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var customers = await _service.GetAllAsync();
        return Ok(customers);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var customer = await _service.GetByIdAsync(id);
        if (customer == null)
            return NotFound(new { code = 404, message = "Customer not found" });
        return Ok(customer);
    }

    [HttpPost]
    public async Task<IActionResult> Create(Customer customer)
    {
        customer.CreatedAt = DateTime.UtcNow;
        var id = await _service.CreateAsync(customer);
        return CreatedAtAction(nameof(GetById), new { id }, customer);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, Customer customer)
    {
        customer.CustomerId = id;
        var rows = await _service.UpdateAsync(customer);
        return rows > 0 ? NoContent() : NotFound();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var rows = await _service.DeleteAsync(id);
        return rows > 0 ? NoContent() : NotFound();
    }
}
