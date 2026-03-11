using Microsoft.EntityFrameworkCore;
using NetRefer.Api.Middleware;
using NetRefer.Application.Interfaces;
using NetRefer.Infrastructure.Data;
using NetRefer.Infrastructure.Repositories;
using NetRefer.Infrastructure.Services;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/app.log")
    .CreateLogger();

builder.Host.UseSerilog();

builder.Services.AddControllers();

builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("Default")));

builder.Services.AddSwaggerGen(options =>
{
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);

    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});

builder.Services.AddHttpClient<IPaymentService, PaymentService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IOrderService, OrderService>();

builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseMiddleware<CorrelationMiddleware>();
app.UseMiddleware<ExceptionMiddleware>();


app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();

app.MapHealthChecks("/health");

app.Run();