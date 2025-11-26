namespace NoticeBoard.Services;

/// <summary>
/// JSON 檔案儲存服務介面
/// </summary>
/// <typeparam name="T">要儲存的資料型別</typeparam>
public interface IJsonStorageService<T> where T : class
{
    /// <summary>
    /// 從 JSON 檔案載入資料
    /// </summary>
    /// <param name="filePath">檔案路徑</param>
    /// <returns>載入的資料列表，若檔案不存在則返回空列表</returns>
    Task<List<T>> LoadAsync(string filePath);

    /// <summary>
    /// 將資料儲存至 JSON 檔案（原子寫入）
    /// </summary>
    /// <param name="filePath">檔案路徑</param>
    /// <param name="data">要儲存的資料列表</param>
    Task SaveAsync(string filePath, List<T> data);

    /// <summary>
    /// 新增單筆資料
    /// </summary>
    /// <param name="filePath">檔案路徑</param>
    /// <param name="item">要新增的項目</param>
    Task AddAsync(string filePath, T item);

    /// <summary>
    /// 更新資料（根據條件）
    /// </summary>
    /// <param name="filePath">檔案路徑</param>
    /// <param name="predicate">找到要更新項目的條件</param>
    /// <param name="updater">更新動作</param>
    /// <returns>是否有更新任何項目</returns>
    Task<bool> UpdateAsync(string filePath, Func<T, bool> predicate, Action<T> updater);

    /// <summary>
    /// 刪除資料（根據條件）
    /// </summary>
    /// <param name="filePath">檔案路徑</param>
    /// <param name="predicate">找到要刪除項目的條件</param>
    /// <returns>刪除的項目數量</returns>
    Task<int> RemoveAsync(string filePath, Func<T, bool> predicate);
}
