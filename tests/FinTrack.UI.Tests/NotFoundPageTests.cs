using Bunit;
using FinTrack.Web.Components.Pages;
using FluentAssertions;
using Xunit;

namespace FinTrack.UI.Tests;

public class NotFoundPageTests : TestContext
{
    [Fact]
    public void NotFoundPage_ShouldRenderNotFoundMessage()
    {
        // Act
        var cut = RenderComponent<NotFound>();

        // Assert
        cut.Find("h3").TextContent.Should().Be("Not Found");
        cut.Find("p").TextContent.Should().Contain("Sorry, the content you are looking for does not exist.");
    }
}
