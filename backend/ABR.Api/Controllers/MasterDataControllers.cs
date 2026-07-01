using ABR.Api.Authorization;
using ABR.Application.Common;
using ABR.Application.DTOs.MasterData;
using ABR.Application.Interfaces;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ABR.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/sites")]
public class SiteController : ControllerBase
{
    private readonly ISiteService _siteService;
    private readonly IValidator<CreateSiteDto> _createValidator;

    public SiteController(ISiteService siteService, IValidator<CreateSiteDto> createValidator)
    {
        _siteService = siteService;
        _createValidator = createValidator;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<SiteDto>>>> GetAll(CancellationToken cancellationToken)
    {
        var sites = await _siteService.GetAllAsync(cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<SiteDto>>.Ok(sites));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<SiteDto>>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var site = await _siteService.GetByIdAsync(id, cancellationToken);
        if (site is null) return NotFound(ApiResponse<SiteDto>.Fail("Site not found."));
        return Ok(ApiResponse<SiteDto>.Ok(site));
    }

    [RequirePermission(AppModules.Sites, PermissionLevel.Manage)]
    [HttpPost]
    public async Task<ActionResult<ApiResponse<SiteDto>>> Create([FromBody] CreateSiteDto dto, CancellationToken cancellationToken)
    {
        var validation = await _createValidator.ValidateAsync(dto, cancellationToken);
        if (!validation.IsValid)
            return BadRequest(ApiResponse<SiteDto>.Fail("Validation failed.", validation.Errors.Select(e => e.ErrorMessage).ToList()));

        var site = await _siteService.CreateAsync(dto, cancellationToken);
        return Ok(ApiResponse<SiteDto>.Ok(site, "Site created."));
    }

    [RequirePermission(AppModules.Sites, PermissionLevel.Manage)]
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<SiteDto>>> Update(Guid id, [FromBody] UpdateSiteDto dto, CancellationToken cancellationToken)
    {
        var site = await _siteService.UpdateAsync(id, dto, cancellationToken);
        if (site is null) return NotFound(ApiResponse<SiteDto>.Fail("Site not found."));
        return Ok(ApiResponse<SiteDto>.Ok(site, "Site updated."));
    }

    [RequirePermission(AppModules.Sites, PermissionLevel.Manage)]
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await _siteService.DeleteAsync(id, cancellationToken);
        if (!deleted) return NotFound(ApiResponse<object>.Fail("Site not found."));
        return Ok(ApiResponse<object>.Ok(new { }, "Site disabled."));
    }
}

[ApiController]
[Authorize]
[Route("api/wings")]
public class WingController : ControllerBase
{
    private readonly IWingService _wingService;
    private readonly IValidator<CreateWingDto> _createValidator;

    public WingController(IWingService wingService, IValidator<CreateWingDto> createValidator)
    {
        _wingService = wingService;
        _createValidator = createValidator;
    }

    [HttpGet("{siteId:guid}")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<WingDto>>>> GetBySite(
        Guid siteId,
        [FromQuery] string? type,
        CancellationToken cancellationToken)
    {
        var wings = await _wingService.GetBySiteAsync(siteId, type ?? "wing", cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<WingDto>>.Ok(wings));
    }

    [RequirePermission(AppModules.Wings, PermissionLevel.Manage)]
    [HttpPost]
    public async Task<ActionResult<ApiResponse<WingDto>>> Create([FromBody] CreateWingDto dto, CancellationToken cancellationToken)
    {
        var validation = await _createValidator.ValidateAsync(dto, cancellationToken);
        if (!validation.IsValid)
            return BadRequest(ApiResponse<WingDto>.Fail("Validation failed.", validation.Errors.Select(e => e.ErrorMessage).ToList()));

        var wing = await _wingService.CreateAsync(dto, cancellationToken);
        return Ok(ApiResponse<WingDto>.Ok(wing, "Wing created with flats."));
    }

    [RequirePermission(AppModules.Wings, PermissionLevel.Manage)]
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<WingDto>>> Update(Guid id, [FromBody] UpdateWingDto dto, CancellationToken cancellationToken)
    {
        var wing = await _wingService.UpdateAsync(id, dto, cancellationToken);
        if (wing is null) return NotFound(ApiResponse<WingDto>.Fail("Wing not found."));
        return Ok(ApiResponse<WingDto>.Ok(wing, "Wing updated."));
    }

    [RequirePermission(AppModules.Wings, PermissionLevel.Manage)]
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var deleted = await _wingService.DeleteAsync(id, cancellationToken);
            if (!deleted) return NotFound(ApiResponse<object>.Fail("Wing not found."));
            return Ok(ApiResponse<object>.Ok(new { }, "Wing deleted."));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<object>.Fail(ex.Message));
        }
    }
}

