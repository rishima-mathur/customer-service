using CustomerService.API.Models;
using Dapper;
using Microsoft.Data.SqlClient;

namespace CustomerService.API.Data;

public class CustomerRepository
{
    private readonly string _connectionString;

    public CustomerRepository(IConfiguration config)
    {
        _connectionString = config.GetConnectionString("CustomerDB");
    }

    public async Task<IEnumerable<Customer>> GetAllAsync()
    {
        using var connection = new SqlConnection(_connectionString);
        return await connection.QueryAsync<Customer>("SELECT * FROM Customers ORDER BY CreatedAt DESC");
    }

    public async Task<Customer?> GetByIdAsync(int id) {
        using var connection = new SqlConnection(_connectionString);
        return await connection.QueryFirstOrDefaultAsync<Customer>(
            "SELECT * FROM Customers WHERE CustomerId = @id", new { id });
    }

    public async Task<int> CreateAsync(Customer customer)
    {
        var sql = @"INSERT INTO Customers (Name, Email, Phone, CreatedAt)
                        VALUES (@Name, @Email, @Phone, @CreatedAt);
                        SELECT CAST(SCOPE_IDENTITY() as int);";
        using var connection = new SqlConnection(_connectionString);
        return await connection.ExecuteScalarAsync<int>(sql, customer);
    }

    public async Task<int> UpdateAsync(Customer customer)
    {
        var sql = @"UPDATE Customers
                        SET Name = @Name, Email = @Email, Phone = @Phone
                        WHERE CustomerId = @CustomerId";
        using var connection = new SqlConnection(_connectionString);
        return await connection.ExecuteAsync(sql, customer);
    }

    public async Task<int> DeleteAsync(int id)
    {
        using var connection = new SqlConnection(_connectionString);
        return await connection.ExecuteAsync("DELETE FROM Customers WHERE CustomerId = @id", new { id });
    }
}
