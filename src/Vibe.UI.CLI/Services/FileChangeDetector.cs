using System.Security.Cryptography;
using System.Text;

namespace Vibe.UI.CLI.Services;

/// <summary>
/// Detects if files have been modified from their original installed state.
/// Uses SHA256 checksums to track file changes.
/// </summary>
public class FileChangeDetector
{
    private readonly string _projectPath;
    private readonly string _checksumFile;

    public FileChangeDetector(string projectPath)
    {
        _projectPath = projectPath;
        _checksumFile = Path.Combine(projectPath, ".vibe", "checksums.json");
    }

    /// <summary>
    /// Computes SHA256 hash of file contents.
    /// </summary>
    public static string ComputeFileHash(string filePath)
    {
        if (!File.Exists(filePath))
            return string.Empty;

        using var stream = File.OpenRead(filePath);
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(stream);
        return Convert.ToBase64String(hash);
    }

    /// <summary>
    /// Checks if a file has been modified since installation.
    /// </summary>
    public async Task<bool> HasFileBeenModifiedAsync(string filePath, string originalHash)
    {
        if (!File.Exists(filePath))
            return false;

        var currentHash = ComputeFileHash(filePath);
        return currentHash != originalHash;
    }

    /// <summary>
    /// Saves the original hash of a file for future comparison.
    /// </summary>
    public async Task SaveFileHashAsync(string relativePath, string hash)
    {
        var checksumDir = Path.GetDirectoryName(_checksumFile);
        if (!string.IsNullOrEmpty(checksumDir))
        {
            Directory.CreateDirectory(checksumDir);
        }

        // Load existing checksums
        var checksums = await LoadChecksumsAsync();

        // Update or add the hash
        checksums[relativePath] = hash;

        // Save back to file
        var json = System.Text.Json.JsonSerializer.Serialize(checksums, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true
        });
        await File.WriteAllTextAsync(_checksumFile, json);
    }

    /// <summary>
    /// Gets the stored hash for a file.
    /// </summary>
    public async Task<string?> GetStoredHashAsync(string relativePath)
    {
        var checksums = await LoadChecksumsAsync();
        return checksums.TryGetValue(relativePath, out var hash) ? hash : null;
    }

    /// <summary>
    /// Creates a backup of a file before overwriting.
    /// </summary>
    public async Task<string> CreateBackupAsync(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"File not found: {filePath}");

        var backupDir = Path.Combine(_projectPath, ".vibe", "backups");
        Directory.CreateDirectory(backupDir);

        var fileName = Path.GetFileName(filePath);
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var backupFileName = $"{Path.GetFileNameWithoutExtension(fileName)}_{timestamp}{Path.GetExtension(fileName)}";
        var backupPath = Path.Combine(backupDir, backupFileName);

        File.Copy(filePath, backupPath, true);
        return backupPath;
    }

    private async Task<Dictionary<string, string>> LoadChecksumsAsync()
    {
        if (!File.Exists(_checksumFile))
            return new Dictionary<string, string>();

        try
        {
            var json = await File.ReadAllTextAsync(_checksumFile);
            var checksums = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(json);
            return checksums ?? new Dictionary<string, string>();
        }
        catch
        {
            return new Dictionary<string, string>();
        }
    }
}

/// <summary>
/// Result of checking if a file can be safely overwritten.
/// </summary>
public class FileOverwriteResult
{
    public bool IsModified { get; set; }
    public bool Exists { get; set; }
    public string? CurrentHash { get; set; }
    public string? StoredHash { get; set; }
    public string? BackupPath { get; set; }
}
