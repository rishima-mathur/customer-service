using CustomerService.API.Models;
using Dapper;
using Microsoft.Data.SqlClient;

namespace CustomerService.API.Data;

public class AddressRepository
{
    private readonly string _connectionString;

    public AddressRepository(IConfiguration config)
    {
        _connectionString = config.GetConnectionString("CustomerDB");
    }

    public async Task<IEnumerable<Address>> GetByCustomerIdAsync(int customerId)
    {
        using var conn = new SqlConnection(_connectionString);
        return await conn.QueryAsync<Address>(
            "SELECT * FROM Addresses WHERE CustomerId = @customerId ORDER BY CreatedAt DESC",
            new { customerId });
    }

    public async Task<Address?> GetByIdAsync(int id)
    {
        using var conn = new SqlConnection(_connectionString);
        return await conn.QueryFirstOrDefaultAsync<Address>(
            "SELECT * FROM Addresses WHERE AddressId = @id", new { id });
    }

    public async Task<int> CreateAsync(Address address)
    {
        var sql = @"INSERT INTO Addresses (CustomerId, Line1, Area, City, Pincode, CreatedAt)
                        VALUES (@CustomerId, @Line1, @Area, @City, @Pincode, @CreatedAt);
                        SELECT CAST(SCOPE_IDENTITY() as int);";
        using var conn = new SqlConnection(_connectionString);
        return await conn.ExecuteScalarAsync<int>(sql, address);
    }

    public async Task<int> UpdateAsync(Address address)
    {
        var sql = @"UPDATE Addresses
                        SET Line1=@Line1, Area=@Area, City=@City, Pincode=@Pincode
                        WHERE AddressId=@AddressId";
        using var conn = new SqlConnection(_connectionString);
        return await conn.ExecuteAsync(sql, address);
    }

    public async Task<int> DeleteAsync(int id)
    {
        using var conn = new SqlConnection(_connectionString);
        return await conn.ExecuteAsync("DELETE FROM Addresses WHERE AddressId=@id", new { id });
    }
}
