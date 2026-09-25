namespace CommerceHub.BuildingBlocks.Results;

/// <summary>
/// A machine-readable failure. <see cref="Code"/> is a stable, dotted identifier
/// (e.g. "catalog.product.not_found") that clients and logs can key on; <see cref="Message"/>
/// is a human-readable description. <see cref="Type"/> lets the API map failures to the right
/// HTTP status without leaking internals.
/// </summary>
public sealed record Error(string Code, string Message, ErrorType Type = ErrorType.Failure)
{
    /// <summary>Sentinel used by successful results.</summary>
    public static readonly Error None = new(string.Empty, string.Empty, ErrorType.Failure);

    public static Error NotFound(string code, string message) => new(code, message, ErrorType.NotFound);
    public static Error Validation(string code, string message) => new(code, message, ErrorType.Validation);
    public static Error Conflict(string code, string message) => new(code, message, ErrorType.Conflict);
    public static Error Unauthorized(string code, string message) => new(code, message, ErrorType.Unauthorized);
    public static Error Forbidden(string code, string message) => new(code, message, ErrorType.Forbidden);
}

/// <summary>Coarse classification of a failure, mapped to HTTP status codes at the API edge.</summary>
public enum ErrorType
{
    Failure = 0,
    Validation = 1,
    NotFound = 2,
    Conflict = 3,
    Unauthorized = 4,
    Forbidden = 5
}
