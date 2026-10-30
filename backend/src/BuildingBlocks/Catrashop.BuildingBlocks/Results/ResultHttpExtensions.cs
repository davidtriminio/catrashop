using Microsoft.AspNetCore.Http;
using HttpResults = Microsoft.AspNetCore.Http.Results;

namespace Catrashop.BuildingBlocks.Results;

/// <summary>
/// Maps a domain <see cref="Result"/> to an HTTP response. Failures become RFC 7807 ProblemDetails with
/// a status derived from <see cref="ErrorType"/> and the stable error <c>code</c> in an extension, so
/// every module's API reports failures identically.
/// </summary>
public static class ResultHttpExtensions
{
    public static IResult ToProblem(this Error error)
    {
        var status = error.Type switch
        {
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            ErrorType.Forbidden => StatusCodes.Status403Forbidden,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status400BadRequest
        };

        return HttpResults.Problem(
            detail: error.Message,
            statusCode: status,
            extensions: new Dictionary<string, object?> { ["code"] = error.Code });
    }

    /// <summary>200 with the value on success, else a ProblemDetails.</summary>
    public static IResult ToHttpResult<T>(this Result<T> result) =>
        result.IsSuccess ? HttpResults.Ok(result.Value) : result.Error.ToProblem();

    /// <summary>Custom success projection (e.g. 201 Created), else a ProblemDetails.</summary>
    public static IResult ToHttpResult<T>(this Result<T> result, Func<T, IResult> onSuccess) =>
        result.IsSuccess ? onSuccess(result.Value) : result.Error.ToProblem();

    /// <summary>200 (empty) on success, else a ProblemDetails.</summary>
    public static IResult ToHttpResult(this Result result) =>
        result.IsSuccess ? HttpResults.Ok() : result.Error.ToProblem();
}