[ApiController]
[Authorize]
[Route("api/plots")]
public class PlotController : ControllerBase
{
    private readonly IWingService _wingService;
    private readonly IValidator<CreatePlotDto> _createValidator;
    private readonly IValidator<UpdatePlotDto> _updateValidator;

    public PlotController(
        IWingService wingService,
        IValidator<CreatePlotDto> createValidator,
        IValidator<UpdatePlotDto> updateValidator)
    {
        _wingService = wingService;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    [HttpGet("{siteId:guid}")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<WingDto>>>> GetBySite(Guid siteId, CancellationToken cancellationToken)
    {
        var plots = await _wingService.GetBySiteAsync(siteId, "plot", cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<WingDto>>.Ok(plots));
    }

    [RequirePermission(AppModules.Wings, PermissionLevel.Manage)]
    [HttpPost]
    public async Task<ActionResult<ApiResponse<WingDto>>> Create([FromBody] CreatePlotDto dto, CancellationToken cancellationToken)
    {
        var validation = await _createValidator.ValidateAsync(dto, cancellationToken);
        if (!validation.IsValid)
            return BadRequest(ApiResponse<WingDto>.Fail("Validation failed.", validation.Errors.Select(e => e.ErrorMessage).ToList()));

        var plot = await _wingService.CreatePlotAsync(dto, cancellationToken);
        return Ok(ApiResponse<WingDto>.Ok(plot, "Plot scheme created with units."));
    }

    [RequirePermission(AppModules.Wings, PermissionLevel.Manage)]
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<WingDto>>> Update(Guid id, [FromBody] UpdatePlotDto dto, CancellationToken cancellationToken)
    {
        var validation = await _updateValidator.ValidateAsync(dto, cancellationToken);
        if (!validation.IsValid)
            return BadRequest(ApiResponse<WingDto>.Fail("Validation failed.", validation.Errors.Select(e => e.ErrorMessage).ToList()));

        var plot = await _wingService.UpdatePlotAsync(id, dto, cancellationToken);
        if (plot is null) return NotFound(ApiResponse<WingDto>.Fail("Plot not found."));
        return Ok(ApiResponse<WingDto>.Ok(plot, "Plot updated."));
    }

    [RequirePermission(AppModules.Wings, PermissionLevel.Manage)]
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var deleted = await _wingService.DeleteAsync(id, cancellationToken);
            if (!deleted) return NotFound(ApiResponse<object>.Fail("Plot not found."));
            return Ok(ApiResponse<object>.Ok(new { }, "Plot deleted."));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<object>.Fail(ex.Message));
        }
    }
}

[ApiController]
[Authorize]
[Route("api/flats")]
public class FlatController : ControllerBase
{
    private readonly IFlatService _flatService;

    public FlatController(IFlatService flatService) => _flatService = flatService;

    [HttpGet("{wingId:guid}/grid")]
    public async Task<ActionResult<ApiResponse<FlatGridDto>>> GetGrid(Guid wingId, CancellationToken cancellationToken)
    {
        try
        {
            var grid = await _flatService.GetGridByWingAsync(wingId, cancellationToken);
            return Ok(ApiResponse<FlatGridDto>.Ok(grid));
        }
        catch (KeyNotFoundException)
        {
            return NotFound(ApiResponse<FlatGridDto>.Fail("Wing not found."));
        }
    }

