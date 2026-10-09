namespace TaxRefund.Api.Shared.Common;

public class LocalFileStorageService(IConfiguration configuration, IWebHostEnvironment env) : IFileStorageService
{
    private readonly string _basePath = configuration["FileStorage:BasePath"] ?? "Uploads/FakturPajak";
    private readonly IWebHostEnvironment _env = env;

    public async Task<(string FilePath, string FileName)> SaveAsync(IFormFile file, string desiredFileName)
    {
        var now = DateTime.UtcNow;
        var relativeFolder = Path.Combine(_basePath, now.Year.ToString(), now.Month.ToString("00"));
        var absoluteFolder = Path.Combine(_env.ContentRootPath, relativeFolder);

        Directory.CreateDirectory(absoluteFolder);

        var extension = Path.GetExtension(file.FileName);
        var fileName = $"{desiredFileName}{extension}";
        var absoluteFilePath = Path.Combine(absoluteFolder, fileName);

        await using (var stream = new FileStream(absoluteFilePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        var relativeFilePath = Path.Combine(relativeFolder, fileName).Replace('\\', '/');
        return (relativeFilePath, fileName);
    }

    public string GetAbsolutePath(string relativeFilePath)
    {
        return Path.Combine(_env.ContentRootPath, relativeFilePath);
    }
}