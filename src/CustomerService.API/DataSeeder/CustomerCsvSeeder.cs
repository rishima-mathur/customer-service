using CsvHelper;
using CustomerService.API.Models;
using Dapper;
using Microsoft.Data.SqlClient;
using System.Globalization;

namespace CustomerService.API.DataSeeder;

public class CustomerCsvSeeder : IHostedService
{
    private readonly ILogger<CustomerCsvSeeder> _logger;
    private readonly IConfiguration _config;
    private readonly IWebHostEnvironment _env;
    private readonly string _connString;

    public CustomerCsvSeeder(ILogger<CustomerCsvSeeder> logger, IConfiguration config, IWebHostEnvironment env)
    {
        _logger = logger;
        _config = config;
        _env = env;
        _connString = config.GetConnectionString("CustomerDB")!;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        const int maxRetries = 10;
        const int delaySeconds = 5;

        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                using var conn = new SqlConnection(_connString);
                await conn.OpenAsync(cancellationToken);

                _logger.LogInformation("✅ Connected to SQL Server successfully.");
                await EnsureTablesExistAsync(conn);
                await SeedDatabaseAsync(conn);
                return;
            }
            catch (SqlException ex)
            {
                _logger.LogWarning(ex, "⏳ SQL connection failed (attempt {attempt}/{maxRetries}). Retrying in {delaySeconds}s...", attempt, maxRetries, delaySeconds);
                await Task.Delay(TimeSpan.FromSeconds(delaySeconds), cancellationToken);
            }
        }

        _logger.LogError("❌ Could not connect to SQL Server after {maxRetries} attempts.", maxRetries);
    }

    private async Task EnsureTablesExistAsync(SqlConnection conn)
    {
        _logger.LogInformation("🔍 Checking and creating tables if missing...");

        string createCustomersTable = @"
            IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Customers' AND xtype='U')
            CREATE TABLE Customers (
                CustomerId INT IDENTITY(1,1) PRIMARY KEY,
                Name NVARCHAR(100) NOT NULL,
                Email NVARCHAR(100),
                Phone NVARCHAR(20),
                CreatedAt DATETIME DEFAULT GETDATE()
            );
        ";

        string createAddressesTable = @"
            IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Addresses' AND xtype='U')
            CREATE TABLE Addresses (
                AddressId INT IDENTITY(1,1) PRIMARY KEY,
                CustomerId INT NOT NULL FOREIGN KEY REFERENCES Customers(CustomerId),
                Line1 NVARCHAR(255),
                Area NVARCHAR(100),
                City NVARCHAR(100),
                Pincode NVARCHAR(10),
                CreatedAt DATETIME DEFAULT GETDATE()
            );
        ";

        await conn.ExecuteAsync(createCustomersTable);
        await conn.ExecuteAsync(createAddressesTable);

        _logger.LogInformation("✅ Tables verified/created successfully.");
    }

    private async Task SeedDatabaseAsync(SqlConnection conn)
    {
        try
        {
            var count = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Customers");
            if (count > 0)
            {
                _logger.LogInformation("Customer table already seeded. Skipping.");
                return;
            }

            var csvPathCustomers = Path.Combine(_env.ContentRootPath, "seed", "customers food.csv");
            var csvPathAddresses = Path.Combine(_env.ContentRootPath, "seed", "addresses.csv");

            if (!File.Exists(csvPathCustomers))
            {
                _logger.LogWarning("⚠️ No customers.csv found at {path}", csvPathCustomers);
                return;
            }

            // Read Customers CSV
            using var reader = new StreamReader(csvPathCustomers);
            using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
            var customers = csv.GetRecords<Customer>().ToList();

            foreach (var c in customers)
            {
                var sql = @"INSERT INTO Customers (Name, Email, Phone, CreatedAt)
                            VALUES (@Name, @Email, @Phone, @CreatedAt);";
                await conn.ExecuteAsync(sql, c);
            }

            // Read Addresses CSV
            if (File.Exists(csvPathAddresses))
            {
                using var addrReader = new StreamReader(csvPathAddresses);
                using var csvAddr = new CsvReader(addrReader, CultureInfo.InvariantCulture);
                var addresses = csvAddr.GetRecords<Address>().ToList();

                foreach (var a in addresses)
                {
                    var sql = @"INSERT INTO Addresses (CustomerId, Line1, Area, City, Pincode, CreatedAt)
                                VALUES (@CustomerId, @Line1, @Area, @City, @Pincode, @CreatedAt)";
                    await conn.ExecuteAsync(sql, a);
                }
            }

            _logger.LogInformation("✅ Seeded {count} customers successfully!", customers.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ CSV seeding failed");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