    [HttpGet("{wingId:guid}")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<FlatDto>>>> GetByWing(Guid wingId, CancellationToken cancellationToken)
    {
        var flats = await _flatService.GetByWingAsync(wingId, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<FlatDto>>.Ok(flats));
    }
}

[ApiController]
[Authorize]
[Route("api/ledgers/main")]
public class MainLedgerController : ControllerBase
{
    private readonly IMainLedgerService _service;
    private readonly IValidator<CreateMainLedgerDto> _validator;

    public MainLedgerController(IMainLedgerService service, IValidator<CreateMainLedgerDto> validator)
    {
        _service = service;
        _validator = validator;
    }

    [HttpGet("{siteId:guid}")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<MainLedgerDto>>>> GetBySite(Guid siteId, CancellationToken cancellationToken)
    {
        var ledgers = await _service.GetBySiteAsync(siteId, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<MainLedgerDto>>.Ok(ledgers));
    }

    [RequirePermission(AppModules.Ledgers, PermissionLevel.Manage)]
    [HttpPost]
    public async Task<ActionResult<ApiResponse<MainLedgerDto>>> Create([FromBody] CreateMainLedgerDto dto, CancellationToken cancellationToken)
    {
        var validation = await _validator.ValidateAsync(dto, cancellationToken);
        if (!validation.IsValid)
            return BadRequest(ApiResponse<MainLedgerDto>.Fail("Validation failed.", validation.Errors.Select(e => e.ErrorMessage).ToList()));

        var ledger = await _service.CreateAsync(dto, cancellationToken);
        return Ok(ApiResponse<MainLedgerDto>.Ok(ledger, "Main ledger created."));
    }

    [RequirePermission(AppModules.Ledgers, PermissionLevel.Manage)]
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<MainLedgerDto>>> Update(Guid id, [FromBody] CreateMainLedgerDto dto, CancellationToken cancellationToken)
    {
        var ledger = await _service.UpdateAsync(id, dto, cancellationToken);
        if (ledger is null) return NotFound(ApiResponse<MainLedgerDto>.Fail("Ledger not found."));
        return Ok(ApiResponse<MainLedgerDto>.Ok(ledger, "Main ledger updated."));
    }

    [RequirePermission(AppModules.Ledgers, PermissionLevel.Manage)]
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var deleted = await _service.DeleteAsync(id, cancellationToken);
            if (!deleted) return NotFound(ApiResponse<object>.Fail("Ledger not found."));
            return Ok(ApiResponse<object>.Ok(new { }, "Main ledger deleted."));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<object>.Fail(ex.Message));
        }
    }
}

[ApiController]
[Authorize]
[Route("api/ledgers/sub")]
public class SubLedgerController : ControllerBase
{
    private readonly ISubLedgerService _service;
    private readonly IValidator<CreateSubLedgerDto> _validator;

    public SubLedgerController(ISubLedgerService service, IValidator<CreateSubLedgerDto> validator)
    {
        _service = service;
        _validator = validator;
    }

