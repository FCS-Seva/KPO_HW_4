using Microsoft.EntityFrameworkCore;
using OrdersService.Domain;

namespace OrdersService.Infrastructure.Persistence;

public sealed class OrdersDbContext : DbContext
{
    public OrdersDbContext(DbContextOptions<OrdersDbContext> options) : base(options) { }

    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Order>(b =>
        {
            b.ToTable("orders");
            b.HasKey(x => x.Id);
            b.Property(x => x.UserId).HasColumnName("user_id").IsRequired();
            b.Property(x => x.Amount).HasColumnName("amount").HasColumnType("numeric(18,2)");
            b.Property(x => x.Description).HasColumnName("description").IsRequired();
            b.Property(x => x.Status).HasColumnName("status").HasConversion<int>();
            b.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
            b.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc");
            b.HasIndex(x => new { x.UserId, x.CreatedAtUtc });
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
