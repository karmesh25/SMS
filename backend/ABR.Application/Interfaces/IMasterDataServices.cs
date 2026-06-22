using ABR.Application.DTOs.MasterData;

namespace ABR.Application.Interfaces;

public interface ISiteService
{
    Task<IReadOnlyList<SiteDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<SiteDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<SiteDto> CreateAsync(CreateSiteDto dto, CancellationToken cancellationToken = default);
    Task<SiteDto?> UpdateAsync(Guid id, UpdateSiteDto dto, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

public interface IWingService
{
    Task<IReadOnlyList<WingDto>> GetBySiteAsync(Guid siteId, CancellationToken cancellationToken = default);
    Task<WingDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<WingDto> CreateAsync(CreateWingDto dto, CancellationToken cancellationToken = default);
    Task<WingDto?> UpdateAsync(Guid id, UpdateWingDto dto, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

public interface IFlatService
{
    Task<FlatGridDto> GetGridByWingAsync(Guid wingId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FlatDto>> GetByWingAsync(Guid wingId, CancellationToken cancellationToken = default);
}

public interface IMainLedgerService
{
    Task<IReadOnlyList<MainLedgerDto>> GetBySiteAsync(Guid siteId, CancellationToken cancellationToken = default);
    Task<MainLedgerDto> CreateAsync(CreateMainLedgerDto dto, CancellationToken cancellationToken = default);
    Task<MainLedgerDto?> UpdateAsync(Guid id, CreateMainLedgerDto dto, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

public interface ISubLedgerService
{
    Task<IReadOnlyList<SubLedgerDto>> GetByMainLedgerAsync(Guid mainLedgerId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SubLedgerDto>> SearchByFlatNoAsync(Guid siteId, string flatNo, CancellationToken cancellationToken = default);
    Task<SubLedgerDto> CreateAsync(CreateSubLedgerDto dto, CancellationToken cancellationToken = default);
    Task<SubLedgerDto?> UpdateAsync(Guid id, CreateSubLedgerDto dto, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

public interface IConditionService
{
    Task<IReadOnlyList<ConditionDto>> GetBySiteAsync(Guid siteId, CancellationToken cancellationToken = default);
    Task<ConditionDto> CreateAsync(CreateConditionDto dto, CancellationToken cancellationToken = default);
    Task<ConditionDto?> UpdateAsync(Guid id, UpdateConditionDto dto, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ConditionItemDto>> GetItemsAsync(Guid conditionId, CancellationToken cancellationToken = default);
    Task<ConditionItemDto> AddItemAsync(Guid conditionId, CreateConditionItemDto dto, CancellationToken cancellationToken = default);
    Task<ConditionItemDto?> UpdateItemAsync(Guid itemId, UpdateConditionItemDto dto, CancellationToken cancellationToken = default);
    Task<bool> DeleteItemAsync(Guid itemId, CancellationToken cancellationToken = default);
}

public interface IBankAccountService
{
    Task<IReadOnlyList<BankAccountDto>> GetBySiteAsync(Guid siteId, CancellationToken cancellationToken = default);
    Task<BankAccountDto> CreateAsync(CreateBankAccountDto dto, CancellationToken cancellationToken = default);
    Task<BankAccountDto?> UpdateAsync(Guid id, UpdateBankAccountDto dto, CancellationToken cancellationToken = default);
    Task<bool> ToggleActiveAsync(Guid id, CancellationToken cancellationToken = default);
}

public interface IBrokerService
{
    Task<IReadOnlyList<BrokerDto>> GetBySiteAsync(Guid siteId, CancellationToken cancellationToken = default);
    Task<BrokerDto> CreateAsync(CreateBrokerDto dto, CancellationToken cancellationToken = default);
    Task<BrokerDto?> UpdateAsync(Guid id, CreateBrokerDto dto, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
