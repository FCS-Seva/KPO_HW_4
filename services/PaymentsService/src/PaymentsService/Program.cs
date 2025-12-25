using Microsoft.EntityFrameworkCore;
using PaymentsService.Abstractions;
using PaymentsService.Api.Middleware;
using PaymentsService.Api.UserContext;
using PaymentsService.Application;
using PaymentsService.Infrastructure.Messaging;
using PaymentsService.Infrastructure.Persistence;
using PaymentsService.Infrastructure.Workers;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.Configure<MessagingOptions>(builder.Configuration.GetSection("Messaging"));
builder.Services.Configure<OutboxOptions>(builder.Configuration.GetSection("Outbox"));

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IUserContextAccessor, UserContextAccessor>();

builder.Services.AddDbContext<PaymentsDbContext>(opts =>
    opts.UseNpgsql(builder.Configuration.GetConnectionString("PaymentsDb")));

builder.Services.AddScoped<IAccountsRepository, AccountsRepository>();
builder.Services.AddScoped<IInboxStore, InboxStore>();
builder.Services.AddScoped<IOutboxStore, OutboxStore>();
builder.Services.AddScoped<IPaymentsApplicationService, PaymentsApplicationService>();
builder.Services.AddScoped<IPaymentProcessor, PaymentProcessor>();

builder.Services.AddSingleton<IRabbitMqConnectionProvider, RabbitMqConnectionProvider>();
builder.Services.AddSingleton<IMessagePublisher, RabbitMqPublisher>();

builder.Services.AddHostedService<OutboxPublisherHostedService>();
builder.Services.AddHostedService<PayOrderConsumerHostedService>();

var app = builder.Build();

await EnsureDbCreatedAsync(app.Services);

static async Task EnsureDbCreatedAsync(IServiceProvider services)
{
    using var scope = services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<PaymentsDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("PaymentsDbInit");

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
