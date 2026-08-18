using Blizka.Api.Common;

namespace Blizka.IntegrationTests.Common;

public sealed class PaginatedResponseTests
{
    [Theory]
    [InlineData(1, 20, 20, false)]
    [InlineData(1, 20, 21, true)]
    [InlineData(2, 20, 40, false)]
    [InlineData(2, 20, 41, true)]
    [InlineData(1, 20, 0, false)]
    public void HasMore_reflects_whether_the_current_page_covers_the_total(int page, int pageSize, int totalCount, bool expectedHasMore)
    {
        var response = new PaginatedResponse<int>(Items: [], page, pageSize, totalCount);

        Assert.Equal(expectedHasMore, response.HasMore);
    }
}
