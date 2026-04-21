using System.Text.Json.Serialization;
using PurchaseOrderChallenge.Service;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// Injeção de dependência para o serviço de pedidos de compra
builder.Services.AddSingleton<PurchaseOrderService>();

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
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
