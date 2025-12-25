namespace OrdersService.Infrastructure.Messaging;

public sealed class MessagingOptions
{
    public string HostName { get; set; } = "rabbitmq";
    public string UserName { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    public string VirtualHost { get; set; } = "/";
    public string CommandsExchange { get; set; } = "orders.payments";
    public string CommandsRoutingKey { get; set; } = "pay-order";
    public string CommandsQueue { get; set; } = "payments.pay-order";

    public string ResultsExchange { get; set; } = "payments.orders";
    public string ResultsRoutingKey { get; set; } = "payment-result";
    public string ResultsQueue { get; set; } = "orders.payment-result";
}
