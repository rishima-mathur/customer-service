using CustomerService.API.Data;
using CustomerService.API.DataSeeder;
using Dapper;
using Microsoft.Data.SqlClient;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddSingleton<CustomerRepository>();
builder.Services.AddSingleton<AddressRepository>();
builder.Services.AddScoped<CustomerService.API.Services.CustomerService>();
builder.Services.AddScoped<CustomerService.API.Services.AddressService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
//builder.Services.AddOpenApi();
builder.Services.AddHostedService<CustomerCsvSeeder>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    //app.MapOpenApi();
}

using (var scope = app.Services.CreateScope())
{
    var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
    var connectionString = configuration.GetConnectionString("CustomerDB");

    using var conn = new SqlConnection(connectionString.Replace("CustomerDB", "master"));
    conn.Open();
    await conn.ExecuteAsync("IF DB_ID('CustomerDB') IS NULL CREATE DATABASE CustomerDB");
}


app.UseHttpsRedirection();
app.MapControllers();
app.Run();
