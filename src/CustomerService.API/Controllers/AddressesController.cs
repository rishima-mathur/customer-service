using CustomerService.API.Models;
using CustomerService.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace CustomerService.API.Controllers;

public class AddressesController : ControllerBase
{
    private readonly AddressService _service;
    private readonly ILogger<AddressesController> _logger;

    public AddressesController(AddressService service, ILogger<AddressesController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpGet("customer/{customerId}")]
    public async Task<IActionResult> GetByCustomer(int customerId)
    {
        var addresses = await _service.GetByCustomerIdAsync(customerId);
        return Ok(addresses);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var address = await _service.GetByIdAsync(id);
        return address == null ? NotFound() : Ok(address);
    }

    [HttpPost]
    public async Task<IActionResult> Create(Address address)
    {
        address.CreatedAt = DateTime.UtcNow;
        var id = await _service.CreateAsync(address);
        return CreatedAtAction(nameof(GetById), new { id }, address);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, Address address)
    {
        address.AddressId = id;
        var rows = await _service.UpdateAsync(address);
        return rows > 0 ? NoContent() : NotFound();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var rows = await _service.DeleteAsync(id);
        return rows > 0 ? NoContent() : NotFound();
    }
}
