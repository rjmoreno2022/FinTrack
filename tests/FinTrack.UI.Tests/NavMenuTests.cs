using Bunit;
using Bunit.TestDoubles;
using FinTrack.Web.Components.Layout;
using FluentAssertions;
using MudBlazor.Services;
using Xunit;

namespace FinTrack.UI.Tests;

public class NavMenuTests : TestContext
{
    public NavMenuTests()
    {
        Services.AddMudServices();
    }

    [Fact]
    public void NavMenu_WhenRendered_ShouldDisplayAllMainNavigationLinks()
    {
        // Arrange
        var authContext = this.AddTestAuthorization();
        authContext.SetAuthorized("testuser@fintrack.com");

        // Act
        var cut = RenderComponent<NavMenu>();

        // Assert
        var markup = cut.Markup;
        markup.Should().Contain("Dashboard");
        markup.Should().Contain("Cuentas");
        markup.Should().Contain("Transacciones");
        markup.Should().Contain("Categorías");
        markup.Should().Contain("Reportes & Analítica");
        markup.Should().Contain("Divisas & Tasas");
        markup.Should().Contain("Presupuestos");
        markup.Should().Contain("Metas de Ahorro");
        markup.Should().Contain("Deudas");
    }

    [Fact]
    public void NavMenu_ForRegularUser_ShouldNotShowAdminSwaggerLink()
    {
        // Arrange
        var authContext = this.AddTestAuthorization();
        authContext.SetAuthorized("user@fintrack.com");

        // Act
        var cut = RenderComponent<NavMenu>();

        // Assert
        cut.Markup.Should().NotContain("API REST (Swagger)");
    }

    [Fact]
    public void NavMenu_ForAdminUser_ShouldShowAdminSwaggerLink()
    {
        // Arrange
        var authContext = this.AddTestAuthorization();
        authContext.SetAuthorized("admin@fintrack.com");
        authContext.SetRoles("Admin");

        // Act
        var cut = RenderComponent<NavMenu>();

        // Assert
        cut.Markup.Should().Contain("API REST (Swagger)");
    }
}
