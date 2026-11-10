using System.Text.RegularExpressions;
using Catrashop.Modules.Notifications.Contracts;

namespace Catrashop.Modules.Notifications.Application.Templates;

/// <summary>
/// Recovers the data tokens of notifications saved before the tokens were stored, by reading them back out of the
/// text those notifications were created with (English first, then Spanish). That lets old rows be shown in the
/// reader's language too. Returns null when the text does not match, so the caller keeps the stored text.
/// </summary>
internal static class LegacyNotificationData
{
    private const RegexOptions Options = RegexOptions.CultureInvariant | RegexOptions.Singleline;

    private static readonly IReadOnlyDictionary<NotificationType, Regex[]> Patterns = new Dictionary<NotificationType, Regex[]>
    {
        [NotificationType.Welcome] = Build(@"^Hi (?<name>.*?), thanks", @"^Hola ?(?<name>.*?), gracias"),
        [NotificationType.OrderPlaced] = Build(@"^Your order (?<orderNumber>\S+) has", @"^Tu pedido (?<orderNumber>\S+) fue"),
        [NotificationType.OrderPaid] = Build(
            @"^Payment for order (?<orderNumber>\S+) was received\. Total: (?<total>\S*) (?<currency>\S*)\.$",
            @"^Recibimos el pago del pedido (?<orderNumber>\S+)\. Total: (?<total>\S*) (?<currency>\S*)\.$"),
        [NotificationType.PaymentFailed] = Build(@"for order (?<orderNumber>\S+)\.", @"del pedido (?<orderNumber>\S+)\."),
        [NotificationType.OrderShipped] = Build(
            @"^Order (?<orderNumber>\S+) has shipped\. Track it with (?<trackingNumber>.*?)\.$",
            @"^El pedido (?<orderNumber>\S+) fue enviado\. Rastréalo con el número (?<trackingNumber>.*?)\.$"),
        [NotificationType.OrderDelivered] = Build(@"^Order (?<orderNumber>\S+) was delivered", @"^El pedido (?<orderNumber>\S+) fue entregado"),
        [NotificationType.OrderCancelled] = Build(
            @"^Order (?<orderNumber>\S+) has been cancelled\.\s*(?<reason>.*)$",
            @"^El pedido (?<orderNumber>\S+) fue cancelado\.\s*(?<reason>.*)$"),
        [NotificationType.RefundCompleted] = Build(@"for order (?<orderNumber>\S+) has", @"del pedido (?<orderNumber>\S+)\."),
        [NotificationType.BackInStock] = Build(@"^(?<productName>.+) is available again\.$", @"^(?<productName>.+) está disponible de nuevo\.$"),
    };

    /// <summary>Types whose text has no tokens at all, so any stored language can be re-rendered as is.</summary>
    private static readonly HashSet<NotificationType> TokenFree = [NotificationType.PasswordReset];

    public static IReadOnlyDictionary<string, string>? TryExtract(NotificationType type, string body)
    {
        if (TokenFree.Contains(type)) return new Dictionary<string, string>();
        if (!Patterns.TryGetValue(type, out var patterns)) return null;

        foreach (var pattern in patterns)
        {
            var match = pattern.Match(body);
            if (!match.Success) continue;

            var data = new Dictionary<string, string>();
            foreach (var name in pattern.GetGroupNames())
                if (name != "0" && match.Groups[name].Success)
                    data[name] = match.Groups[name].Value;
            return data;
        }

        return null;
    }

    private static Regex[] Build(params string[] patterns) =>
        patterns.Select(p => new Regex(p, Options)).ToArray();
}
