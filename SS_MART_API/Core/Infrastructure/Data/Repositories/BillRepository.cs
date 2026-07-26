using Microsoft.EntityFrameworkCore;
using SS_MART_API.Core.Domain.Entities;
using SS_MART_API.Core.Domain.Interfaces;

namespace SS_MART_API.Core.Infrastructure.Data.Repositories;

public class BillRepository : IBillRepository
{
    private readonly AppDbContext _context;

    public BillRepository(AppDbContext context) => _context = context;

    public async Task<IEnumerable<Bill>> GetAllAsync(Guid? customerId = null, string? startDate = null, string? endDate = null, int page = 1, int perPage = 20)
    {
        var query = _context.Bills.Where(b => b.DeletedAt == null).AsQueryable();
        if (customerId.HasValue) query = query.Where(b => b.CustomerId == customerId);
        if (!string.IsNullOrEmpty(startDate) && DateTime.TryParse(startDate, out var start)) query = query.Where(b => b.BillDate >= start);
        if (!string.IsNullOrEmpty(endDate) && DateTime.TryParse(endDate, out var end)) query = query.Where(b => b.BillDate <= end);
        return await query.OrderByDescending(b => b.BillDate).Skip((page - 1) * perPage).Take(perPage).ToListAsync();
    }

    public async Task<Bill?> GetByIdAsync(Guid id)
        => await _context.Bills.Include(b => b.Items).Include(b => b.Customer).FirstOrDefaultAsync(b => b.Id == id);

    public async Task<Bill> CreateAsync(Bill bill)
    {
        bill.Id = Guid.NewGuid();
        bill.CreatedAt = DateTime.UtcNow;
        bill.UpdatedAt = DateTime.UtcNow;
        bill.BillNumber = await GenerateBillNumberAsync();
        _context.Bills.Add(bill);
        await _context.SaveChangesAsync();
        foreach (var item in bill.Items)
        {
            var stock = await _context.Stocks.FirstOrDefaultAsync(s => s.ProductId == item.ProductId);
            if (stock != null) { stock.Quantity -= (int)item.Quantity; stock.LastUpdated = DateTime.UtcNow; }
        }
        await _context.SaveChangesAsync();
        return bill;
    }

    public async Task<Bill> CreateReturnAsync(Guid originalBillId, List<BillItem> returnItems)
    {
        var original = await _context.Bills.Include(b => b.Items).FirstOrDefaultAsync(b => b.Id == originalBillId);
        if (original == null) throw new InvalidOperationException("Original bill not found");
        var returnBill = new Bill
        {
            Id = Guid.NewGuid(),
            BillNumber = await GenerateBillNumberAsync(),
            CustomerId = original.CustomerId,
            BillDate = DateTime.UtcNow,
            Subtotal = returnItems.Sum(i => (decimal)i.Quantity * i.UnitPrice),
            TotalAmount = returnItems.Sum(i => (decimal)i.Quantity * i.UnitPrice),
            PaymentMode = "CASH",
            Status = "completed",
            IsReturn = true,
            ReferenceBillId = originalBillId,
            CreatedBy = original.CreatedBy,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Items = returnItems.Select(i => new BillItem { Id = Guid.NewGuid(), ProductId = i.ProductId, Quantity = i.Quantity, UnitPrice = i.UnitPrice, TotalAmount = (decimal)i.Quantity * i.UnitPrice }).ToList()
        };
        _context.Bills.Add(returnBill);
        foreach (var item in returnBill.Items)
        {
            var stock = await _context.Stocks.FirstOrDefaultAsync(s => s.ProductId == item.ProductId);
            if (stock != null) { stock.Quantity += (int)item.Quantity; stock.LastUpdated = DateTime.UtcNow; }
        }
        await _context.SaveChangesAsync();
        return returnBill;
    }

    public async Task<string> GenerateBillNumberAsync()
    {
        var lastBill = await _context.Bills.OrderByDescending(b => b.CreatedAt).FirstOrDefaultAsync();
        if (lastBill == null) return "BILL-0001";
        var lastNumber = int.Parse(lastBill.BillNumber.Split('-')[1]);
        return $"BILL-{(lastNumber + 1).ToString("D4")}";
    }
}
