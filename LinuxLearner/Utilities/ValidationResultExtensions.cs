using FluentValidation.Results;
using Microsoft.AspNetCore.Http.HttpResults;

namespace LinuxLearner.Utilities;

public static class ValidationResultExtensions
{
    public static ValidationProblem ToProblem(this ValidationResult validationResult, HttpContext httpContext)
    {
        var errors = validationResult.ToDictionary();
        var errorAmount = errors
            .Select(x => x.Value.Length)
            .Sum();

        var title = errorAmount == 1 ? "A validation error occurred" : $"{errorAmount} validation errors occurred";
        var detail = errorAmount == 1
            ? "See the \"errors\" field for details about the error"
            : "See the \"errors\" field for details about each error";

        return TypedResults.ValidationProblem(
            errors,
            detail: detail,
            instance: httpContext.Request.Path,
            title: title);
    }
}