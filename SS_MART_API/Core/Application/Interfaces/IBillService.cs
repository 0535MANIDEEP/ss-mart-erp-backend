using SS_MART_API.Core.Domain.Entities;

namespace SS_MART_API.Core.Application.Interfaces;

public interface IBillService
{
    Task<IEnumerable<Bill>> GetBillsAsync(Guid? customerId = null, string? startDate = null, string? endDate = null, int page = 1, int perPage = 20);
    Task<Bill?> GetBillByIdAsync(Guid id);
    Task<Bill> CreateBillAsync(Bill bill);
    Task<Bill> ProcessReturnAsync(Guid billId, List<BillItem> returnItems);
}
