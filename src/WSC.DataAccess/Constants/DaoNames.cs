namespace WSC.DataAccess.Constants;

/// <summary>
/// DAO Name Constants - Giống pattern trong MrFu.SmartCheck
/// Dùng để reference DAO names thay vì hardcode strings
/// </summary>
public static class DaoNames
{
    // ═══════════════════════════════════════════════════════════════
    // DAO Names (DAO000 - DAO099)
    // ═══════════════════════════════════════════════════════════════

    public const string DAO000 = "DAO000";
    public const string DAO001 = "DAO001";
    public const string DAO002 = "DAO002";
    public const string DAO003 = "DAO003";
    public const string DAO004 = "DAO004";
    public const string DAO005 = "DAO005";
    public const string DAO006 = "DAO006";
    public const string DAO007 = "DAO007";
    public const string DAO008 = "DAO008";
    public const string DAO009 = "DAO009";
    public const string DAO010 = "DAO010";
    public const string DAO011 = "DAO011";
    public const string DAO012 = "DAO012";
    public const string DAO013 = "DAO013";
    public const string DAO014 = "DAO014";
    public const string DAO015 = "DAO015";
    public const string DAO016 = "DAO016";
    public const string DAO017 = "DAO017";
    public const string DAO018 = "DAO018";
    public const string DAO019 = "DAO019";
    public const string DAO020 = "DAO020";

    // Có thể thêm DAO021 - DAO099 nếu cần...

    // ═══════════════════════════════════════════════════════════════
    // Named DAOs (Descriptive names)
    // ═══════════════════════════════════════════════════════════════

    /// <summary>Order management DAO</summary>
    public const string ORDER = "Order";

    /// <summary>Customer management DAO</summary>
    public const string CUSTOMER = "Customer";

    /// <summary>Product catalog DAO</summary>
    public const string PRODUCT = "Product";

    /// <summary>Inventory tracking DAO</summary>
    public const string INVENTORY = "Inventory";

    /// <summary>Payment processing DAO</summary>
    public const string PAYMENT = "Payment";

    /// <summary>Shipping management DAO</summary>
    public const string SHIPPING = "Shipping";

    /// <summary>User management DAO</summary>
    public const string USER = "User";

    /// <summary>Group/Role management DAO</summary>
    public const string GROUP = "Group";

    /// <summary>Application settings DAO</summary>
    public const string APPLICATION = "Application";

    /// <summary>Reporting queries DAO</summary>
    public const string REPORT = "Report";

    /// <summary>Generic utilities DAO</summary>
    public const string GENERIC = "Generic";

    // ═══════════════════════════════════════════════════════════════
    // Helper Methods
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Lấy tất cả DAO names (DAO000 - DAO020)
    /// </summary>
    public static string[] GetAllDaoNumbers()
    {
        return new[]
        {
            DAO000, DAO001, DAO002, DAO003, DAO004, DAO005,
            DAO006, DAO007, DAO008, DAO009, DAO010, DAO011,
            DAO012, DAO013, DAO014, DAO015, DAO016, DAO017,
            DAO018, DAO019, DAO020
        };
    }

    /// <summary>
    /// Lấy tất cả named DAOs
    /// </summary>
    public static string[] GetAllNamedDaos()
    {
        return new[]
        {
            ORDER, CUSTOMER, PRODUCT, INVENTORY, PAYMENT, SHIPPING,
            USER, GROUP, APPLICATION, REPORT, GENERIC
        };
    }

    /// <summary>
    /// Kiểm tra DAO name có hợp lệ không
    /// </summary>
    public static bool IsValid(string daoName)
    {
        if (string.IsNullOrWhiteSpace(daoName))
            return false;

        // Check if it's a DAO number
        if (daoName.StartsWith("DAO", StringComparison.OrdinalIgnoreCase))
            return true;

        // Check if it's a named DAO
        var namedDaos = GetAllNamedDaos();
        return namedDaos.Contains(daoName, StringComparer.OrdinalIgnoreCase);
    }
}
