using CommerceHub.Modules.Inventory.Domain;
using Xunit;

namespace CommerceHub.Modules.Inventory.Tests;

public class InventoryItemTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 4, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Expiry = Now.AddMinutes(15);

    private static InventoryItem NewItem(int onHand, int threshold = 0) =>
        InventoryItem.Create("SKU-1", onHand, threshold, Now);

    // --- Overselling prevention -----------------------------------------------------------------

    [Fact]
    public void Reserve_succeeds_when_enough_available()
    {
        var item = NewItem(10);

        var result = item.Reserve(Guid.NewGuid(), 4, Now, Expiry);

        Assert.True(result.IsSuccess);
        Assert.Equal(4, item.QuantityReserved);
        Assert.Equal(6, item.AvailableQuantity);
        Assert.Equal(10, item.QuantityOnHand);
    }

    [Fact]
    public void Reserve_fails_when_insufficient_available()
    {
        var item = NewItem(3);

        var result = item.Reserve(Guid.NewGuid(), 5, Now, Expiry);

        Assert.True(result.IsFailure);
        Assert.Equal("inventory.insufficient_stock", result.Error.Code);
        Assert.Equal(0, item.QuantityReserved); // nothing was held
    }

    [Fact]
    public void Reserve_cannot_exceed_available_across_multiple_reservations()
    {
        var item = NewItem(10);
        Assert.True(item.Reserve(Guid.NewGuid(), 7, Now, Expiry).IsSuccess);

        var second = item.Reserve(Guid.NewGuid(), 5, Now, Expiry); // only 3 left

        Assert.True(second.IsFailure);
        Assert.Equal(7, item.QuantityReserved);
    }

    // --- Idempotency ----------------------------------------------------------------------------

    [Fact]
    public void Reserve_is_idempotent_for_the_same_reference()
    {
        var item = NewItem(10);
        var reference = Guid.NewGuid();

        Assert.True(item.Reserve(reference, 4, Now, Expiry).IsSuccess);
        Assert.True(item.Reserve(reference, 4, Now, Expiry).IsSuccess); // retry

        Assert.Equal(4, item.QuantityReserved); // not doubled
        Assert.Single(item.Reservations);
    }

    [Fact]
    public void Release_is_idempotent_and_safe_for_unknown_reference()
    {
        var item = NewItem(10);
        var reference = Guid.NewGuid();
        item.Reserve(reference, 4, Now, Expiry);

        Assert.True(item.Release(reference, Now).IsSuccess);
        Assert.True(item.Release(reference, Now).IsSuccess); // repeat
        Assert.True(item.Release(Guid.NewGuid(), Now).IsSuccess); // never existed

        Assert.Equal(0, item.QuantityReserved);
    }

    // --- Reservation lifecycle ------------------------------------------------------------------

    [Fact]
    public void Commit_reduces_on_hand_and_reserved()
    {
        var item = NewItem(10);
        var reference = Guid.NewGuid();
        item.Reserve(reference, 4, Now, Expiry);

        var result = item.Commit(reference, Now);

        Assert.True(result.IsSuccess);
        Assert.Equal(6, item.QuantityOnHand);
        Assert.Equal(0, item.QuantityReserved);
        Assert.Equal(6, item.AvailableQuantity);
    }

    [Fact]
    public void Release_frees_reserved_stock_back_to_available()
    {
        var item = NewItem(10);
        var reference = Guid.NewGuid();
        item.Reserve(reference, 4, Now, Expiry);

        item.Release(reference, Now);

        Assert.Equal(0, item.QuantityReserved);
        Assert.Equal(10, item.AvailableQuantity);
    }

    [Fact]
    public void Expired_reservations_are_released_by_the_sweep()
    {
        var item = NewItem(10);
        item.Reserve(Guid.NewGuid(), 4, Now, Now.AddMinutes(5));

        var released = item.ReleaseExpiredReservations(Now.AddMinutes(10));

        Assert.Equal(1, released);
        Assert.Equal(0, item.QuantityReserved);
    }

    // --- Adjustments ----------------------------------------------------------------------------

    [Fact]
    public void Adjust_cannot_drop_on_hand_below_reserved()
    {
        var item = NewItem(10);
        item.Reserve(Guid.NewGuid(), 8, Now, Expiry);

        var result = item.Adjust(-5, "damage", Now); // would leave 5 on-hand < 8 reserved

        Assert.True(result.IsFailure);
        Assert.Equal("inventory.adjustment_below_reserved", result.Error.Code);
        Assert.Equal(10, item.QuantityOnHand);
    }

    [Fact]
    public void Receive_increases_on_hand_and_available()
    {
        var item = NewItem(2);

        Assert.True(item.Receive(8, "restock", Now).IsSuccess);

        Assert.Equal(10, item.QuantityOnHand);
        Assert.Equal(10, item.AvailableQuantity);
    }

    // --- Low-stock notification -----------------------------------------------------------------

    [Fact]
    public void Low_stock_notifies_once_per_downward_crossing()
    {
        var item = NewItem(10, threshold: 5);
        item.Reserve(Guid.NewGuid(), 6, Now, Expiry); // available 4 <= 5

        Assert.True(item.ShouldNotifyLowStock());   // first time → notify
        Assert.False(item.ShouldNotifyLowStock());  // still low → don't spam
    }

    [Fact]
    public void Low_stock_flag_resets_when_stock_recovers()
    {
        var item = NewItem(10, threshold: 5);
        item.Reserve(Guid.NewGuid(), 6, Now, Expiry);
        Assert.True(item.ShouldNotifyLowStock());

        item.Receive(10, "restock", Now); // available now 14 > 5
        Assert.False(item.ShouldNotifyLowStock()); // resets, no notification

        // Drop again → should notify again on the new crossing.
        item.Reserve(Guid.NewGuid(), 10, Now, Expiry);
        Assert.True(item.ShouldNotifyLowStock());
    }
}
