using Catrashop.BuildingBlocks.Messaging;
using Catrashop.BuildingBlocks.Results;
using Catrashop.Modules.Orders.Contracts;
using Catrashop.Modules.Payments.Application.Abstractions;
using Catrashop.Modules.Payments.Application.Configuration;
using Catrashop.Modules.Payments.Application.Services;
using Catrashop.Modules.Payments.Domain;
using Microsoft.Extensions.Options;
using Xunit;

namespace Catrashop.Modules.Payments.Tests;

/// <summary>PR-051: the cash-on-delivery, bank-transfer and pay-at-store flows of <see cref="PaymentService"/>.</summary>
public class ManualPaymentTests
{
    private static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 1, 2, 3];
    private static readonly byte[] Exe = [0x4D, 0x5A, 0x90, 0x00, 1, 2, 3];

    private sealed class Harness
    {
        public readonly Guid CustomerId = Guid.NewGuid();
        public readonly FakeOrders Orders = new();
        public readonly FakePayments Payments = new();
        public readonly FakeReceipts Receipts = new();
        public readonly PaymentService Service;

        public Harness()
        {
            var options = Options.Create(new PaymentsOptions
            {
                MaxReceiptBytes = 1024,
                BankAccounts = [new BankAccountOption { Code = "BAC-1", Bank = "BAC", Holder = "Catrashop", AccountNumber = "123", AccountType = "Ahorro" }]
            });
            Service = new PaymentService(Payments, new NoProvider(), Orders, new NoEvents(), new NoUnitOfWork(),
                TimeProvider.System, Receipts, options);
        }

        public Guid NewOrder(string? method, string status = "PaymentPending")
        {
            var id = Guid.NewGuid();
            Orders.Orders[id] = new FakeOrder(CustomerId, status, method);
            return id;
        }
    }

    // --- Cash on delivery / pay at the store --------------------------------------------------

    [Theory]
    [InlineData(PaymentMethodCodes.CashOnDelivery)]
    [InlineData(PaymentMethodCodes.PayAtStore)]
    public async Task Collected_later_methods_accept_the_order_without_charging_anything(string method)
    {
        var h = new Harness();
        var orderId = h.NewOrder(method);

        var result = await h.Service.InitiateAsync(h.CustomerId, orderId, null);

        Assert.True(result.IsSuccess);
        Assert.Equal("Pending", result.Value.Status); // not paid
        Assert.Equal(method, result.Value.Method);
        Assert.Equal("Confirmed", h.Orders.Orders[orderId].Status);
        Assert.Equal(0, h.Orders.PaidCalls);
    }

    [Fact]
    public async Task Retrying_a_collected_later_payment_returns_the_same_payment()
    {
        var h = new Harness();
        var orderId = h.NewOrder(PaymentMethodCodes.CashOnDelivery);

        var first = await h.Service.InitiateAsync(h.CustomerId, orderId, null);
        var second = await h.Service.InitiateAsync(h.CustomerId, orderId, null);

        Assert.Equal(first.Value.Id, second.Value.Id);
        Assert.Single(h.Payments.All);
    }

    [Fact]
    public async Task The_money_is_collected_only_after_the_order_is_delivered()
    {
        var h = new Harness();
        var orderId = h.NewOrder(PaymentMethodCodes.CashOnDelivery);
        var payment = (await h.Service.InitiateAsync(h.CustomerId, orderId, null)).Value;

        var early = await h.Service.CollectAsync(payment.Id);
        Assert.True(early.IsFailure);
        Assert.Equal("payments.order_not_delivered", early.Error.Code);

        h.Orders.Orders[orderId] = h.Orders.Orders[orderId] with { Status = "Delivered" };
        var collected = await h.Service.CollectAsync(payment.Id);

        Assert.True(collected.IsSuccess);
        Assert.Equal("Captured", collected.Value.Status);
        Assert.True((await h.Service.CollectAsync(payment.Id)).IsSuccess); // idempotent
    }

    [Fact]
    public async Task A_transfer_cannot_be_registered_as_collected()
    {
        var h = new Harness();
        var orderId = h.NewOrder(PaymentMethodCodes.BankTransfer);
        var payment = (await h.Service.InitiateAsync(h.CustomerId, orderId, null)).Value;

        var result = await h.Service.CollectAsync(payment.Id);

        Assert.Equal("payments.not_collected_later", result.Error.Code);
    }

    // --- Bank transfer ------------------------------------------------------------------------

    [Fact]
    public async Task A_transfer_stays_awaiting_payment_until_a_receipt_is_verified()
    {
        var h = new Harness();
        var orderId = h.NewOrder(PaymentMethodCodes.BankTransfer);

        var payment = (await h.Service.InitiateAsync(h.CustomerId, orderId, null)).Value;

        Assert.Equal("Pending", payment.Status);
        Assert.Equal("PaymentPending", h.Orders.Orders[orderId].Status); // not accepted, not paid
        Assert.Equal(0, h.Orders.PaidCalls);
    }

    [Fact]
    public async Task Submitting_a_receipt_puts_the_payment_under_review_and_approving_it_pays_the_order()
    {
        var h = new Harness();
        var orderId = h.NewOrder(PaymentMethodCodes.BankTransfer);
        var payment = (await h.Service.InitiateAsync(h.CustomerId, orderId, null)).Value;

        var submitted = await h.Service.SubmitTransferProofAsync(h.CustomerId, payment.Id, "BAC-1", " 99887 ", Png);
        Assert.True(submitted.IsSuccess);
        Assert.Equal("UnderReview", submitted.Value.Status);
        Assert.Equal("99887", submitted.Value.TransferReference);
        Assert.True(submitted.Value.HasReceipt);
        Assert.Single(h.Receipts.Files);
        Assert.Equal(0, h.Orders.PaidCalls); // a receipt alone never pays an order

        var approved = await h.Service.ApproveTransferAsync(payment.Id);

        Assert.True(approved.IsSuccess);
        Assert.Equal("Captured", approved.Value.Status);
        Assert.Equal(1, h.Orders.PaidCalls);
        Assert.Equal("Paid", h.Orders.Orders[orderId].Status);
    }

    [Fact]
    public async Task A_rejected_receipt_goes_back_to_the_customer_who_can_send_another()
    {
        var h = new Harness();
        var orderId = h.NewOrder(PaymentMethodCodes.BankTransfer);
        var payment = (await h.Service.InitiateAsync(h.CustomerId, orderId, null)).Value;
        await h.Service.SubmitTransferProofAsync(h.CustomerId, payment.Id, "BAC-1", "111", Png);

        var rejected = await h.Service.RejectTransferAsync(payment.Id, " The amount does not match ");

        Assert.Equal("Pending", rejected.Value.Status);
        Assert.Equal("The amount does not match", rejected.Value.ReviewNote);
        Assert.Equal(0, h.Orders.PaidCalls);

        var again = await h.Service.SubmitTransferProofAsync(h.CustomerId, payment.Id, "BAC-1", "222", Png);
        Assert.True(again.IsSuccess);
        Assert.Null(again.Value.ReviewNote);
        Assert.Single(h.Receipts.Files); // the replaced receipt was deleted
    }

    [Fact]
    public async Task Approving_without_a_submitted_receipt_is_refused()
    {
        var h = new Harness();
        var payment = (await h.Service.InitiateAsync(h.CustomerId, h.NewOrder(PaymentMethodCodes.BankTransfer), null)).Value;

        var result = await h.Service.ApproveTransferAsync(payment.Id);

        Assert.Equal("payments.not_under_review", result.Error.Code);
        Assert.Equal(0, h.Orders.PaidCalls);
    }

    [Fact]
    public async Task A_cancelled_order_cannot_be_paid_by_approving_its_transfer()
    {
        var h = new Harness();
        var orderId = h.NewOrder(PaymentMethodCodes.BankTransfer);
        var payment = (await h.Service.InitiateAsync(h.CustomerId, orderId, null)).Value;
        await h.Service.SubmitTransferProofAsync(h.CustomerId, payment.Id, "BAC-1", "111", Png);
        h.Orders.Orders[orderId] = h.Orders.Orders[orderId] with { Status = "Cancelled" };

        var result = await h.Service.ApproveTransferAsync(payment.Id);

        Assert.Equal("payments.order_not_payable", result.Error.Code);
        Assert.Equal(0, h.Orders.PaidCalls);
    }

    [Theory]
    [InlineData("NOPE", "111", "payments.unknown_bank_account")]
    [InlineData(null, "111", "payments.unknown_bank_account")]
    [InlineData("BAC-1", " ", "payments.reference_required")]
    [InlineData("BAC-1", null, "payments.reference_required")]
    public async Task The_bank_account_and_the_reference_are_required(string? bank, string? reference, string code)
    {
        var h = new Harness();
        var payment = (await h.Service.InitiateAsync(h.CustomerId, h.NewOrder(PaymentMethodCodes.BankTransfer), null)).Value;

        var result = await h.Service.SubmitTransferProofAsync(h.CustomerId, payment.Id, bank, reference, Png);

        Assert.Equal(code, result.Error.Code);
        Assert.Empty(h.Receipts.Files);
    }

    [Fact]
    public async Task The_receipt_must_be_an_image_or_a_pdf_within_the_size_limit()
    {
        var h = new Harness();
        var payment = (await h.Service.InitiateAsync(h.CustomerId, h.NewOrder(PaymentMethodCodes.BankTransfer), null)).Value;
        var big = Png.Concat(new byte[2000]).ToArray();

        Assert.Equal("payments.receipt_required", (await h.Service.SubmitTransferProofAsync(h.CustomerId, payment.Id, "BAC-1", "1", [])).Error.Code);
        Assert.Equal("payments.receipt_unsupported", (await h.Service.SubmitTransferProofAsync(h.CustomerId, payment.Id, "BAC-1", "1", Exe)).Error.Code);
        Assert.Equal("payments.receipt_too_large", (await h.Service.SubmitTransferProofAsync(h.CustomerId, payment.Id, "BAC-1", "1", big)).Error.Code);
        Assert.Empty(h.Receipts.Files);

        var pdf = "%PDF-1.7 ..."u8.ToArray();
        Assert.True((await h.Service.SubmitTransferProofAsync(h.CustomerId, payment.Id, "BAC-1", "1", pdf)).IsSuccess);
    }

    [Fact]
    public async Task Only_the_owner_can_attach_a_receipt_and_only_to_a_transfer()
    {
        var h = new Harness();
        var transfer = (await h.Service.InitiateAsync(h.CustomerId, h.NewOrder(PaymentMethodCodes.BankTransfer), null)).Value;
        var cash = (await h.Service.InitiateAsync(h.CustomerId, h.NewOrder(PaymentMethodCodes.CashOnDelivery), null)).Value;

        var stranger = await h.Service.SubmitTransferProofAsync(Guid.NewGuid(), transfer.Id, "BAC-1", "1", Png);
        var wrongMethod = await h.Service.SubmitTransferProofAsync(h.CustomerId, cash.Id, "BAC-1", "1", Png);

        Assert.Equal("payments.not_found", stranger.Error.Code);
        Assert.Equal("payments.not_a_transfer", wrongMethod.Error.Code);
    }

    [Fact]
    public async Task The_customer_cannot_replace_a_receipt_that_is_already_under_review()
    {
        var h = new Harness();
        var payment = (await h.Service.InitiateAsync(h.CustomerId, h.NewOrder(PaymentMethodCodes.BankTransfer), null)).Value;
        await h.Service.SubmitTransferProofAsync(h.CustomerId, payment.Id, "BAC-1", "1", Png);

        var again = await h.Service.SubmitTransferProofAsync(h.CustomerId, payment.Id, "BAC-1", "2", Png);

        Assert.Equal("payments.not_awaiting_proof", again.Error.Code);
    }

    [Fact]
    public async Task The_queue_lists_the_transfers_waiting_for_verification()
    {
        var h = new Harness();
        var waiting = (await h.Service.InitiateAsync(h.CustomerId, h.NewOrder(PaymentMethodCodes.BankTransfer), null)).Value;
        await h.Service.SubmitTransferProofAsync(h.CustomerId, waiting.Id, "BAC-1", "1", Png);
        await h.Service.InitiateAsync(h.CustomerId, h.NewOrder(PaymentMethodCodes.BankTransfer), null);

        var queue = await h.Service.ListAsync("UnderReview", null);

        Assert.Equal(waiting.Id, Assert.Single(queue).Id);
    }

    [Fact]
    public void Payment_state_machine_models_the_review()
    {
        Assert.True(PaymentStateMachine.CanTransition(PaymentStatus.Pending, PaymentStatus.UnderReview));
        Assert.True(PaymentStateMachine.CanTransition(PaymentStatus.UnderReview, PaymentStatus.Captured));
        Assert.True(PaymentStateMachine.CanTransition(PaymentStatus.UnderReview, PaymentStatus.Pending));
        Assert.False(PaymentStateMachine.CanTransition(PaymentStatus.UnderReview, PaymentStatus.Refunded));
        Assert.False(PaymentStateMachine.CanTransition(PaymentStatus.Captured, PaymentStatus.UnderReview));
    }

    // --- Fakes ----------------------------------------------------------------------------------

    private sealed record FakeOrder(Guid CustomerId, string Status, string? Method);

    private sealed class FakeOrders : IOrderWorkflow
    {
        public readonly Dictionary<Guid, FakeOrder> Orders = new();
        public int PaidCalls;

        public Task<PayableOrder?> GetPayableAsync(Guid orderId, CancellationToken ct = default)
        {
            if (!Orders.TryGetValue(orderId, out var o)) return Task.FromResult<PayableOrder?>(null);
            var awaiting = o.Status is "Pending" or "PaymentPending" or "PaymentFailed";
            return Task.FromResult<PayableOrder?>(new PayableOrder(orderId, o.CustomerId, "HNL", 250m, awaiting, o.Status == "Paid", o.Status, o.Method));
        }

        public Task<Result> ConfirmPaymentAsync(Guid orderId, CancellationToken ct = default)
        {
            if (Orders[orderId].Status is "PaymentPending" or "PaymentFailed" or "Pending")
            {
                Orders[orderId] = Orders[orderId] with { Status = "Paid" };
                PaidCalls++;
            }
            return Task.FromResult(Result.Success());
        }

        public Task<Result> ConfirmWithoutPaymentAsync(Guid orderId, CancellationToken ct = default)
        {
            if (Orders[orderId].Status == "PaymentPending")
                Orders[orderId] = Orders[orderId] with { Status = "Confirmed" };
            return Task.FromResult(Result.Success());
        }

        public Task<Result> FailPaymentAsync(Guid orderId, string? reason, CancellationToken ct = default) => Task.FromResult(Result.Success());
        public Task<Result> RefundAsync(Guid orderId, CancellationToken ct = default) => Task.FromResult(Result.Success());
    }

    private sealed class FakePayments : IPaymentRepository
    {
        public readonly List<Payment> All = [];
        public void Add(Payment payment) => All.Add(payment);
        public Task<Payment?> GetByIdAsync(Guid id, CancellationToken ct = default) => Task.FromResult(All.FirstOrDefault(p => p.Id == id));
        public Task<Payment?> GetByIdempotencyKeyAsync(string key, CancellationToken ct = default) => Task.FromResult(All.FirstOrDefault(p => p.IdempotencyKey == key));
        public Task<Payment?> GetByProviderReferenceAsync(string r, CancellationToken ct = default) => Task.FromResult(All.FirstOrDefault(p => p.ProviderReference == r));
        public Task<Payment?> GetLatestForOrderAsync(Guid orderId, CancellationToken ct = default) => Task.FromResult(All.LastOrDefault(p => p.OrderId == orderId));
        public Task<IReadOnlyList<Payment>> ListAsync(PaymentStatus? status, string? method, int take, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<Payment>>(All.Where(p => (status is null || p.Status == status) && (method is null || p.Method == method)).Take(take).ToList());
        public Task<bool> WebhookEventExistsAsync(string eventId, CancellationToken ct = default) => Task.FromResult(false);
        public void AddWebhookEvent(ProcessedWebhookEvent webhookEvent) { }
    }

    private sealed class FakeReceipts : IReceiptStore
    {
        public readonly Dictionary<string, byte[]> Files = new();
        public Task<string> SaveAsync(Guid paymentId, byte[] content, string extension, CancellationToken ct = default)
        {
            var key = $"{paymentId:N}-{Files.Count}-{Guid.NewGuid():N}{extension}";
            Files[key] = content;
            return Task.FromResult(key);
        }
        public Task<Stream?> OpenAsync(string key, CancellationToken ct = default) =>
            Task.FromResult<Stream?>(Files.TryGetValue(key, out var b) ? new MemoryStream(b) : null);
        public Task DeleteAsync(string key, CancellationToken ct = default) { Files.Remove(key); return Task.CompletedTask; }
    }

    private sealed class NoProvider : IPaymentProvider
    {
        public string Name => "none";
        public Task<PaymentProviderResult> AuthorizeAndCaptureAsync(PaymentRequest request, CancellationToken ct = default) =>
            throw new InvalidOperationException("A manual method must never reach the gateway.");
        public Task<PaymentProviderResult> RefundAsync(string providerReference, decimal amount, CancellationToken ct = default) =>
            throw new InvalidOperationException("A manual payment is never refunded through the gateway.");
    }

    private sealed class NoEvents : IIntegrationEventPublisher
    {
        public void Enqueue(IIntegrationEvent integrationEvent, Guid? correlationId = null) { }
    }

    private sealed class NoUnitOfWork : IPaymentUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);
    }
}
