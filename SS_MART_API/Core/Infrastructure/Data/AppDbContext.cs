using Microsoft.EntityFrameworkCore;
using SS_MART_API.Core.Domain.Entities;

namespace SS_MART_API.Core.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Product> Products => Set<Product>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Bill> Bills => Set<Bill>();
    public DbSet<BillItem> BillItems => Set<BillItem>();
    public DbSet<Stock> Stocks => Set<Stock>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();
    public DbSet<LoyaltyTransaction> LoyaltyTransactions => Set<LoyaltyTransaction>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<SyncQueue> SyncQueues => Set<SyncQueue>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Product configuration
        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
            entity.Property(e => e.HSNCode).IsRequired().HasMaxLength(20);
            entity.Property(e => e.MRP).HasColumnType("numeric(12,2)");
            entity.Property(e => e.SellingPrice).HasColumnType("numeric(12,2)");
            entity.Property(e => e.PurchasePrice).HasColumnType("numeric(12,2)");
            entity.Property(e => e.TaxRate).HasColumnType("numeric(5,2)");
        });

        // Customer configuration
        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Phone).HasMaxLength(15);
            entity.Property(e => e.CreditLimit).HasColumnType("numeric(12,2)");
            entity.Property(e => e.CurrentBalance).HasColumnType("numeric(12,2)");
        });

        // Bill configuration
        modelBuilder.Entity<Bill>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.BillNumber).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Subtotal).HasColumnType("numeric(12,2)");
            entity.Property(e => e.TaxAmount).HasColumnType("numeric(12,2)");
            entity.Property(e => e.DiscountAmount).HasColumnType("numeric(12,2)");
            entity.Property(e => e.TotalAmount).HasColumnType("numeric(12,2)");
            entity.Property(e => e.PaidAmount).HasColumnType("numeric(12,2)");
            entity.Property(e => e.DueAmount).HasColumnType("numeric(12,2)");
        });

        // BillItem configuration
        modelBuilder.Entity<BillItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.UnitPrice).HasColumnType("numeric(12,2)");
            entity.Property(e => e.TotalAmount).HasColumnType("numeric(12,2)");
        });
    }
}
