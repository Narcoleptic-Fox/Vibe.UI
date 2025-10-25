namespace Vibe.UI.Tests.Components.DataDisplay;

public class ChartTests : TestContext
{
    public ChartTests()
    {
        this.AddVibeUIServices();
    }

    [Fact]
    public void Chart_RendersWithBasicData()
    {
        // Arrange
        var chartData = new ChartData
        {
            Labels = new List<string> { "Jan", "Feb", "Mar" },
            Datasets = new List<ChartDataset>
            {
                new() { Label = "Sales", Data = new List<double> { 10, 20, 30 } }
            }
        };

        // Act
        var cut = RenderComponent<Chart>(parameters => parameters
            .Add(p => p.ChartData, chartData));

        // Assert
        cut.Find("canvas").Should().NotBeNull();
        cut.Find("canvas").ClassList.Should().Contain("vibe-chart-canvas");
    }

    [Fact]
    public void Chart_DisplaysTitle()
    {
        // Arrange
        var chartData = new ChartData
        {
            Labels = new List<string> { "A", "B" },
            Datasets = new List<ChartDataset>
            {
                new() { Data = new List<double> { 1, 2 } }
            }
        };

        // Act
        var cut = RenderComponent<Chart>(parameters => parameters
            .Add(p => p.ChartData, chartData)
            .Add(p => p.Title, "Sales Chart"));

        // Assert
        var title = cut.Find(".vibe-chart-title");
        title.TextContent.Should().Be("Sales Chart");
    }

    [Fact]
    public void Chart_DisplaysDescription()
    {
        // Arrange
        var chartData = new ChartData
        {
            Labels = new List<string> { "A" },
            Datasets = new List<ChartDataset>
            {
                new() { Data = new List<double> { 1 } }
            }
        };

        // Act
        var cut = RenderComponent<Chart>(parameters => parameters
            .Add(p => p.ChartData, chartData)
            .Add(p => p.Title, "Chart")
            .Add(p => p.Description, "Monthly data"));

        // Assert
        var description = cut.Find(".vibe-chart-description");
        description.TextContent.Should().Be("Monthly data");
    }

    [Theory]
    [InlineData(ChartType.Line, "vibe-chart-line")]
    [InlineData(ChartType.Bar, "vibe-chart-bar")]
    [InlineData(ChartType.Pie, "vibe-chart-pie")]
    [InlineData(ChartType.Doughnut, "vibe-chart-doughnut")]
    public void Chart_AppliesCorrectTypeClass(ChartType type, string expectedClass)
    {
        // Arrange
        var chartData = new ChartData
        {
            Labels = new List<string> { "A" },
            Datasets = new List<ChartDataset>
            {
                new() { Data = new List<double> { 1 } }
            }
        };

        // Act
        var cut = RenderComponent<Chart>(parameters => parameters
            .Add(p => p.ChartData, chartData)
            .Add(p => p.Type, type));

        // Assert
        cut.Find(".vibe-chart").ClassList.Should().Contain(expectedClass);
    }

    [Fact]
    public void Chart_AppliesCustomHeight()
    {
        // Arrange
        var chartData = new ChartData
        {
            Labels = new List<string> { "A" },
            Datasets = new List<ChartDataset>
            {
                new() { Data = new List<double> { 1 } }
            }
        };

        // Act
        var cut = RenderComponent<Chart>(parameters => parameters
            .Add(p => p.ChartData, chartData)
            .Add(p => p.Height, 500));

        // Assert
        var container = cut.Find(".vibe-chart-container");
        container.GetAttribute("style").Should().Contain("height: 500px");
    }

    [Fact]
    public void Chart_ShowsLegend_ByDefault()
    {
        // Arrange
        var chartData = new ChartData
        {
            Labels = new List<string> { "A" },
            Datasets = new List<ChartDataset>
            {
                new() { Label = "Dataset 1", Data = new List<double> { 1 } }
            }
        };

        // Act
        var cut = RenderComponent<Chart>(parameters => parameters
            .Add(p => p.ChartData, chartData));

        // Assert
        cut.FindAll(".vibe-chart-legend").Should().NotBeEmpty();
    }

    [Fact]
    public void Chart_HidesLegend_WhenDisabled()
    {
        // Arrange
        var chartData = new ChartData
        {
            Labels = new List<string> { "A" },
            Datasets = new List<ChartDataset>
            {
                new() { Label = "Dataset 1", Data = new List<double> { 1 } }
            }
        };

        // Act
        var cut = RenderComponent<Chart>(parameters => parameters
            .Add(p => p.ChartData, chartData)
            .Add(p => p.ShowLegend, false));

        // Assert
        cut.FindAll(".vibe-chart-legend").Should().BeEmpty();
    }

    [Fact]
    public void Chart_DisplaysFooterContent()
    {
        // Arrange
        var chartData = new ChartData
        {
            Labels = new List<string> { "A" },
            Datasets = new List<ChartDataset>
            {
                new() { Data = new List<double> { 1 } }
            }
        };

        RenderFragment footer = builder => builder.AddContent(0, "Footer text");

        // Act
        var cut = RenderComponent<Chart>(parameters => parameters
            .Add(p => p.ChartData, chartData)
            .Add(p => p.FooterContent, footer));

        // Assert
        var footerElement = cut.Find(".vibe-chart-footer");
        footerElement.TextContent.Should().Contain("Footer text");
    }

    [Fact]
    public void Chart_SupportsMultipleDatasets()
    {
        // Arrange
        var chartData = new ChartData
        {
            Labels = new List<string> { "A", "B", "C" },
            Datasets = new List<ChartDataset>
            {
                new() { Label = "Dataset 1", Data = new List<double> { 1, 2, 3 } },
                new() { Label = "Dataset 2", Data = new List<double> { 4, 5, 6 } }
            }
        };

        // Act
        var cut = RenderComponent<Chart>(parameters => parameters
            .Add(p => p.ChartData, chartData));

        // Assert
        var legendItems = cut.FindAll(".vibe-chart-legend-item");
        legendItems.Should().HaveCount(2);
    }

    [Fact]
    public void Chart_UsesCustomOptions()
    {
        // Arrange
        var chartData = new ChartData
        {
            Labels = new List<string> { "A" },
            Datasets = new List<ChartDataset>
            {
                new() { Data = new List<double> { 1 } }
            }
        };

        var options = new ChartOptions
        {
            Responsive = false,
            MaintainAspectRatio = true
        };

        // Act
        var cut = RenderComponent<Chart>(parameters => parameters
            .Add(p => p.ChartData, chartData)
            .Add(p => p.Options, options));

        // Assert
        cut.Find("canvas").Should().NotBeNull();
    }
}
