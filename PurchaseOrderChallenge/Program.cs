using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using PurchaseOrderChallenge.Data;
using PurchaseOrderChallenge.Repository;
using PurchaseOrderChallenge.Service;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// Injeção de dependência para o serviço de pedidos de compra
builder.Services.AddDbContext<PurchaseOrderDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<PurchaseOrderService>();
builder.Services.AddScoped<PurchaseRequestRepository>();
builder.Services.AddScoped<ApprovalStepsRepository>();

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        // Evitar ciclos de referência durante a serialização JSON
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    });
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
