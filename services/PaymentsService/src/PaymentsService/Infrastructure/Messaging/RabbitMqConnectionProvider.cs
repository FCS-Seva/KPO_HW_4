using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace PaymentsService.Infrastructure.Messaging;

public sealed class RabbitMqConnectionProvider : IRabbitMqConnectionProvider, IDisposable
{
    private readonly ConnectionFactory _factory;
    private IConnection? _connection;
    private readonly object _lock = new();

    public RabbitMqConnectionProvider(IOptions<MessagingOptions> options)
    {
        var o = options.Value;

        _factory = new ConnectionFactory
        {
            HostName = o.HostName,
            UserName = o.UserName,
            Password = o.Password,
            VirtualHost = o.VirtualHost,
            DispatchConsumersAsync = true,
            AutomaticRecoveryEnabled = true
        };
    }

    public IConnection GetConnection()
    {
        if (_connection is not null && _connection.IsOpen) return _connection;

        lock (_lock)
        {
            if (_connection is not null && _connection.IsOpen) return _connection;
            _connection = _factory.CreateConnection();
            return _connection;
        }
    }

    public void Dispose()
    {
        _connection?.Dispose();
    }
}
