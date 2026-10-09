namespace TaxRefund.Api.Shared.Common;

public interface IFileStorageService
{
    Task<(string FilePath, string FileName)> SaveAsync(IFormFile file, string desiredFileName);
    string GetAbsolutePath(string relativeFilePath);
}