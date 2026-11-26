namespace Vibe.UI.Tests.Components.Fixtures;

/// <summary>
/// <c>NullableBindingFixture.razor</c> binds nullable fields to the typed inputs without stating
/// <c>TValue</c>. That it compiles with warnings as errors is the main check; this shows the
/// binding it compiled to also works.
/// </summary>
public class NullableBindingTests : TestBase
{
    [Fact]
    public void A_nullable_string_field_binds_to_options_of_string()
    {
        var cut = Render<NullableBindingFixture>();
        cut.Instance.Framework.ShouldBeNull();

        cut.Find("#framework").Focus();
        cut.FindAll(".combobox-option").Single(o => o.TextContent.Trim() == "Blazor").Click();

        cut.Instance.Framework.ShouldBe("blazor");
    }
}
