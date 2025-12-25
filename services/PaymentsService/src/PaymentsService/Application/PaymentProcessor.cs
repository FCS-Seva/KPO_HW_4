using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PaymentsService.Abstractions;
using PaymentsService.Domain;
using PaymentsService.Infrastructure.Persistence;
using Shared.Contracts.Messaging;

namespace PaymentsService.Application;

public sealed class PaymentProcessor : IPaymentProcessor
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly PaymentsDbContext _db;
    private readonly IInboxStore _inbox;
    private readonly IAccountsRepository _accounts;
    private readonly IOutboxStore _outbox;

    public PaymentProcessor(PaymentsDbContext db, IInboxStore inbox, IAccountsRepository accounts, IOutboxStore outbox)
    {
        _db = db;
        _inbox = inbox;
        _accounts = accounts;
        _outbox = outbox;
    }

    public async Task ProcessAsync(PayOrderCommand command, CancellationToken ct)
    {
        var now = DateTime.UtcNow;

        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        var inboxMsg = await _inbox.GetAsync(command.MessageId, ct);
        if (inboxMsg is not null && inboxMsg.ProcessedAtUtc is not null)
        {
            await tx.CommitAsync(ct);
            return;
        }

        if (inboxMsg is null)
        {
            await _inbox.AddIfMissingAsync(new InboxMessage
            {
                Id = command.MessageId,
                ReceivedAtUtc = now,
                Status = InboxMessageStatus.Received
            }, ct);
        }

        var existingTx = await _db.PaymentTransactions.FirstOrDefaultAsync(x => x.OrderId == command.OrderId, ct);
        if (existingTx is not null && existingTx.Status != PaymentTransactionStatus.Started)
        {
            await EnqueueResultAsync(existingTx, ct);
            await _inbox.MarkProcessedAsync(command.MessageId, now, ct);
            await tx.CommitAsync(ct);
            return;
        }

        var paymentTx = existingTx;
        if (paymentTx is null)
        {
            paymentTx = new PaymentTransaction
            {
                Id = Guid.NewGuid(),
                OrderId = command.OrderId,
                UserId = command.UserId,
                Amount = command.Amount,
                Status = PaymentTransactionStatus.Started,
                CreatedAtUtc = now
            };

            _db.PaymentTransactions.Add(paymentTx);
            try
            {
                await _db.SaveChangesAsync(ct);
            }
            catch (DbUpdateException)
            {
                paymentTx = await _db.PaymentTransactions.FirstOrDefaultAsync(x => x.OrderId == command.OrderId, ct);
            }
        }

        if (paymentTx is null)
        {
            await tx.CommitAsync(ct);
            return;
        }

        if (paymentTx.Status == PaymentTransactionStatus.Started)
        {
            var debited = await _accounts.TryDebitAsync(command.UserId, command.Amount, ct);

            if (debited)
            {
                paymentTx.Status = PaymentTransactionStatus.Succeeded;
            }
            else
            {
                paymentTx.Status = PaymentTransactionStatus.Failed;
                paymentTx.Reason = "Insufficient funds or account not found.";
            }

            _db.PaymentTransactions.Update(paymentTx);
            await _db.SaveChangesAsync(ct);
        }

        await EnqueueResultAsync(paymentTx, ct);
        await _inbox.MarkProcessedAsync(command.MessageId, now, ct);

        await tx.CommitAsync(ct);
    }

    private async Task EnqueueResultAsync(PaymentTransaction paymentTx, CancellationToken ct)
    {
        var now = DateTime.UtcNow;

        var evt = new PaymentResultEvent(
            MessageId: Guid.NewGuid(),
            OrderId: paymentTx.OrderId,
            Status: paymentTx.Status == PaymentTransactionStatus.Succeeded ? PaymentResultStatus.Succeeded : PaymentResultStatus.Failed,
            Reason: paymentTx.Status == PaymentTransactionStatus.Failed ? paymentTx.Reason : null,
            CreatedAtUtc: now
        );

        var outbox = new OutboxMessage
        {
            Id = evt.MessageId,
            AggregateId = paymentTx.OrderId,
            Type = nameof(PaymentResultEvent),
            PayloadJson = JsonSerializer.Serialize(evt, JsonOptions),
            CreatedAtUtc = now,
            Attempts = 0
        };

        await _outbox.AddAsync(outbox, ct);
    }
}
