using Microsoft.AspNetCore.Http;
using Tameru.SharedKernel.Results;
using Tameru.Web.Common.Contracts;
using HttpResults = Microsoft.AspNetCore.Http.Results;

namespace Tameru.Web.Common.Results;

/// <summary>Maps a domain <see cref="Result"/> to an HTTP response using the standard envelope.</summary>
public static class ResultHttpExtensions
{
    public static IResult ToHttp<T>(this Result<T> result) =>
        result.IsSuccess
            ? HttpResults.Ok(ApiResponse<T>.Ok(result.Value))
            : Fail(result.Error);

    public static IResult ToHttp(this Result result) =>
        result.IsSuccess
            ? HttpResults.Ok(ApiResponse.Ok())
            : Fail(result.Error);

    private static IResult Fail(Error error)
    {
        var status = StatusFor(error.Code);
        var payload = ApiResponse.Fail(error.Message, new ApiError { Code = error.Code });
        return HttpResults.Json(payload, statusCode: status);
    }

    private static int StatusFor(string code) => code switch
    {
        "validation_error" => StatusCodes.Status400BadRequest,
        "unauthenticated" or "invalid_credentials" or "invalid_refresh_token"
            => StatusCodes.Status401Unauthorized,
        "forbidden" => StatusCodes.Status403Forbidden,
        "not_found" => StatusCodes.Status404NotFound,
        "conflict" => StatusCodes.Status409Conflict,
        _ => StatusCodes.Status422UnprocessableEntity,
    };
}
