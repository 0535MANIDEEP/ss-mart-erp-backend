using SS_MART_API.Core.Domain.Entities;

namespace SS_MART_API.Core.Domain.Interfaces;

public interface IBillRepository
{
    Task<IEnumerable<Bill>> GetAllAsync(Guid? customerId = null, string? startDate = null, string? endDate = null, int page = 1, int perPage = 20);
    Task<Bill?> GetByIdAsync(Guid id);
    Task<Bill> CreateAsync(Bill bill);
    Task<Bill> CreateReturnAsync(Guid originalBillId, List<BillItem> returnItems);
    Task<string> GenerateBillNumberAsync();
}
