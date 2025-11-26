using System.Text;
using System.Text.Json;

namespace NoticeBoard.Services;

/// <summary>
/// JSON 檔案儲存服務實作（原子寫入 + mutex）
/// </summary>
/// <typeparam name="T">要儲存的資料型別</typeparam>
public class JsonStorageService<T> : IJsonStorageService<T> where T : class
{
    private static readonly SemaphoreSlim WriteLock = new(1, 1);
    private readonly ILogger<JsonStorageService<T>> _logger;
    
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    /// <summary>
    /// 建立 JSON 儲存服務實例
    /// </summary>
    /// <param name="logger">日誌記錄器</param>
    public JsonStorageService(ILogger<JsonStorageService<T>> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<List<T>> LoadAsync(string filePath)
    {
        if (!File.Exists(filePath))
        {
            _logger.LogDebug("檔案不存在，返回空列表: {FilePath}", filePath);
            return [];
        }

        try
        {
            var json = await File.ReadAllTextAsync(filePath, Encoding.UTF8);
            if (string.IsNullOrWhiteSpace(json))
            {
                return [];
            }

            var data = JsonSerializer.Deserialize<List<T>>(json, JsonOptions);
            return data ?? [];
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON 反序列化失敗: {FilePath}", filePath);
            return [];
        }
    }

    /// <inheritdoc/>
    public async Task SaveAsync(string filePath, List<T> data)
    {
        await WriteLock.WaitAsync();
        try
        {
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var tempPath = filePath + ".tmp";
            var json = JsonSerializer.Serialize(data, JsonOptions);

            // 寫入暫存檔
            await File.WriteAllTextAsync(tempPath, json, Encoding.UTF8);

            // 原子重新命名
            File.Move(tempPath, filePath, overwrite: true);

            _logger.LogDebug("資料已儲存: {FilePath}, 項目數: {Count}", filePath, data.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "儲存資料失敗: {FilePath}", filePath);
            throw;
        }
        finally
        {
            WriteLock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task AddAsync(string filePath, T item)
    {
        await WriteLock.WaitAsync();
        try
        {
            var data = await LoadInternalAsync(filePath);
            data.Add(item);
            await SaveInternalAsync(filePath, data);
            _logger.LogDebug("項目已新增: {FilePath}", filePath);
        }
        finally
        {
            WriteLock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<bool> UpdateAsync(string filePath, Func<T, bool> predicate, Action<T> updater)
    {
        await WriteLock.WaitAsync();
        try
        {
            var data = await LoadInternalAsync(filePath);
            var itemsToUpdate = data.Where(predicate).ToList();

            if (itemsToUpdate.Count == 0)
            {
                return false;
            }

            foreach (var item in itemsToUpdate)
            {
                updater(item);
            }

            await SaveInternalAsync(filePath, data);
            _logger.LogDebug("項目已更新: {FilePath}, 更新數: {Count}", filePath, itemsToUpdate.Count);
            return true;
        }
        finally
        {
            WriteLock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<int> RemoveAsync(string filePath, Func<T, bool> predicate)
    {
        await WriteLock.WaitAsync();
        try
        {
            var data = await LoadInternalAsync(filePath);
            var originalCount = data.Count;
            data.RemoveAll(new Predicate<T>(predicate));
            var removedCount = originalCount - data.Count;

            if (removedCount > 0)
            {
                await SaveInternalAsync(filePath, data);
                _logger.LogDebug("項目已刪除: {FilePath}, 刪除數: {Count}", filePath, removedCount);
            }

            return removedCount;
        }
        finally
        {
            WriteLock.Release();
        }
    }

    /// <summary>
    /// 內部載入方法（不使用鎖，供已持有鎖的方法使用）
    /// </summary>
    private async Task<List<T>> LoadInternalAsync(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return [];
        }

        try
        {
            var json = await File.ReadAllTextAsync(filePath, Encoding.UTF8);
            if (string.IsNullOrWhiteSpace(json))
            {
                return [];
            }

            var data = JsonSerializer.Deserialize<List<T>>(json, JsonOptions);
            return data ?? [];
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON 反序列化失敗: {FilePath}", filePath);
            return [];
        }
    }

    /// <summary>
    /// 內部儲存方法（不使用鎖，供已持有鎖的方法使用）
    /// </summary>
    private async Task SaveInternalAsync(string filePath, List<T> data)
    {
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var tempPath = filePath + ".tmp";
        var json = JsonSerializer.Serialize(data, JsonOptions);

        await File.WriteAllTextAsync(tempPath, json, Encoding.UTF8);
        File.Move(tempPath, filePath, overwrite: true);
    }
}