    [HttpGet("{mainLedgerId:guid}")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<SubLedgerDto>>>> GetByMainLedger(Guid mainLedgerId, CancellationToken cancellationToken)
    {
        var ledgers = await _service.GetByMainLedgerAsync(mainLedgerId, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<SubLedgerDto>>.Ok(ledgers));
    }

    [HttpGet("search")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<SubLedgerDto>>>> Search([FromQuery] Guid siteId, [FromQuery] string flatNo, CancellationToken cancellationToken)
    {
        var ledgers = await _service.SearchByFlatNoAsync(siteId, flatNo, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<SubLedgerDto>>.Ok(ledgers));
    }

    [RequirePermission(AppModules.Ledgers, PermissionLevel.Manage)]
    [HttpPost]
    public async Task<ActionResult<ApiResponse<SubLedgerDto>>> Create([FromBody] CreateSubLedgerDto dto, CancellationToken cancellationToken)
    {
        var validation = await _validator.ValidateAsync(dto, cancellationToken);
        if (!validation.IsValid)
            return BadRequest(ApiResponse<SubLedgerDto>.Fail("Validation failed.", validation.Errors.Select(e => e.ErrorMessage).ToList()));

        var ledger = await _service.CreateAsync(dto, cancellationToken);
        return Ok(ApiResponse<SubLedgerDto>.Ok(ledger, "Sub ledger created."));
    }

    [RequirePermission(AppModules.Ledgers, PermissionLevel.Manage)]
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<SubLedgerDto>>> Update(Guid id, [FromBody] CreateSubLedgerDto dto, CancellationToken cancellationToken)
    {
        var ledger = await _service.UpdateAsync(id, dto, cancellationToken);
        if (ledger is null) return NotFound(ApiResponse<SubLedgerDto>.Fail("Sub ledger not found."));
        return Ok(ApiResponse<SubLedgerDto>.Ok(ledger, "Sub ledger updated."));
    }

    [RequirePermission(AppModules.Ledgers, PermissionLevel.Manage)]
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var deleted = await _service.DeleteAsync(id, cancellationToken);
            if (!deleted) return NotFound(ApiResponse<object>.Fail("Sub ledger not found."));
            return Ok(ApiResponse<object>.Ok(new { }, "Sub ledger deleted."));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<object>.Fail(ex.Message));
        }
    }
}

[ApiController]
[Authorize]
[Route("api/conditions")]
public class ConditionController : ControllerBase
{
    private readonly IConditionService _service;
    private readonly IValidator<CreateConditionDto> _conditionValidator;
    private readonly IValidator<CreateConditionItemDto> _itemValidator;
    private readonly IValidator<UpdateConditionDto> _updateConditionValidator;
    private readonly IValidator<UpdateConditionItemDto> _updateItemValidator;

    public ConditionController(
        IConditionService service,
        IValidator<CreateConditionDto> conditionValidator,
        IValidator<CreateConditionItemDto> itemValidator,
        IValidator<UpdateConditionDto> updateConditionValidator,
        IValidator<UpdateConditionItemDto> updateItemValidator)
    {
        _service = service;
        _conditionValidator = conditionValidator;
        _itemValidator = itemValidator;
        _updateConditionValidator = updateConditionValidator;
        _updateItemValidator = updateItemValidator;
    }

