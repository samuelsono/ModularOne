

namespace CarTrack.Modules.Leave;

public class LeaveDocumentStorage(IWebHostEnvironment environment) : ILeaveDocumentStorage
{
    private const long MaxFileSizeBytes = 5 * 1024 * 1024;

    private static readonly Dictionary<string, string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        [".pdf"] = "application/pdf",
        [".jpg"] = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".png"] = "image/png",
        [".heic"] = "image/heic",
        [".doc"] = "application/msword",
        [".docx"] = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
    };

    public async Task<LeaveDocumentMetadata> SaveAsync(
        Guid requestId,
        IFormFile file,
        CancellationToken cancellationToken = default)
    {
        if (file.Length <= 0)
        {
            throw new InvalidOperationException("The uploaded document is empty.");
        }

        if (file.Length > MaxFileSizeBytes)
        {
            throw new InvalidOperationException("Documents must be 5 MB or smaller.");
        }

        var extension = Path.GetExtension(file.FileName);
        if (string.IsNullOrWhiteSpace(extension) || !AllowedExtensions.ContainsKey(extension))
        {
            throw new InvalidOperationException(
                "Unsupported document type. Allowed formats: PDF, JPG, PNG, HEIC, DOC, DOCX.");
        }

        var contentType = AllowedExtensions[extension];
        var storedFileName = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
        var directory = Path.Combine(
            environment.ContentRootPath,
            "App_Data",
            "leave-documents",
            requestId.ToString("N"));

        Directory.CreateDirectory(directory);

        var fullPath = Path.Combine(directory, storedFileName);
        await using (var stream = new FileStream(fullPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
        {
            await file.CopyToAsync(stream, cancellationToken);
        }

        var storedPath = Path.Combine("leave-documents", requestId.ToString("N"), storedFileName)
            .Replace('\\', '/');

        return new LeaveDocumentMetadata(
            storedPath,
            Path.GetFileName(file.FileName),
            contentType);
    }

    public Task<(Stream Stream, string ContentType, string FileName)?> OpenReadAsync(
        string storedPath,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(storedPath))
        {
            return Task.FromResult<(Stream, string, string)?>(null);
        }

        var normalizedPath = storedPath.Replace('/', Path.DirectorySeparatorChar);
        var fullPath = Path.GetFullPath(Path.Combine(environment.ContentRootPath, "App_Data", normalizedPath));
        var rootPath = Path.GetFullPath(Path.Combine(environment.ContentRootPath, "App_Data", "leave-documents"));

        if (!fullPath.StartsWith(rootPath, StringComparison.OrdinalIgnoreCase) || !File.Exists(fullPath))
        {
            return Task.FromResult<(Stream, string, string)?>(null);
        }

        var extension = Path.GetExtension(fullPath);
        var contentType = AllowedExtensions.TryGetValue(extension, out var mapped)
            ? mapped
            : "application/octet-stream";

        Stream stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        return Task.FromResult<(Stream, string, string)?>((stream, contentType, Path.GetFileName(fullPath)));
    }

    public Task DeleteIfExistsAsync(string? storedPath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(storedPath))
        {
            return Task.CompletedTask;
        }

        var normalizedPath = storedPath.Replace('/', Path.DirectorySeparatorChar);
        var fullPath = Path.GetFullPath(Path.Combine(environment.ContentRootPath, "App_Data", normalizedPath));
        var rootPath = Path.GetFullPath(Path.Combine(environment.ContentRootPath, "App_Data", "leave-documents"));

        if (fullPath.StartsWith(rootPath, StringComparison.OrdinalIgnoreCase) && File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }

        var directory = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrWhiteSpace(directory)
            && Directory.Exists(directory)
            && !Directory.EnumerateFileSystemEntries(directory).Any())
        {
            Directory.Delete(directory);
        }

        return Task.CompletedTask;
    }
}
