using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http.Extensions;

namespace LinuxLearner.Domain;

public record PaginationData(
    [property: JsonPropertyName("totalAmount")]
    int TotalAmount,
    [property: JsonPropertyName("totalPageAmount")]
    int TotalPageAmount,
    [property: JsonPropertyName("previousPageUrl")]
    string? PreviousPageUrl,
    [property: JsonPropertyName("nextPageUrl")]
    string? NextPageUrl)
{
    public static void Add(HttpContext httpContext, int totalAmount, int page, int pageSize)
    {
        string? previousPageUrl = null;
        if (page > 1)
        {
            previousPageUrl = httpContext.Request.GetEncodedUrl()
                .Replace($"page={page}", $"page={page - 1}");
        }

        string? nextPageUrl = null;
        if (pageSize * page < totalAmount)
        {
            nextPageUrl = httpContext.Request.GetEncodedUrl()
                .Replace($"page={page}", $"page={page + 1}");
        }

        var totalPageAmount = (totalAmount + pageSize - 1) / pageSize;
        var paginationData = new PaginationData(totalAmount, totalPageAmount, previousPageUrl, nextPageUrl);
        var paginationDataJson = JsonSerializer.Serialize(paginationData);

        httpContext.Response.Headers.Append("X-Pagination", paginationDataJson);
    }
}