    [HttpGet("{siteId:guid}")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ConditionDto>>>> GetBySite(Guid siteId, CancellationToken cancellationToken)
    {
        var conditions = await _service.GetBySiteAsync(siteId, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<ConditionDto>>.Ok(conditions));
    }

    [RequirePermission(AppModules.Conditions, PermissionLevel.Manage)]
    [HttpPost]
    public async Task<ActionResult<ApiResponse<ConditionDto>>> Create([FromBody] CreateConditionDto dto, CancellationToken cancellationToken)
    {
        var validation = await _conditionValidator.ValidateAsync(dto, cancellationToken);
        if (!validation.IsValid)
            return BadRequest(ApiResponse<ConditionDto>.Fail("Validation failed.", validation.Errors.Select(e => e.ErrorMessage).ToList()));

        var condition = await _service.CreateAsync(dto, cancellationToken);
        return Ok(ApiResponse<ConditionDto>.Ok(condition, "Condition created."));
    }

    [HttpGet("{id:guid}/items")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ConditionItemDto>>>> GetItems(Guid id, CancellationToken cancellationToken)
    {
        var items = await _service.GetItemsAsync(id, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<ConditionItemDto>>.Ok(items));
    }

    [RequirePermission(AppModules.Conditions, PermissionLevel.Manage)]
    [HttpPost("{id:guid}/items")]
    public async Task<ActionResult<ApiResponse<ConditionItemDto>>> AddItem(Guid id, [FromBody] CreateConditionItemDto dto, CancellationToken cancellationToken)
    {
        var validation = await _itemValidator.ValidateAsync(dto, cancellationToken);
        if (!validation.IsValid)
            return BadRequest(ApiResponse<ConditionItemDto>.Fail("Validation failed.", validation.Errors.Select(e => e.ErrorMessage).ToList()));

        var item = await _service.AddItemAsync(id, dto, cancellationToken);
        return Ok(ApiResponse<ConditionItemDto>.Ok(item, "Condition item added."));
    }

    [RequirePermission(AppModules.Conditions, PermissionLevel.Manage)]
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<ConditionDto>>> Update(Guid id, [FromBody] UpdateConditionDto dto, CancellationToken cancellationToken)
    {
        var validation = await _updateConditionValidator.ValidateAsync(dto, cancellationToken);
        if (!validation.IsValid)
            return BadRequest(ApiResponse<ConditionDto>.Fail("Validation failed.", validation.Errors.Select(e => e.ErrorMessage).ToList()));

        var condition = await _service.UpdateAsync(id, dto, cancellationToken);
        if (condition is null) return NotFound(ApiResponse<ConditionDto>.Fail("Condition not found."));
        return Ok(ApiResponse<ConditionDto>.Ok(condition, "Condition updated."));
    }

    [RequirePermission(AppModules.Conditions, PermissionLevel.Manage)]
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var deleted = await _service.DeleteAsync(id, cancellationToken);
            if (!deleted) return NotFound(ApiResponse<object>.Fail("Condition not found."));
            return Ok(ApiResponse<object>.Ok(new { }, "Condition deleted."));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<object>.Fail(ex.Message));
        }
    }

    [RequirePermission(AppModules.Conditions, PermissionLevel.Manage)]
    [HttpPut("items/{itemId:guid}")]
    public async Task<ActionResult<ApiResponse<ConditionItemDto>>> UpdateItem(Guid itemId, [FromBody] UpdateConditionItemDto dto, CancellationToken cancellationToken)
    {
        var validation = await _updateItemValidator.ValidateAsync(dto, cancellationToken);
        if (!validation.IsValid)
            return BadRequest(ApiResponse<ConditionItemDto>.Fail("Validation failed.", validation.Errors.Select(e => e.ErrorMessage).ToList()));

        try
        {
            var item = await _service.UpdateItemAsync(itemId, dto, cancellationToken);
            if (item is null) return NotFound(ApiResponse<ConditionItemDto>.Fail("Condition item not found."));
            return Ok(ApiResponse<ConditionItemDto>.Ok(item, "Condition item updated."));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<ConditionItemDto>.Fail(ex.Message));
        }
    }

    [RequirePermission(AppModules.Conditions, PermissionLevel.Manage)]
    [HttpDelete("items/{itemId:guid}")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteItem(Guid itemId, CancellationToken cancellationToken)
    {
        try
        {
            var deleted = await _service.DeleteItemAsync(itemId, cancellationToken);
            if (!deleted) return NotFound(ApiResponse<object>.Fail("Condition item not found."));
            return Ok(ApiResponse<object>.Ok(new { }, "Condition item deleted."));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<object>.Fail(ex.Message));
        }
    }
}

[ApiController]
[Authorize]
[Route("api/banks")]
public class BankController : ControllerBase
{
    private readonly IBankAccountService _service;
    private readonly IValidator<CreateBankAccountDto> _createValidator;

    public BankController(IBankAccountService service, IValidator<CreateBankAccountDto> createValidator)
    {
        _service = service;
        _createValidator = createValidator;
    }

