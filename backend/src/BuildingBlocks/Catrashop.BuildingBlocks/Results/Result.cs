namespace Catrashop.BuildingBlocks.Results;

/// <summary>
/// Outcome of an operation that can fail for expected, domain reasons. Using an explicit result
/// instead of exceptions keeps business rule violations off the exception path and forces callers
/// to handle failure. Exceptions remain for the truly exceptional (bugs, infrastructure faults).
/// </summary>
public class Result
{
    protected Result(bool isSuccess, Error error)
    {
        if (isSuccess && error != Error.None)
            throw new InvalidOperationException("A successful result cannot carry an error.");
        if (!isSuccess && error == Error.None)
            throw new InvalidOperationException("A failed result must carry an error.");

        IsSuccess = isSuccess;
        Error = error;
    }

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public Error Error { get; }

    public static Result Success() => new(true, Error.None);
    public static Result Failure(Error error) => new(false, error);

    public static Result<TValue> Success<TValue>(TValue value) => new(value, true, Error.None);
    public static Result<TValue> Failure<TValue>(Error error) => new(default, false, error);

    /// <summary>Lets a use case `return someError;` where a non-generic <see cref="Result"/> is expected.</summary>
    public static implicit operator Result(Error error) => Failure(error);
}

/// <summary>A <see cref="Result"/> that carries a value on success.</summary>
public sealed class Result<TValue> : Result
{
    private readonly TValue? _value;

    internal Result(TValue? value, bool isSuccess, Error error) : base(isSuccess, error) => _value = value;

    /// <summary>The value. Throws if accessed on a failed result — check <see cref="Result.IsSuccess"/> first.</summary>
    public TValue Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("The value of a failed result cannot be accessed.");

    public static implicit operator Result<TValue>(TValue value) => Success(value);
    public static implicit operator Result<TValue>(Error error) => Failure<TValue>(error);
}
