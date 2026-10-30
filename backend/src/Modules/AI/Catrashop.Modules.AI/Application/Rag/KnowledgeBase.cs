using Catrashop.Modules.AI.Application.Abstractions;

namespace Catrashop.Modules.AI.Application.Rag;

/// <summary>
/// The seeded store-support knowledge base for RAG. Non-PII policy/help content only. In a real deployment this
/// would be backed by a vector store; the content and the <see cref="IKnowledgeRetriever"/> seam stay the same.
/// </summary>
public static class KnowledgeBase
{
    public static readonly IReadOnlyList<(string Title, string Content)> Documents = new List<(string, string)>
    {
        ("Shipping",
            "We offer standard and express shipping. Standard delivery typically takes 3–5 business days; express " +
            "takes 1–2 business days. Shipping costs and estimates are shown at checkout and depend on the method chosen."),
        ("Returns & refunds",
            "You can return most items within 30 days of delivery for a refund to your original payment method. " +
            "Items must be unused and in their original packaging. Start a return from your order's page; refunds " +
            "are processed after we receive the item."),
        ("Payments",
            "We accept major credit and debit cards. Payment is captured when your order is confirmed. If a payment " +
            "fails, the order stays unpaid and you can retry from your orders page."),
        ("Order tracking",
            "Once your order ships you receive a tracking number. You can track a shipment from your order's page or " +
            "the public tracking page; statuses move Pending → Shipped → Delivered."),
        ("Coupons & discounts",
            "Enter a coupon code at checkout to apply a discount. Coupons may have a minimum order value, an expiry " +
            "date, and per-customer or total usage limits. Only one coupon can be applied per order."),
        ("Accounts",
            "Create an account to check out, save addresses, keep a wishlist, and view your order history and " +
            "notifications. You can manage addresses and marketing preferences from your account page."),
    };
}
