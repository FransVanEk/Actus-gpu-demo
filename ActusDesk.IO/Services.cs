using ActusDesk.Domain;

namespace ActusDesk.IO;

/// <summary>
/// Contract loader interface
/// </summary>
public interface IContractLoader
{
    Task<List<IContractTerms>> LoadAsync(string path, CancellationToken ct = default);
}

/// <summary>
/// Cache service interface
/// </summary>
public interface ICacheService
{
    Task<bool> ExistsAsync(string cacheKey);
    Task SaveAsync(string cacheKey, object data, CancellationToken ct = default);
    Task<T?> LoadAsync<T>(string cacheKey, CancellationToken ct = default);
}

/// <summary>
/// JSON contract loader
/// </summary>
public class JsonContractLoader : IContractLoader
{
    public Task<List<IContractTerms>> LoadAsync(string path, CancellationToken ct = default)
    {
        // TODO: Implement JSON loading with System.Text.Json
        return Task.FromResult(new List<IContractTerms>());
    }
}

/// <summary>
/// Binary cache service using SoA format
/// </summary>
public class BinaryCacheService : ICacheService
{
    private readonly string _cacheDirectory;

    public BinaryCacheService()
    {
        _cacheDirectory = Path.Combine(Directory.GetCurrentDirectory(), "cache");
        Directory.CreateDirectory(_cacheDirectory);
    }

    public Task<bool> ExistsAsync(string cacheKey)
    {
        string path = GetCachePath(cacheKey);
        return Task.FromResult(File.Exists(path));
    }

    public async Task SaveAsync(string cacheKey, object data, CancellationToken ct = default)
    {
        string path = GetCachePath(cacheKey);
        // TODO: Implement binary serialization
        await Task.CompletedTask;
    }

    public async Task<T?> LoadAsync<T>(string cacheKey, CancellationToken ct = default)
    {
        string path = GetCachePath(cacheKey);
        if (!File.Exists(path))
            return default;
        
        // TODO: Implement binary deserialization
        await Task.CompletedTask;
        return default;
    }

    private string GetCachePath(string cacheKey)
    {
        return Path.Combine(_cacheDirectory, $"{cacheKey}.bin");
    }
}
