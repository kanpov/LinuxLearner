using System.Text.Json;
using FluentAssertions;
using LinuxLearner.Utilities;
using Microsoft.AspNetCore.Http;

namespace LinuxLearner.UnitTests;

public class PaginationDataTests
{
    [Fact]
    public void ShouldRecord_OnlyPage()
    {
        var httpContext = MakeContext(page: 1);
        PaginationData.Add(httpContext, totalAmount: 10, page: 1, pageSize: 10);
        AssertPaginationData(httpContext, paginationData =>
        {
            paginationData.PreviousPageUrl.Should().BeNull();
            paginationData.NextPageUrl.Should().BeNull();
            paginationData.TotalAmount.Should().Be(10);
            paginationData.TotalPageAmount.Should().Be(1);
        });
    }

    [Fact]
    public void ShouldRecord_FirstPage()
    {
        var httpContext = MakeContext(page: 1);
        PaginationData.Add(httpContext, totalAmount: 31, page: 1, pageSize: 10);
        AssertPaginationData(httpContext, paginationData =>
        {
            paginationData.PreviousPageUrl.Should().BeNull();
            paginationData.NextPageUrl.Should().Be(":///localhost:11111/request?page=2");
            paginationData.TotalAmount.Should().Be(31);
            paginationData.TotalPageAmount.Should().Be(4);
        });
    }

    [Fact]
    public void ShouldRecord_LastPage()
    {
        var httpContext = MakeContext(page: 4);
        PaginationData.Add(httpContext, totalAmount: 31, page: 4, pageSize: 10);
        AssertPaginationData(httpContext, paginationData =>
        {
            paginationData.PreviousPageUrl.Should().Be(":///localhost:11111/request?page=3");
            paginationData.NextPageUrl.Should().BeNull();
            paginationData.TotalAmount.Should().Be(31);
            paginationData.TotalPageAmount.Should().Be(4);
        });
    }

    [Fact]
    public void ShouldRecord_MiddlePage()
    {
        var httpContext = MakeContext(page: 2);
        PaginationData.Add(httpContext, totalAmount: 31, page: 2, pageSize: 10);
        AssertPaginationData(httpContext, paginationData =>
        {
            paginationData.PreviousPageUrl.Should().Be(":///localhost:11111/request?page=1");
            paginationData.NextPageUrl.Should().Be(":///localhost:11111/request?page=3");
            paginationData.TotalAmount.Should().Be(31);
            paginationData.TotalPageAmount.Should().Be(4);
        });
    }

    private static void AssertPaginationData(HttpContext httpContext, Action<PaginationData> assertion)
    {
        var json = httpContext.Response.Headers["X-Pagination"];
        json.Should().NotBeNull();
        
        var paginationData = JsonSerializer.Deserialize<PaginationData>(json!);
        paginationData.Should().NotBeNull();
        
        assertion(paginationData!);
    }
    
    private static DefaultHttpContext MakeContext(int page = 2)
    {
        return new DefaultHttpContext
        {
            Request =
            {
                PathBase = new PathString("/localhost:11111"),
                Path = new PathString("/request"),
                QueryString = new QueryString($"?page={page}")
            }
        };
    }
}