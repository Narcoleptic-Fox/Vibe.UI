namespace Vibe.UI.Tests.Components.Input;

public class FileUploadTests : TestContext
{
    public FileUploadTests()
    {
        this.AddVibeUIServices();
    }

    [Fact]
    public void FileUpload_RendersWithBasicStructure()
    {
        // Arrange & Act
        var cut = RenderComponent<FileUpload>();

        // Assert
        cut.Find(".vibe-file-upload").Should().NotBeNull();
        cut.Find("input[type='file']").Should().NotBeNull();
    }

    [Fact]
    public void FileUpload_ShowsEmptyState_WhenNoFiles()
    {
        // Arrange & Act
        var cut = RenderComponent<FileUpload>();

        // Assert
        cut.Find(".file-upload-empty").Should().NotBeNull();
        cut.FindAll(".file-upload-list").Should().BeEmpty();
    }

    [Fact]
    public void FileUpload_DisplaysDropText()
    {
        // Arrange & Act
        var cut = RenderComponent<FileUpload>(parameters => parameters
            .Add(p => p.DropText, "Drop your files here"));

        // Assert
        cut.Find(".file-upload-title").TextContent.Should().Be("Drop your files here");
    }

    [Fact]
    public void FileUpload_DisplaysHintText()
    {
        // Arrange & Act
        var cut = RenderComponent<FileUpload>(parameters => parameters
            .Add(p => p.HintText, "PDF and Images only"));

        // Assert
        cut.Find(".file-upload-hint").TextContent.Should().Be("PDF and Images only");
    }

    [Fact]
    public void FileUpload_ShowsFileList_WhenFilesExist()
    {
        // Arrange
        var files = new List<UploadedFile>
        {
            new() { Name = "document.pdf", Size = 1024000 },
            new() { Name = "image.jpg", Size = 2048000 }
        };

        // Act
        var cut = RenderComponent<FileUpload>(parameters => parameters
            .Add(p => p.Files, files));

        // Assert
        cut.Find(".file-upload-list").Should().NotBeNull();
        cut.FindAll(".file-upload-item").Should().HaveCount(2);
        cut.FindAll(".file-upload-empty").Should().BeEmpty();
    }

    [Fact]
    public void FileUpload_DisplaysFileName()
    {
        // Arrange
        var files = new List<UploadedFile>
        {
            new() { Name = "document.pdf", Size = 1024 }
        };

        // Act
        var cut = RenderComponent<FileUpload>(parameters => parameters
            .Add(p => p.Files, files));

        // Assert
        cut.Find(".file-item-name").TextContent.Should().Be("document.pdf");
    }

    [Fact]
    public void FileUpload_DisplaysFormattedFileSize()
    {
        // Arrange
        var files = new List<UploadedFile>
        {
            new() { Name = "file.txt", Size = 1024 } // 1 KB
        };

        // Act
        var cut = RenderComponent<FileUpload>(parameters => parameters
            .Add(p => p.Files, files));

        // Assert
        cut.Find(".file-item-size").TextContent.Should().Contain("KB");
    }

    [Fact]
    public void FileUpload_RemovesFile_WhenRemoveButtonClicked()
    {
        // Arrange
        var files = new List<UploadedFile>
        {
            new() { Name = "document.pdf", Size = 1024 }
        };
        var cut = RenderComponent<FileUpload>(parameters => parameters
            .Add(p => p.Files, files)
            .Add(p => p.FilesChanged, EventCallback.Factory.Create<List<UploadedFile>>(this, updatedFiles => files = updatedFiles)));

        // Act
        cut.Find(".file-item-remove").Click();

        // Assert
        files.Should().BeEmpty();
    }

    [Fact]
    public void FileUpload_ShowsAddMoreButton_WhenFilesExist()
    {
        // Arrange
        var files = new List<UploadedFile>
        {
            new() { Name = "file1.txt", Size = 1024 }
        };

        // Act
        var cut = RenderComponent<FileUpload>(parameters => parameters
            .Add(p => p.Files, files));

        // Assert
        cut.Find(".file-upload-add").Should().NotBeNull();
        cut.Find(".file-upload-add").TextContent.Should().Contain("Add More Files");
    }

    [Fact]
    public void FileUpload_SupportsMultipleFiles_ByDefault()
    {
        // Arrange & Act
        var cut = RenderComponent<FileUpload>();

        // Assert
        cut.Find("input[type='file']").HasAttribute("multiple").Should().BeTrue();
    }

    [Fact]
    public void FileUpload_DisablesMultiple_WhenSet()
    {
        // Arrange & Act
        var cut = RenderComponent<FileUpload>(parameters => parameters
            .Add(p => p.Multiple, false));

        // Assert
        cut.Find("input[type='file']").HasAttribute("multiple").Should().BeFalse();
    }

    [Fact]
    public void FileUpload_AppliesAcceptAttribute()
    {
        // Arrange & Act
        var cut = RenderComponent<FileUpload>(parameters => parameters
            .Add(p => p.Accept, ".pdf,.jpg,.png"));

        // Assert
        cut.Find("input[type='file']").GetAttribute("accept").Should().Be(".pdf,.jpg,.png");
    }

    [Fact]
    public void FileUpload_AppliesCustomCssClass()
    {
        // Arrange & Act
        var cut = RenderComponent<FileUpload>(parameters => parameters
            .Add(p => p.CssClass, "custom-file-upload"));

        // Assert
        cut.Find(".vibe-file-upload").ClassList.Should().Contain("custom-file-upload");
    }

    [Fact]
    public void FileUpload_FormatsBytes_Correctly()
    {
        // Arrange
        var files = new List<UploadedFile>
        {
            new() { Name = "small.txt", Size = 500 }, // 500 B
            new() { Name = "medium.txt", Size = 500000 }, // ~488 KB
            new() { Name = "large.txt", Size = 5000000 } // ~4.77 MB
        };

        // Act
        var cut = RenderComponent<FileUpload>(parameters => parameters
            .Add(p => p.Files, files));

        // Assert
        var sizes = cut.FindAll(".file-item-size");
        sizes[0].TextContent.Should().Contain("B");
        sizes[1].TextContent.Should().Contain("KB");
        sizes[2].TextContent.Should().Contain("MB");
    }

    [Fact]
    public void FileUpload_HandlesEmptyFileList()
    {
        // Arrange
        var emptyFiles = new List<UploadedFile>();

        // Act
        var cut = RenderComponent<FileUpload>(parameters => parameters
            .Add(p => p.Files, emptyFiles));

        // Assert
        cut.Find(".file-upload-empty").Should().NotBeNull();
    }
}
