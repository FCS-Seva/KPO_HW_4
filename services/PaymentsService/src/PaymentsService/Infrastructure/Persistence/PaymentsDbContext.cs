using Microsoft.EntityFrameworkCore;
using PaymentsService.Domain;

namespace PaymentsService.Infrastructure.Persistence;

public sealed class PaymentsDbContext : DbContext
{
    public PaymentsDbContext(DbContextOptions<PaymentsDbContext> options) : base(options) { }

    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<InboxMessage> InboxMessages => Set<InboxMessage>();
    public DbSet<PaymentTransaction> PaymentTransactions => Set<PaymentTransaction>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Account>(b =>
        {
            b.ToTable("accounts");
            b.HasKey(x => x.UserId);
            b.Property(x => x.UserId).HasColumnName("user_id");
            b.Property(x => x.Balance).HasColumnName("balance").HasColumnType("numeric(18,2)");
            b.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
            b.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc");
        });

        modelBuilder.Entity<InboxMessage>(b =>
        {
            b.ToTable("inbox_messages");
            b.HasKey(x => x.Id);
            b.Property(x => x.ReceivedAtUtc).HasColumnName("received_at_utc");
            b.Property(x => x.ProcessedAtUtc).HasColumnName("processed_at_utc");
            b.Property(x => x.Status).HasColumnName("status").HasConversion<int>();
            b.Property(x => x.LastError).HasColumnName("last_error");
        });

        modelBuilder.Entity<PaymentTransaction>(b =>
        {
            b.ToTable("payment_transactions");
            b.HasKey(x => x.Id);
            b.Property(x => x.OrderId).HasColumnName("order_id");
            b.Property(x => x.UserId).HasColumnName("user_id").IsRequired();
            b.Property(x => x.Amount).HasColumnName("amount").HasColumnType("numeric(18,2)");
            b.Property(x => x.Status).HasColumnName("status").HasConversion<int>();
            b.Property(x => x.Reason).HasColumnName("reason");
            b.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
            b.HasIndex(x => x.OrderId).IsUnique();
        });

        modelBuilder.Entity<OutboxMessage>(b =>
        {
            b.ToTable("outbox_messages");
            b.HasKey(x => x.Id);
            b.Property(x => x.AggregateId).HasColumnName("aggregate_id");
            b.Property(x => x.Type).HasColumnName("type").IsRequired();
            b.Property(x => x.PayloadJson).HasColumnName("payload_json").HasColumnType("jsonb").IsRequired();
            b.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
            b.Property(x => x.PublishedAtUtc).HasColumnName("published_at_utc");
            b.Property(x => x.Attempts).HasColumnName("attempts");
            b.Property(x => x.LastError).HasColumnName("last_error");
            b.HasIndex(x => x.PublishedAtUtc);
        });
    }
}
