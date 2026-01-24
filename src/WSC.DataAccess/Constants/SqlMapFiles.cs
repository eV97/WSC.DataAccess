namespace WSC.DataAccess.Constants;

/// <summary>
/// Constants cho SQL Map file paths
/// Sử dụng: SqlMapFiles.DAO002 thay vì "SqlMaps/DAO002.xml"
/// </summary>
public static class SqlMapFiles
{
    /// <summary>
    /// Base directory cho SQL map files
    /// </summary>
    public const string BASE_DIR = "SqlMaps";

    // ============================================================================
    // DAO Files - Thêm constants cho từng file DAO ở đây
    // ============================================================================

    /// <summary>DAO001.xml - [Mô tả chức năng]</summary>
    public const string DAO001 = BASE_DIR + "/DAO001.xml";

    /// <summary>DAO002.xml - [Mô tả chức năng]</summary>
    public const string DAO002 = BASE_DIR + "/DAO002.xml";

    /// <summary>DAO003.xml - [Mô tả chức năng]</summary>
    public const string DAO003 = BASE_DIR + "/DAO003.xml";

    /// <summary>DAO004.xml - [Mô tả chức năng]</summary>
    public const string DAO004 = BASE_DIR + "/DAO004.xml";

    /// <summary>DAO005.xml - Order management</summary>
    public const string DAO005 = BASE_DIR + "/DAO005.xml";

    /// <summary>DAO006.xml - [Mô tả chức năng]</summary>
    public const string DAO006 = BASE_DIR + "/DAO006.xml";

    /// <summary>DAO007.xml - [Mô tả chức năng]</summary>
    public const string DAO007 = BASE_DIR + "/DAO007.xml";

    /// <summary>DAO008.xml - [Mô tả chức năng]</summary>
    public const string DAO008 = BASE_DIR + "/DAO008.xml";

    /// <summary>DAO009.xml - [Mô tả chức năng]</summary>
    public const string DAO009 = BASE_DIR + "/DAO009.xml";

    /// <summary>DAO010.xml - Customer management</summary>
    public const string DAO010 = BASE_DIR + "/DAO010.xml";

    /// <summary>DAO011.xml - [Mô tả chức năng]</summary>
    public const string DAO011 = BASE_DIR + "/DAO011.xml";

    /// <summary>DAO012.xml - [Mô tả chức năng]</summary>
    public const string DAO012 = BASE_DIR + "/DAO012.xml";

    /// <summary>DAO013.xml - [Mô tả chức năng]</summary>
    public const string DAO013 = BASE_DIR + "/DAO013.xml";

    /// <summary>DAO014.xml - [Mô tả chức năng]</summary>
    public const string DAO014 = BASE_DIR + "/DAO014.xml";

    /// <summary>DAO015.xml - Product management</summary>
    public const string DAO015 = BASE_DIR + "/DAO015.xml";

    /// <summary>DAO016.xml - [Mô tả chức năng]</summary>
    public const string DAO016 = BASE_DIR + "/DAO016.xml";

    /// <summary>DAO017.xml - [Mô tả chức năng]</summary>
    public const string DAO017 = BASE_DIR + "/DAO017.xml";

    /// <summary>DAO018.xml - [Mô tả chức năng]</summary>
    public const string DAO018 = BASE_DIR + "/DAO018.xml";

    /// <summary>DAO019.xml - [Mô tả chức năng]</summary>
    public const string DAO019 = BASE_DIR + "/DAO019.xml";

    /// <summary>DAO020.xml - Reporting</summary>
    public const string DAO020 = BASE_DIR + "/DAO020.xml";

    // Thêm các DAO files khác theo nhu cầu...
    // public const string DAO021 = BASE_DIR + "/DAO021.xml";
    // public const string DAO022 = BASE_DIR + "/DAO022.xml";
    // ...

    // ============================================================================
    // Named Files - Các file có tên cụ thể
    // ============================================================================

    /// <summary>ApplicationMap.xml - Application entity mapping</summary>
    public const string APPLICATION_MAP = BASE_DIR + "/ApplicationMap.xml";

    /// <summary>GenericMap.xml - Generic database queries</summary>
    public const string GENERIC_MAP = BASE_DIR + "/GenericMap.xml";

    /// <summary>ProductMap.xml - Product entity mapping</summary>
    public const string PRODUCT_MAP = BASE_DIR + "/ProductMap.xml";

    /// <summary>UserMap.xml - User entity mapping</summary>
    public const string USER_MAP = BASE_DIR + "/UserMap.xml";

    /// <summary>CustomerMap.xml - Customer entity mapping</summary>
    public const string CUSTOMER_MAP = BASE_DIR + "/CustomerMap.xml";

    /// <summary>OrderMap.xml - Order entity mapping</summary>
    public const string ORDER_MAP = BASE_DIR + "/OrderMap.xml";

    // ============================================================================
    // Helper Methods
    // ============================================================================

    /// <summary>
    /// Lấy full path từ base path
    /// </summary>
    /// <param name="basePath">Base path của application</param>
    /// <param name="sqlMapFile">SQL map file constant (ví dụ: SqlMapFiles.DAO002)</param>
    /// <returns>Full absolute path</returns>
    public static string GetFullPath(string basePath, string sqlMapFile)
    {
        return Path.Combine(basePath, sqlMapFile);
    }

    /// <summary>
    /// Kiểm tra file có tồn tại không
    /// </summary>
    public static bool Exists(string basePath, string sqlMapFile)
    {
        var fullPath = GetFullPath(basePath, sqlMapFile);
        return File.Exists(fullPath);
    }

    /// <summary>
    /// Lấy tất cả DAO files (DAO001 đến DAO020)
    /// </summary>
    public static string[] GetAllDaoFiles()
    {
        return new[]
        {
            DAO001, DAO002, DAO003, DAO004, DAO005,
            DAO006, DAO007, DAO008, DAO009, DAO010,
            DAO011, DAO012, DAO013, DAO014, DAO015,
            DAO016, DAO017, DAO018, DAO019, DAO020
        };
    }

    /// <summary>
    /// Lấy tất cả named map files
    /// </summary>
    public static string[] GetAllNamedMapFiles()
    {
        return new[]
        {
            APPLICATION_MAP,
            GENERIC_MAP,
            PRODUCT_MAP,
            USER_MAP,
            CUSTOMER_MAP,
            ORDER_MAP
        };
    }
}
