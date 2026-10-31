using Catrashop.Modules.Notifications.Application.Contracts;
using FluentValidation;

namespace Catrashop.Modules.Notifications.Application.Validation;

public sealed class SendNotificationRequestValidator : AbstractValidator<SendNotificationRequest>
{
    public SendNotificationRequestValidator()
    {
        RuleFor(x => x.RecipientId).NotEmpty();
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x.RecipientEmail).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.RecipientEmail));
        RuleFor(x => x.RecipientName).MaximumLength(150);
        RuleFor(x => x.IdempotencyKey).MaximumLength(200);
    }
}

public sealed class UpdateChannelPreferenceRequestValidator : AbstractValidator<UpdateChannelPreferenceRequest>
{
    public UpdateChannelPreferenceRequestValidator()
    {
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x.Channel).IsInEnum();
    }
}
