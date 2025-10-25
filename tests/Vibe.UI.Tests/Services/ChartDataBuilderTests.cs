namespace Vibe.UI.Tests.Services;

public class ChartDataBuilderTests
{
    [Fact]
    public void ChartDataBuilder_WithLabels_SetsLabels()
    {
        // Arrange & Act
        var chartData = new ChartDataBuilder()
            .WithLabels("Jan", "Feb", "Mar")
            .Build();

        // Assert
        chartData.Labels.Should().HaveCount(3);
        chartData.Labels.Should().Contain(new[] { "Jan", "Feb", "Mar" });
    }

    [Fact]
    public void ChartDataBuilder_AddDataset_AddsDataset()
    {
        // Arrange & Act
        var chartData = new ChartDataBuilder()
            .WithLabels("A", "B")
            .AddDataset("Sales", new[] { 10.0, 20.0 })
            .Build();

        // Assert
        chartData.Datasets.Should().HaveCount(1);
        chartData.Datasets[0].Label.Should().Be("Sales");
        chartData.Datasets[0].Data.Should().Equal(10.0, 20.0);
    }

    [Fact]
    public void ChartDataBuilder_AddDataset_WithCustomColor()
    {
        // Arrange & Act
        var chartData = new ChartDataBuilder()
            .WithLabels("A")
            .AddDataset("Sales", new[] { 10.0 }, "#FF0000")
            .Build();

        // Assert
        chartData.Datasets[0].Color.Should().Be("#FF0000");
    }

    [Fact]
    public void ChartDataBuilder_AddMultipleDatasets()
    {
        // Arrange & Act
        var chartData = new ChartDataBuilder()
            .WithLabels("A", "B", "C")
            .AddDataset("Dataset 1", new[] { 1.0, 2.0, 3.0 })
            .AddDataset("Dataset 2", new[] { 4.0, 5.0, 6.0 })
            .Build();

        // Assert
        chartData.Datasets.Should().HaveCount(2);
        chartData.Datasets[0].Label.Should().Be("Dataset 1");
        chartData.Datasets[1].Label.Should().Be("Dataset 2");
    }

    [Fact]
    public void ChartDataBuilder_AddDataset_WithConfiguration()
    {
        // Arrange & Act
        var chartData = new ChartDataBuilder()
            .WithLabels("A")
            .AddDataset(dataset =>
            {
                dataset.Label = "Custom";
                dataset.Data = new List<double> { 100 };
                dataset.BorderWidth = 5;
                dataset.Fill = true;
            })
            .Build();

        // Assert
        chartData.Datasets[0].Label.Should().Be("Custom");
        chartData.Datasets[0].BorderWidth.Should().Be(5);
        chartData.Datasets[0].Fill.Should().BeTrue();
    }

    [Fact]
    public void ChartDataBuilder_CreateLineChart_CreatesCorrectStructure()
    {
        // Arrange
        var labels = new[] { "Jan", "Feb", "Mar" };
        var datasets = new Dictionary<string, double[]>
        {
            ["Revenue"] = new[] { 100.0, 200.0, 300.0 },
            ["Costs"] = new[] { 50.0, 75.0, 100.0 }
        };

        // Act
        var chartData = ChartDataBuilder.CreateLineChart(labels, datasets);

        // Assert
        chartData.Labels.Should().Equal(labels);
        chartData.Datasets.Should().HaveCount(2);
        chartData.Datasets[0].Label.Should().Be("Revenue");
        chartData.Datasets[1].Label.Should().Be("Costs");
    }

    [Fact]
    public void ChartDataBuilder_CreateBarChart_CreatesCorrectStructure()
    {
        // Arrange
        var labels = new[] { "Q1", "Q2", "Q3" };
        var datasets = new Dictionary<string, double[]>
        {
            ["Sales"] = new[] { 1000.0, 1500.0, 2000.0 }
        };

        // Act
        var chartData = ChartDataBuilder.CreateBarChart(labels, datasets);

        // Assert
        chartData.Labels.Should().Equal(labels);
        chartData.Datasets.Should().HaveCount(1);
        chartData.Datasets[0].Data.Should().Equal(1000.0, 1500.0, 2000.0);
    }

    [Fact]
    public void ChartDataBuilder_CreatePieChart_CreatesCorrectStructure()
    {
        // Arrange
        var labels = new[] { "Chrome", "Firefox", "Safari" };
        var values = new[] { 60.0, 25.0, 15.0 };

        // Act
        var chartData = ChartDataBuilder.CreatePieChart(labels, values);

        // Assert
        chartData.Labels.Should().Equal(labels);
        chartData.Datasets.Should().HaveCount(1);
        chartData.Datasets[0].Data.Should().Equal(values);
    }

    [Fact]
    public void ChartDataBuilder_CreatePieChart_WithCustomColors()
    {
        // Arrange
        var labels = new[] { "A", "B" };
        var values = new[] { 50.0, 50.0 };
        var colors = new[] { "#FF0000", "#00FF00" };

        // Act
        var chartData = ChartDataBuilder.CreatePieChart(labels, values, colors);

        // Assert
        chartData.Datasets[0].BackgroundColor.Should().Contain("#FF0000");
        chartData.Datasets[0].BackgroundColor.Should().Contain("#00FF00");
    }

    [Fact]
    public void ChartDataBuilder_CreateAreaChart_CreatesCorrectStructure()
    {
        // Arrange
        var labels = new[] { "Mon", "Tue", "Wed" };
        var datasets = new Dictionary<string, double[]>
        {
            ["Desktop"] = new[] { 100.0, 150.0, 200.0 },
            ["Mobile"] = new[] { 80.0, 120.0, 160.0 }
        };

        // Act
        var chartData = ChartDataBuilder.CreateAreaChart(labels, datasets);

        // Assert
        chartData.Datasets.Should().HaveCount(2);
        chartData.Datasets[0].Fill.Should().BeTrue();
        chartData.Datasets[1].Fill.Should().BeTrue();
        chartData.Datasets[0].BackgroundColor.Should().NotBeNullOrEmpty();
    }

    [Theory]
    [InlineData(ChartType.Line)]
    [InlineData(ChartType.Bar)]
    [InlineData(ChartType.Pie)]
    [InlineData(ChartType.Doughnut)]
    [InlineData(ChartType.Area)]
    public void ChartDataBuilder_CreateSampleData_WorksForAllTypes(ChartType type)
    {
        // Act
        var chartData = ChartDataBuilder.CreateSampleData(type);

        // Assert
        chartData.Should().NotBeNull();
        chartData.Labels.Should().NotBeEmpty();
        chartData.Datasets.Should().NotBeEmpty();
    }

    [Fact]
    public void ChartDataBuilder_AssignsDefaultColors_Automatically()
    {
        // Arrange & Act
        var chartData = new ChartDataBuilder()
            .WithLabels("A", "B", "C")
            .AddDataset("Dataset 1", new[] { 1.0, 2.0, 3.0 })
            .AddDataset("Dataset 2", new[] { 4.0, 5.0, 6.0 })
            .AddDataset("Dataset 3", new[] { 7.0, 8.0, 9.0 })
            .Build();

        // Assert
        chartData.Datasets[0].Color.Should().NotBeNullOrEmpty();
        chartData.Datasets[1].Color.Should().NotBeNullOrEmpty();
        chartData.Datasets[2].Color.Should().NotBeNullOrEmpty();
        // Colors should be different
        chartData.Datasets[0].Color.Should().NotBe(chartData.Datasets[1].Color);
    }
}