    [HttpGet("{siteId:guid}")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<BankAccountDto>>>> GetBySite(Guid siteId, CancellationToken cancellationToken)
    {
        var banks = await _service.GetBySiteAsync(siteId, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<BankAccountDto>>.Ok(banks));
    }

    [RequirePermission(AppModules.Banks, PermissionLevel.Manage)]
    [HttpPost]
    public async Task<ActionResult<ApiResponse<BankAccountDto>>> Create([FromBody] CreateBankAccountDto dto, CancellationToken cancellationToken)
    {
        var validation = await _createValidator.ValidateAsync(dto, cancellationToken);
        if (!validation.IsValid)
            return BadRequest(ApiResponse<BankAccountDto>.Fail("Validation failed.", validation.Errors.Select(e => e.ErrorMessage).ToList()));

        var bank = await _service.CreateAsync(dto, cancellationToken);
        return Ok(ApiResponse<BankAccountDto>.Ok(bank, "Bank account created."));
    }

    [RequirePermission(AppModules.Banks, PermissionLevel.Manage)]
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<BankAccountDto>>> Update(Guid id, [FromBody] UpdateBankAccountDto dto, CancellationToken cancellationToken)
    {
        var bank = await _service.UpdateAsync(id, dto, cancellationToken);
        if (bank is null) return NotFound(ApiResponse<BankAccountDto>.Fail("Bank account not found."));
        return Ok(ApiResponse<BankAccountDto>.Ok(bank, "Bank account updated."));
    }

    [RequirePermission(AppModules.Banks, PermissionLevel.Manage)]
    [HttpPut("{id:guid}/toggle")]
    public async Task<ActionResult<ApiResponse<object>>> Toggle(Guid id, CancellationToken cancellationToken)
    {
        var toggled = await _service.ToggleActiveAsync(id, cancellationToken);
        if (!toggled) return NotFound(ApiResponse<object>.Fail("Bank account not found."));
        return Ok(ApiResponse<object>.Ok(new { }, "Bank account status updated."));
    }
}

[ApiController]
[Authorize]
[Route("api/brokers")]
public class BrokerController : ControllerBase
{
    private readonly IBrokerService _service;
    private readonly IValidator<CreateBrokerDto> _validator;

    public BrokerController(IBrokerService service, IValidator<CreateBrokerDto> validator)
    {
        _service = service;
        _validator = validator;
    }

    [HttpGet("{siteId:guid}")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<BrokerDto>>>> GetBySite(Guid siteId, CancellationToken cancellationToken)
    {
        var brokers = await _service.GetBySiteAsync(siteId, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<BrokerDto>>.Ok(brokers));
    }

    [RequirePermission(AppModules.Brokers, PermissionLevel.Manage)]
    [HttpPost]
    public async Task<ActionResult<ApiResponse<BrokerDto>>> Create([FromBody] CreateBrokerDto dto, CancellationToken cancellationToken)
    {
        var validation = await _validator.ValidateAsync(dto, cancellationToken);
        if (!validation.IsValid)
            return BadRequest(ApiResponse<BrokerDto>.Fail("Validation failed.", validation.Errors.Select(e => e.ErrorMessage).ToList()));

        var broker = await _service.CreateAsync(dto, cancellationToken);
        return Ok(ApiResponse<BrokerDto>.Ok(broker, "Broker created."));
    }

    [RequirePermission(AppModules.Brokers, PermissionLevel.Manage)]
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<BrokerDto>>> Update(Guid id, [FromBody] CreateBrokerDto dto, CancellationToken cancellationToken)
    {
        var broker = await _service.UpdateAsync(id, dto, cancellationToken);
        if (broker is null) return NotFound(ApiResponse<BrokerDto>.Fail("Broker not found."));
        return Ok(ApiResponse<BrokerDto>.Ok(broker, "Broker updated."));
    }

    [RequirePermission(AppModules.Brokers, PermissionLevel.Manage)]
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var deleted = await _service.DeleteAsync(id, cancellationToken);
            if (!deleted) return NotFound(ApiResponse<object>.Fail("Broker not found."));
            return Ok(ApiResponse<object>.Ok(new { }, "Broker deleted."));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<object>.Fail(ex.Message));
        }
    }
}
