using Catrashop.BuildingBlocks.Money;
using Catrashop.Modules.Shipping.Application.Abstractions;
using Catrashop.Modules.Shipping.Application.Contracts;
using Catrashop.Modules.Shipping.Application.Services;
using Catrashop.Modules.Shipping.Application.Validation;
using Catrashop.Modules.Shipping.Domain;
using Microsoft.Extensions.Options;
using Xunit;

namespace Catrashop.Modules.Shipping.Tests;

public class HondurasDepartmentsTests
{
    [Fact]
    public void There_are_eighteen_departments_with_unique_iso_codes()
    {
        Assert.Equal(18, HondurasDepartments.All.Count);
        Assert.Equal(18, HondurasDepartments.All.Select(d => d.Code).Distinct().Count());
        Assert.Equal(18, HondurasDepartments.All.Select(d => d.Name).Distinct().Count());
        Assert.All(HondurasDepartments.All, d => Assert.Matches("^HN-[A-Z]{2}$", d.Code));
    }

    [Theory]
    [InlineData("HN-FM", "HN-FM")]
    [InlineData(" hn-cr ", "HN-CR")]
    [InlineData("HN-XX", null)]
    [InlineData("FM", null)]
    [InlineData("", null)]
    [InlineData(null, null)]
    public void Codes_are_normalized_and_unknown_ones_rejected(string? input, string? expected) =>
        Assert.Equal(expected, HondurasDepartments.Normalize(input));
}

public class ShippingZoneTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 28, 12, 0, 0, TimeSpan.Zero);

    private static ShippingZone Zone(params string[] departments) =>
        ShippingZone.Create(" Central ", "Zona Central", 30m, "hnl", 1, 2, true, departments, Now);

    [Fact]
    public void Create_normalizes_and_keeps_the_departments()
    {
        var zone = Zone("hn-fm", "HN-CM", "HN-FM");

        Assert.Equal("central", zone.Code);
        Assert.Equal("HNL", zone.Currency);
        Assert.True(zone.IsActive);
        Assert.Equal(new[] { "HN-CM", "HN-FM" }, zone.Departments.Select(d => d.DepartmentCode).Order());
    }

    [Fact]
    public void Update_replaces_the_departments_and_the_price_but_not_the_code()
    {
        var zone = Zone("HN-FM", "HN-CM");

        zone.Update("Zona Centro", 45m, "HNL", 1, 3, false, ["HN-CM", "HN-LP"], Now.AddHours(1));

        Assert.Equal("central", zone.Code);
        Assert.Equal(45m, zone.Cost);
        Assert.False(zone.CashOnDeliveryCovered);
        Assert.Equal(new[] { "HN-CM", "HN-LP" }, zone.Departments.Select(d => d.DepartmentCode).Order());
    }

    [Fact]
    public void A_zone_can_be_switched_off_and_on()
    {
        var zone = Zone("HN-FM");
        zone.Deactivate(Now);
        Assert.False(zone.IsActive);
        zone.Activate(Now);
        Assert.True(zone.IsActive);
    }
}

public class ShippingZoneValidationTests
{
    private static readonly IOptions<StoreOptions> Store = Options.Create(new StoreOptions { Currency = "HNL" });
    private static readonly CreateShippingZoneRequestValidator Zone = new(Store);
    private static readonly SavePickupStoreRequestValidator PickupStore = new();

    private static CreateShippingZoneRequest Valid() =>
        new("central", "Central", 30m, "HNL", 1, 2, true, ["HN-FM", "HN-CM"]);

    [Fact]
    public void A_complete_zone_is_valid() => Assert.True(Zone.Validate(Valid()).IsValid);

    [Theory]
    [InlineData(-1)]
    [InlineData(10.999)]
    public void The_cost_cannot_be_negative_or_have_more_than_two_decimals(double cost) =>
        Assert.False(Zone.Validate(Valid() with { Cost = (decimal)cost }).IsValid);

    [Fact]
    public void Free_delivery_is_allowed() => Assert.True(Zone.Validate(Valid() with { Cost = 0m }).IsValid);

    [Fact]
    public void Only_the_store_currency_is_accepted() =>
        Assert.False(Zone.Validate(Valid() with { Currency = "USD" }).IsValid);

    [Fact]
    public void The_maximum_days_cannot_be_below_the_minimum() =>
        Assert.False(Zone.Validate(Valid() with { EstimatedDaysMin = 5, EstimatedDaysMax = 2 }).IsValid);

    [Fact]
    public void A_zone_needs_valid_departments_listed_once()
    {
        Assert.False(Zone.Validate(Valid() with { DepartmentCodes = [] }).IsValid);
        Assert.False(Zone.Validate(Valid() with { DepartmentCodes = ["HN-ZZ"] }).IsValid);
        Assert.False(Zone.Validate(Valid() with { DepartmentCodes = ["HN-FM", "hn-fm"] }).IsValid);
    }

    [Fact]
    public void A_pickup_store_needs_a_name_an_address_and_a_real_department()
    {
        var store = new SavePickupStoreRequest("Tienda Centro", "Blvd. Morazán", "Tegucigalpa", "HN-FM", null, "9-5");
        Assert.True(PickupStore.Validate(store).IsValid);
        Assert.False(PickupStore.Validate(store with { Name = "" }).IsValid);
        Assert.False(PickupStore.Validate(store with { DepartmentCode = "HN-ZZ" }).IsValid);
    }
}

