using Microsoft.EntityFrameworkCore;
using OrdersService.Abstractions;
using OrdersService.Api.Middleware;
using OrdersService.Api.UserContext;
using OrdersService.Application;
using OrdersService.Infrastructure.Messaging;
using OrdersService.Infrastructure.Persistence;
using OrdersService.Infrastructure.Workers;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.Configure<MessagingOptions>(builder.Configuration.GetSection("Messaging"));
builder.Services.Configure<OutboxOptions>(builder.Configuration.GetSection("Outbox"));

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IUserContextAccessor, UserContextAccessor>();

builder.Services.AddDbContext<OrdersDbContext>(opts =>
    opts.UseNpgsql(builder.Configuration.GetConnectionString("OrdersDb")));

builder.Services.AddScoped<IOrdersRepository, OrdersRepository>();
builder.Services.AddScoped<IOrderCreationStore, OrderCreationStore>();
builder.Services.AddScoped<IOutboxStore, OutboxStore>();
builder.Services.AddScoped<IOrdersApplicationService, OrdersApplicationService>();

builder.Services.AddSingleton<IRabbitMqConnectionProvider, RabbitMqConnectionProvider>();
builder.Services.AddSingleton<IMessagePublisher, RabbitMqPublisher>();

builder.Services.AddHostedService<OutboxPublisherHostedService>();
builder.Services.AddHostedService<PaymentResultConsumerHostedService>();

var app = builder.Build();

await EnsureDbCreatedAsync(app.Services);

static async Task EnsureDbCreatedAsync(IServiceProvider services)
{
    using var scope = services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("OrdersDbInit");

    for (var attempt = 1; attempt <= 30; attempt++)
    {
        try
        {
            await db.Database.EnsureCreatedAsync();
            return;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Database is not ready. Attempt {Attempt}/30", attempt);
            await Task.Delay(1000);
        }
    }

    await db.Database.EnsureCreatedAsync();
}
app.UseSwagger();
app.UseSwaggerUI();

app.UseMiddleware<UserIdMiddleware>();

app.MapControllers();

app.Run();
