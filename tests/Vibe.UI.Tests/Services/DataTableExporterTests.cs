namespace Vibe.UI.Tests.Services;

public class DataTableExporterTests
{
    private class TestData
    {
        public string Name { get; set; } = "";
        public int Age { get; set; }
        public string Email { get; set; } = "";
    }

    [Fact]
    public void ToCsv_ExportsDataCorrectly()
    {
        // Arrange
        var data = new List<TestData>
        {
            new() { Name = "John", Age = 30, Email = "john@example.com" },
            new() { Name = "Jane", Age = 25, Email = "jane@example.com" }
        };

        // Act
        var csv = DataTableExporter.ToCsv(data);

        // Assert
        csv.Should().Contain("Name,Age,Email");
        csv.Should().Contain("John,30,john@example.com");
        csv.Should().Contain("Jane,25,jane@example.com");
    }

    [Fact]
    public void ToCsv_EscapesCommas()
    {
        // Arrange
        var data = new List<TestData>
        {
            new() { Name = "Doe, John", Age = 30, Email = "john@example.com" }
        };

        // Act
        var csv = DataTableExporter.ToCsv(data);

        // Assert
        csv.Should().Contain("\"Doe, John\"");
    }

    [Fact]
    public void ToCsv_EscapesQuotes()
    {
        // Arrange
        var data = new List<TestData>
        {
            new() { Name = "John \"Johnny\" Doe", Age = 30, Email = "john@example.com" }
        };

        // Act
        var csv = DataTableExporter.ToCsv(data);

        // Assert
        csv.Should().Contain("\"John \"\"Johnny\"\" Doe\"");
    }

    [Fact]
    public void ToCsv_HandlesEmptyList()
    {
        // Arrange
        var data = new List<TestData>();

        // Act
        var csv = DataTableExporter.ToCsv(data);

        // Assert
        csv.Should().BeEmpty();
    }

    [Fact]
    public void ToCsv_WithCustomColumns()
    {
        // Arrange
        var data = new List<TestData>
        {
            new() { Name = "John", Age = 30, Email = "john@example.com" }
        };

        var columns = new Dictionary<string, Func<TestData, object?>>
        {
            ["Full Name"] = d => d.Name,
            ["Years"] = d => d.Age
        };

        // Act
        var csv = DataTableExporter.ToCsv(data, columns);

        // Assert
        csv.Should().Contain("Full Name,Years");
        csv.Should().NotContain("Email");
    }

    [Fact]
    public void ToTsv_ExportsWithTabs()
    {
        // Arrange
        var data = new List<TestData>
        {
            new() { Name = "John", Age = 30, Email = "john@example.com" }
        };

        // Act
        var tsv = DataTableExporter.ToTsv(data);

        // Assert
        tsv.Should().Contain("Name\tAge\tEmail");
        tsv.Should().Contain("John\t30\tjohn@example.com");
    }

    [Fact]
    public void ToJson_ExportsAsJson()
    {
        // Arrange
        var data = new List<TestData>
        {
            new() { Name = "John", Age = 30, Email = "john@example.com" }
        };

        // Act
        var json = DataTableExporter.ToJson(data);

        // Assert
        json.Should().Contain("\"name\": \"John\"");
        json.Should().Contain("\"age\": 30");
        json.Should().Contain("\"email\": \"john@example.com\"");
    }

    [Fact]
    public void ToHtml_ExportsAsHtmlTable()
    {
        // Arrange
        var data = new List<TestData>
        {
            new() { Name = "John", Age = 30, Email = "john@example.com" }
        };

        // Act
        var html = DataTableExporter.ToHtml(data);

        // Assert
        html.Should().Contain("<table>");
        html.Should().Contain("<thead>");
        html.Should().Contain("<tbody>");
        html.Should().Contain("<th>Name</th>");
        html.Should().Contain("<td>John</td>");
    }

    [Fact]
    public void ToHtml_WithCustomTableClass()
    {
        // Arrange
        var data = new List<TestData>
        {
            new() { Name = "John", Age = 30, Email = "john@example.com" }
        };

        // Act
        var html = DataTableExporter.ToHtml(data, tableClass: "custom-table");

        // Assert
        html.Should().Contain("<table class=\"custom-table\">");
    }

    [Fact]
    public void ToHtml_EscapesHtmlCharacters()
    {
        // Arrange
        var data = new List<TestData>
        {
            new() { Name = "<script>alert('XSS')</script>", Age = 30, Email = "test@example.com" }
        };

        // Act
        var html = DataTableExporter.ToHtml(data);

        // Assert
        html.Should().Contain("&lt;script&gt;");
        html.Should().NotContain("<script>");
    }

    [Fact]
    public void ToDataUri_CreatesValidDataUri()
    {
        // Arrange
        var content = "test data";

        // Act
        var dataUri = DataTableExporter.ToDataUri(content, "text/plain");

        // Assert
        dataUri.Should().StartWith("data:text/plain;base64,");
    }

    [Fact]
    public void ToCsvDataUri_CreatesValidCsvDataUri()
    {
        // Arrange
        var csvContent = "Name,Age\nJohn,30";

        // Act
        var dataUri = DataTableExporter.ToCsvDataUri(csvContent);

        // Assert
        dataUri.Should().StartWith("data:text/csv;charset=utf-8;base64,");
    }

    [Fact]
    public void ToJsonDataUri_CreatesValidJsonDataUri()
    {
        // Arrange
        var jsonContent = "{\"name\":\"John\"}";

        // Act
        var dataUri = DataTableExporter.ToJsonDataUri(jsonContent);

        // Assert
        dataUri.Should().StartWith("data:application/json;charset=utf-8;base64,");
    }
}
