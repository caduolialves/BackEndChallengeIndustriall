using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using PurchaseOrderChallenge.Data;
using PurchaseOrderChallenge.Repository;
using PurchaseOrderChallenge.Repository.Interfaces;
using PurchaseOrderChallenge.Service;
using PurchaseOrderChallenge.Service.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// Injeção de dependência para o serviço de pedidos de compra
builder.Services.AddDbContext<PurchaseOrderDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IPurchaseOrderService, PurchaseOrderService>();
builder.Services.AddScoped<IPurchaseRequestRepository, PurchaseRequestRepository>();
builder.Services.AddScoped<IApprovalStepsRepository, ApprovalStepsRepository>();
builder.Services.AddScoped<IPurchaseRequestHistoryRepository, PurchaseRequestHistoryRepository>();

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

    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "v1");
    });
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