public class ShippingQuotationTests
{
    private sealed class FakeUnitOfWork : IShippingUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(1);
    }

    private sealed class FakeZones : IShippingZoneRepository
    {
        public readonly List<ShippingZone> Zones = new();
        public void Add(ShippingZone zone) => Zones.Add(zone);
        public Task<ShippingZone?> GetByIdAsync(Guid id, CancellationToken ct = default) => Task.FromResult(Zones.FirstOrDefault(z => z.Id == id));
        public Task<bool> CodeExistsAsync(string code, CancellationToken ct = default) => Task.FromResult(Zones.Any(z => z.Code == code));
        public Task<IReadOnlyList<ShippingZone>> ListAsync(bool activeOnly, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<ShippingZone>>(Zones.Where(z => !activeOnly || z.IsActive).ToArray());
        public Task<ShippingZone?> GetActiveByDepartmentAsync(string departmentCode, CancellationToken ct = default) =>
            Task.FromResult(Zones.FirstOrDefault(z => z.IsActive && z.Departments.Any(d => d.DepartmentCode == departmentCode)));
        public Task<IReadOnlyList<string>> DepartmentsInOtherZonesAsync(IEnumerable<string> departmentCodes, Guid? exceptZoneId, CancellationToken ct = default)
        {
            var codes = departmentCodes.ToHashSet();
            return Task.FromResult<IReadOnlyList<string>>(Zones.Where(z => z.Id != exceptZoneId)
                .SelectMany(z => z.Departments).Select(d => d.DepartmentCode).Where(codes.Contains).ToArray());
        }
    }

    private sealed class FakeStores : IPickupStoreRepository
    {
        public readonly List<PickupStore> Stores = new();
        public void Add(PickupStore store) => Stores.Add(store);
        public Task<PickupStore?> GetByIdAsync(Guid id, CancellationToken ct = default) => Task.FromResult(Stores.FirstOrDefault(s => s.Id == id));
        public Task<IReadOnlyList<PickupStore>> ListAsync(bool activeOnly, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<PickupStore>>(Stores.Where(s => !activeOnly || s.IsActive).ToArray());
    }

    private readonly FakeZones _zones = new();
    private readonly FakeStores _stores = new();
    private readonly ShippingZoneService _service;

    public ShippingQuotationTests() =>
        _service = new ShippingZoneService(_zones, _stores, new FakeUnitOfWork(), TimeProvider.System);

    private static CreateShippingZoneRequest Zone(string code, decimal cost, params string[] departments) =>
        new(code, code, cost, "HNL", 1, 3, code == "central", departments);

    [Fact]
    public async Task The_cost_comes_from_the_zone_of_the_destination_department()
    {
        Assert.True((await _service.CreateAsync(Zone("central", 30m, "HN-FM", "HN-CM"))).IsSuccess);
        Assert.True((await _service.CreateAsync(Zone("norte", 100m, "HN-CR"))).IsSuccess);

        var central = await _service.QuoteDeliveryAsync("HN-FM");
        var north = await _service.QuoteDeliveryAsync("hn-cr");

        Assert.Equal(30m, central!.Cost);
        Assert.True(central.CashOnDeliveryCovered);
        Assert.Equal("Francisco Morazán", central.DepartmentName);
        Assert.Equal(100m, north!.Cost);
        Assert.False(north.CashOnDeliveryCovered);
    }

    [Fact]
    public async Task A_department_without_an_active_zone_or_an_unknown_code_has_no_quote()
    {
        var created = await _service.CreateAsync(Zone("central", 30m, "HN-FM"));
        Assert.Null(await _service.QuoteDeliveryAsync("HN-CR"));
        Assert.Null(await _service.QuoteDeliveryAsync("HN-ZZ"));

        await _service.SetZoneActiveAsync(created.Value.Id, false);
        Assert.Null(await _service.QuoteDeliveryAsync("HN-FM"));
    }

    [Fact]
    public async Task A_department_cannot_belong_to_two_zones()
    {
        await _service.CreateAsync(Zone("central", 30m, "HN-FM", "HN-CM"));

        var clash = await _service.CreateAsync(Zone("norte", 100m, "HN-CM", "HN-CR"));

        Assert.True(clash.IsFailure);
        Assert.Equal("shipping.zone.department_in_use", clash.Error.Code);
    }

    [Fact]
    public async Task A_zone_can_keep_its_own_departments_when_updated_and_codes_are_unique()
    {
        var created = await _service.CreateAsync(Zone("central", 30m, "HN-FM", "HN-CM"));
        var update = new UpdateShippingZoneRequest("Central", 35m, "HNL", 1, 2, true, ["HN-FM", "HN-LP"]);

        var updated = await _service.UpdateAsync(created.Value.Id, update);

        Assert.True(updated.IsSuccess);
        Assert.Equal(35m, (await _service.QuoteDeliveryAsync("HN-LP"))!.Cost);
        Assert.Null(await _service.QuoteDeliveryAsync("HN-CM"));

        var duplicate = await _service.CreateAsync(Zone("central", 10m, "HN-CR"));
        Assert.Equal("shipping.zone.code_in_use", duplicate.Error.Code);
    }

    [Fact]
    public async Task Only_active_pickup_stores_can_be_chosen()
    {
        var created = await _service.CreateStoreAsync(new SavePickupStoreRequest("Tienda Centro", "Blvd. Morazán", "Tegucigalpa", "HN-FM", null, null));
        var id = created.Value.Id;

        var store = await _service.GetPickupStoreAsync(id);
        Assert.Equal("Tienda Centro", store!.Name);
        Assert.Equal("Francisco Morazán", store.DepartmentName);

        await _service.SetStoreActiveAsync(id, false);
        Assert.Null(await _service.GetPickupStoreAsync(id));
        Assert.Null(await _service.GetPickupStoreAsync(Guid.NewGuid()));
        Assert.Empty(await _service.ListStoresAsync(activeOnly: true));
        Assert.Single(await _service.ListStoresAsync(activeOnly: false));
    }
}
