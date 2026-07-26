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
    public DbSet<Attendance> Attendances => Set<Attendance>();
    public DbSet<SyncQueue> SyncQueues => Set<SyncQueue>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<PurchaseOrder> PurchaseOrders => Set<PurchaseOrder>();
    public DbSet<PurchaseOrderItem> PurchaseOrderItems => Set<PurchaseOrderItem>();
    public DbSet<SalesOrder> SalesOrders => Set<SalesOrder>();
    public DbSet<SalesOrderItem> SalesOrderItems => Set<SalesOrderItem>();
    public DbSet<DeliveryChallan> DeliveryChallans => Set<DeliveryChallan>();
    public DbSet<DeliveryChallanItem> DeliveryChallanItems => Set<DeliveryChallanItem>();
    public DbSet<LedgerEntry> LedgerEntries => Set<LedgerEntry>();
    public DbSet<LabelTemplate> LabelTemplates => Set<LabelTemplate>();
    public DbSet<Setting> Settings => Set<Setting>();

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

        modelBuilder.Entity<Attendance>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.EmployeeId).IsRequired();
            entity.Property(e => e.Date).IsRequired();
            entity.Property(e => e.ClockInTime).IsRequired();
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
        });

        modelBuilder.Entity<Supplier>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Phone).HasMaxLength(15);
            entity.Property(e => e.CreditLimit).HasColumnType("numeric(12,2)");
            entity.Property(e => e.CurrentBalance).HasColumnType("numeric(12,2)");
        });

        modelBuilder.Entity<PurchaseOrder>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.OrderNumber).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Subtotal).HasColumnType("numeric(12,2)");
            entity.Property(e => e.TaxAmount).HasColumnType("numeric(12,2)");
            entity.Property(e => e.DiscountAmount).HasColumnType("numeric(12,2)");
            entity.Property(e => e.TotalAmount).HasColumnType("numeric(12,2)");
            entity.HasOne(e => e.Supplier).WithMany().HasForeignKey(e => e.SupplierId);
            entity.HasMany(e => e.Items).WithOne(e => e.PurchaseOrder).HasForeignKey(e => e.PurchaseOrderId);
        });

        modelBuilder.Entity<PurchaseOrderItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.UnitPrice).HasColumnType("numeric(12,2)");
            entity.Property(e => e.TaxAmount).HasColumnType("numeric(12,2)");
            entity.Property(e => e.TotalAmount).HasColumnType("numeric(12,2)");
            entity.HasOne(e => e.Product).WithMany().HasForeignKey(e => e.ProductId);
        });

        modelBuilder.Entity<SalesOrder>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.OrderNumber).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Subtotal).HasColumnType("numeric(12,2)");
            entity.Property(e => e.TaxAmount).HasColumnType("numeric(12,2)");
            entity.Property(e => e.DiscountAmount).HasColumnType("numeric(12,2)");
            entity.Property(e => e.TotalAmount).HasColumnType("numeric(12,2)");
            entity.HasOne(e => e.Customer).WithMany().HasForeignKey(e => e.CustomerId);
            entity.HasMany(e => e.Items).WithOne(e => e.SalesOrder).HasForeignKey(e => e.SalesOrderId);
        });

        modelBuilder.Entity<SalesOrderItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.UnitPrice).HasColumnType("numeric(12,2)");
            entity.Property(e => e.TaxAmount).HasColumnType("numeric(12,2)");
            entity.Property(e => e.TotalAmount).HasColumnType("numeric(12,2)");
            entity.HasOne(e => e.Product).WithMany().HasForeignKey(e => e.ProductId);
        });

        modelBuilder.Entity<DeliveryChallan>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ChallanNumber).IsRequired().HasMaxLength(50);
            entity.HasOne(e => e.Customer).WithMany().HasForeignKey(e => e.CustomerId);
            entity.HasOne(e => e.SalesOrder).WithMany().HasForeignKey(e => e.SalesOrderId);
            entity.HasMany(e => e.Items).WithOne(e => e.DeliveryChallan).HasForeignKey(e => e.DeliveryChallanId);
        });

        modelBuilder.Entity<DeliveryChallanItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.Product).WithMany().HasForeignKey(e => e.ProductId);
        });

        modelBuilder.Entity<LedgerEntry>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.EntryType).IsRequired().HasMaxLength(20);
            entity.Property(e => e.AccountHead).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Amount).HasColumnType("numeric(12,2)");
        });

        modelBuilder.Entity<LabelTemplate>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Type).IsRequired().HasMaxLength(50);
        });

        modelBuilder.Entity<Setting>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Key).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Value).IsRequired().HasMaxLength(1000);
        });
    }
}
