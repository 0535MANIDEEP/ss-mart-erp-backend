using SS_MART_API.Core.Application.Interfaces;
using SS_MART_API.Core.Domain.Entities;
using SS_MART_API.Core.Domain.Interfaces;

namespace SS_MART_API.Core.Application.Services;

public class BillService : IBillService
{
    private readonly IBillRepository _repository;

    public BillService(IBillRepository repository) => _repository = repository;

    public Task<IEnumerable<Bill>> GetBillsAsync(Guid? customerId = null, string? startDate = null, string? endDate = null, int page = 1, int perPage = 20)
        => _repository.GetAllAsync(customerId, startDate, endDate, page, perPage);

    public Task<Bill?> GetBillByIdAsync(Guid id) => _repository.GetByIdAsync(id);

    public Task<Bill> CreateBillAsync(Bill bill) => _repository.CreateAsync(bill);

    public Task<Bill> ProcessReturnAsync(Guid billId, List<BillItem> returnItems)
        => _repository.CreateReturnAsync(billId, returnItems);
}
