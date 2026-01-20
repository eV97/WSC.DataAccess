# Hướng dẫn Sử dụng CDATA trong SQL Maps

Guide chi tiết về cách sử dụng CDATA sections trong SQL Map XML files.

## 📋 Mục lục

1. [CDATA là gì?](#cdata-là-gì)
2. [Khi nào cần dùng CDATA?](#khi-nào-cần-dùng-cdata)
3. [Syntax và Cách dùng](#syntax-và-cách-dùng)
4. [Ví dụ Thực tế](#ví-dụ-thực-tế)
5. [CDATA vs XML Entities](#cdata-vs-xml-entities)
6. [Best Practices](#best-practices)
7. [Common Mistakes](#common-mistakes)

---

## CDATA là gì?

**CDATA** (Character Data) là một section đặc biệt trong XML cho phép bạn viết text mà không cần escape các ký tự đặc biệt.

### Vấn đề với XML

Trong XML, các ký tự sau có ý nghĩa đặc biệt:

| Ký tự | Ý nghĩa | XML Entity |
|-------|---------|------------|
| `<`   | Tag opening | `&lt;` |
| `>`   | Tag closing | `&gt;` |
| `&`   | Entity start | `&amp;` |
| `"`   | Attribute quote | `&quot;` |
| `'`   | Attribute quote | `&apos;` |

### Ví dụ Vấn đề

```xml
<!-- ❌ SAI - XML parser sẽ lỗi -->
<select id="GetProducts">
  SELECT * FROM Products
  WHERE Price < 100 AND Price > 10
</select>
```

XML parser sẽ nghĩ `< 100` là một XML tag!

### Giải pháp 1: XML Entities

```xml
<!-- ✅ ĐÚNG - Nhưng khó đọc -->
<select id="GetProducts">
  SELECT * FROM Products
  WHERE Price &lt; 100 AND Price &gt; 10
</select>
```

### Giải pháp 2: CDATA (Recommended)

```xml
<!-- ✅ ĐÚNG - Dễ đọc hơn -->
<select id="GetProducts">
  <![CDATA[
    SELECT * FROM Products
    WHERE Price < 100 AND Price > 10
  ]]>
</select>
```

---

## Khi nào cần dùng CDATA?

### ✅ DÙNG CDATA khi:

1. **SQL có dấu `<` hoặc `>`**
   ```sql
   WHERE Price < 100
   WHERE Quantity >= 10
   WHERE Date <= GETDATE()
   ```

2. **SQL có dấu `&`**
   ```sql
   WHERE Name LIKE '%A&B%'
   WHERE Description LIKE '%Q&A%'
   ```

3. **Complex queries với nhiều điều kiện**
   ```sql
   WHERE (Price >= @Min AND Price <= @Max)
     AND (Category = 'A' OR Category = 'B')
   ```

4. **Subqueries và CTEs**
   ```sql
   WITH CTE AS (...)
   SELECT * FROM CTE WHERE ...
   ```

5. **CASE statements**
   ```sql
   CASE
     WHEN Value < 10 THEN 'Low'
     WHEN Value >= 10 AND Value <= 50 THEN 'Medium'
     ELSE 'High'
   END
   ```

### ❌ KHÔNG CẦN CDATA khi:

1. **Simple SELECT không có điều kiện**
   ```xml
   <select id="GetAll">
     SELECT * FROM Products
   </select>
   ```

2. **INSERT/UPDATE đơn giản**
   ```xml
   <insert id="Insert">
     INSERT INTO Products (Name, Price)
     VALUES (@Name, @Price)
   </insert>
   ```

3. **SQL chỉ có `=`, `LIKE`, `IN`**
   ```xml
   <select id="GetByName">
     SELECT * FROM Products WHERE Name = @Name
   </select>
   ```

---

## Syntax và Cách dùng

### Basic Syntax

```xml
<select id="StatementId">
  <![CDATA[
    -- SQL code here
    -- Không cần escape <, >, &
  ]]>
</select>
```

### Quy tắc

1. **Bắt đầu**: `<![CDATA[`
2. **Kết thúc**: `]]>`
3. **Không được nested**: Không thể có CDATA trong CDATA
4. **Không thể chứa**: Không được có chuỗi `]]>` trong content

### Template Chuẩn

```xml
<?xml version="1.0" encoding="utf-8" ?>
<sqlMap namespace="YourEntity">

  <!-- Simple query - không cần CDATA -->
  <select id="YourEntity.SimpleQuery">
    SELECT * FROM YourTable WHERE Id = @Id
  </select>

  <!-- Complex query - dùng CDATA -->
  <select id="YourEntity.ComplexQuery">
    <![CDATA[
      SELECT *
      FROM YourTable
      WHERE Price >= @MinPrice
        AND Price <= @MaxPrice
        AND CreatedDate > DATEADD(day, -30, GETDATE())
      ORDER BY Price DESC
    ]]>
  </select>

</sqlMap>
```

---

## Ví dụ Thực tế

### Example 1: Price Range Query

**❌ Không dùng CDATA - Phải escape**

```xml
<select id="GetByPriceRange">
  SELECT * FROM Products
  WHERE Price &gt;= @MinPrice
    AND Price &lt;= @MaxPrice
  ORDER BY Price
</select>
```

**✅ Dùng CDATA - Dễ đọc**

```xml
<select id="GetByPriceRange">
  <![CDATA[
    SELECT * FROM Products
    WHERE Price >= @MinPrice
      AND Price <= @MaxPrice
    ORDER BY Price
  ]]>
</select>
```

### Example 2: Date Range with Comparisons

```xml
<select id="GetByDateRange">
  <![CDATA[
    SELECT *
    FROM Orders
    WHERE OrderDate >= @StartDate
      AND OrderDate <= @EndDate
      AND (ShippedDate IS NULL OR ShippedDate < GETDATE())
    ORDER BY OrderDate DESC
  ]]>
</select>
```

### Example 3: Complex Filter với CASE

```xml
<select id="GetProductsWithCategory">
  <![CDATA[
    SELECT
      Id,
      ProductName,
      Price,
      StockQuantity,
      CASE
        WHEN StockQuantity < 10 THEN 'Low'
        WHEN StockQuantity >= 10 AND StockQuantity <= 50 THEN 'Medium'
        WHEN StockQuantity > 50 THEN 'High'
        ELSE 'Unknown'
      END AS StockLevel
    FROM Products
    WHERE IsActive = 1
      AND (
        (@Category IS NULL OR Category = @Category)
        AND (@MinPrice IS NULL OR Price >= @MinPrice)
        AND (@MaxPrice IS NULL OR Price <= @MaxPrice)
      )
    ORDER BY ProductName
  ]]>
</select>
```

### Example 4: Subquery với NOT IN

```xml
<select id="GetAvailableProducts">
  <![CDATA[
    SELECT *
    FROM Products
    WHERE Id NOT IN (
      SELECT ProductId
      FROM OutOfStockItems
      WHERE CheckedDate > DATEADD(day, -7, GETDATE())
    )
    AND IsActive = 1
    ORDER BY ProductName
  ]]>
</select>
```

### Example 5: CTE (Common Table Expression)

```xml
<select id="GetTopSellingProducts">
  <![CDATA[
    WITH SalesData AS (
      SELECT
        p.Id,
        p.ProductName,
        SUM(oi.Quantity) AS TotalSold,
        SUM(oi.Quantity * oi.UnitPrice) AS TotalRevenue
      FROM Products p
      INNER JOIN OrderItems oi ON p.Id = oi.ProductId
      WHERE oi.OrderDate >= @SinceDate
      GROUP BY p.Id, p.ProductName
      HAVING SUM(oi.Quantity) > 0
    )
    SELECT TOP (@TopN)
      Id,
      ProductName,
      TotalSold,
      TotalRevenue,
      CASE
        WHEN TotalRevenue >= 10000 THEN 'Top Seller'
        WHEN TotalRevenue >= 5000 THEN 'Good Seller'
        ELSE 'Regular'
      END AS SellerCategory
    FROM SalesData
    WHERE TotalRevenue > @MinRevenue
    ORDER BY TotalRevenue DESC
  ]]>
</select>
```

### Example 6: Dynamic Search

```xml
<select id="DynamicSearch">
  <![CDATA[
    SELECT *
    FROM Products
    WHERE IsActive = 1
      AND (@ProductName IS NULL OR ProductName LIKE '%' + @ProductName + '%')
      AND (@Category IS NULL OR Category = @Category)
      AND (@MinPrice IS NULL OR Price >= @MinPrice)
      AND (@MaxPrice IS NULL OR Price <= @MaxPrice)
      AND (@MinStock IS NULL OR StockQuantity >= @MinStock)
    ORDER BY
      CASE WHEN @SortBy = 'Name' THEN ProductName END ASC,
      CASE WHEN @SortBy = 'Price' THEN Price END DESC,
      CASE WHEN @SortBy = 'Stock' THEN StockQuantity END DESC,
      ProductName -- Default sort
  ]]>
</select>
```

### Example 7: Query với XML Characters

```xml
<select id="SearchSpecialCharacters">
  <![CDATA[
    SELECT *
    FROM Products
    WHERE ProductName LIKE '%<tag>%'
       OR ProductName LIKE '%&%'
       OR Description LIKE '%<%'
       OR Description LIKE '%>%'
    ORDER BY ProductName
  ]]>
</select>
```

---

## CDATA vs XML Entities

### So sánh

| Aspect | CDATA | XML Entities |
|--------|-------|--------------|
| Readability | ✅ Dễ đọc | ❌ Khó đọc |
| Maintenance | ✅ Dễ maintain | ❌ Dễ sai |
| Editor Support | ⚠️ Giảm validation | ✅ Full validation |
| Performance | ✅ Same | ✅ Same |
| Best for | Complex queries | Simple queries |

### Example Comparison

**Query**: `SELECT * FROM Products WHERE Price >= 10 AND Price <= 100`

#### Cách 1: XML Entities

```xml
<select id="GetProducts">
  SELECT * FROM Products
  WHERE Price &gt;= 10 AND Price &lt;= 100
</select>
```

**Pros:**
- XML editor có thể validate
- Syntax highlighting tốt

**Cons:**
- Khó đọc
- Dễ quên escape
- Khó maintain

#### Cách 2: CDATA

```xml
<select id="GetProducts">
  <![CDATA[
    SELECT * FROM Products
    WHERE Price >= 10 AND Price <= 100
  ]]>
</select>
```

**Pros:**
- Dễ đọc như SQL bình thường
- Không cần escape
- Dễ copy/paste từ SQL editor

**Cons:**
- XML editor không validate SQL syntax
- Phải nhớ đóng CDATA tag

---

## Best Practices

### 1. Indent SQL trong CDATA

```xml
<!-- ✅ GOOD -->
<select id="GetProducts">
  <![CDATA[
    SELECT
      Id,
      ProductName,
      Price
    FROM Products
    WHERE Price >= @MinPrice
      AND IsActive = 1
    ORDER BY ProductName
  ]]>
</select>

<!-- ❌ BAD -->
<select id="GetProducts">
<![CDATA[
SELECT Id,ProductName,Price FROM Products WHERE Price>=@MinPrice AND IsActive=1 ORDER BY ProductName
]]>
</select>
```

### 2. Comment trong SQL

```xml
<select id="GetProducts">
  <![CDATA[
    -- Get active products within price range
    SELECT
      Id,
      ProductName,
      Price
    FROM Products
    WHERE IsActive = 1
      AND Price >= @MinPrice  -- Minimum price filter
      AND Price <= @MaxPrice  -- Maximum price filter
    ORDER BY ProductName
  ]]>
</select>
```

### 3. Line Breaks cho Dễ đọc

```xml
<select id="ComplexQuery">
  <![CDATA[
    SELECT
      p.Id,
      p.ProductName,
      c.CategoryName,
      s.SupplierName
    FROM Products p
    INNER JOIN Categories c
      ON p.CategoryId = c.Id
    LEFT JOIN Suppliers s
      ON p.SupplierId = s.Id
    WHERE p.IsActive = 1
      AND p.Price >= @MinPrice
    ORDER BY p.ProductName
  ]]>
</select>
```

### 4. Consistent Formatting

```xml
<!-- Chọn một style và stick with it -->
<select id="Style1">
  <![CDATA[
  SELECT * FROM Products WHERE Id = @Id
  ]]>
</select>

<!-- Hoặc -->
<select id="Style2"><![CDATA[
  SELECT * FROM Products WHERE Id = @Id
]]></select>
```

Recommend: Style 1 (CDATA trên dòng riêng)

### 5. Parameterized Queries

```xml
<!-- ✅ GOOD - Parameterized -->
<select id="GetByName">
  <![CDATA[
    SELECT * FROM Products
    WHERE ProductName = @ProductName
  ]]>
</select>

<!-- ❌ BAD - String concatenation risk -->
<select id="GetByName">
  <![CDATA[
    SELECT * FROM Products
    WHERE ProductName = '' + @ProductName + ''
  ]]>
</select>
```

---

## Common Mistakes

### Mistake 1: Quên đóng CDATA

```xml
<!-- ❌ ERROR -->
<select id="GetProducts">
  <![CDATA[
    SELECT * FROM Products
  <!-- Missing ]]> -->
</select>
```

**Error**: XML parse error

**Fix**:
```xml
<select id="GetProducts">
  <![CDATA[
    SELECT * FROM Products
  ]]>
</select>
```

### Mistake 2: Nested CDATA

```xml
<!-- ❌ ERROR - Cannot nest CDATA -->
<select id="GetProducts">
  <![CDATA[
    SELECT * FROM Products
    <![CDATA[ MORE STUFF ]]>
  ]]>
</select>
```

**Fix**: Remove nested CDATA

### Mistake 3: Có `]]>` trong SQL

```xml
<!-- ❌ ERROR - ]]> closes CDATA prematurely -->
<select id="GetProducts">
  <![CDATA[
    SELECT * FROM Products
    WHERE Description LIKE '%]]>%'
  ]]>
</select>
```

**Fix**: Tách CDATA hoặc dùng parameter:
```xml
<select id="GetProducts">
  <![CDATA[
    SELECT * FROM Products
    WHERE Description LIKE @Pattern
  ]]>
</select>
```

### Mistake 4: Escape trong CDATA

```xml
<!-- ❌ WRONG - Không cần escape trong CDATA -->
<select id="GetProducts">
  <![CDATA[
    SELECT * FROM Products
    WHERE Price &gt;= 10
  ]]>
</select>

<!-- ✅ CORRECT -->
<select id="GetProducts">
  <![CDATA[
    SELECT * FROM Products
    WHERE Price >= 10
  ]]>
</select>
```

### Mistake 5: Indent sai

```xml
<!-- ❌ BAD - Hard to read -->
<select id="GetProducts">
<![CDATA[SELECT * FROM Products
WHERE Price >= 10
AND IsActive = 1]]>
</select>

<!-- ✅ GOOD -->
<select id="GetProducts">
  <![CDATA[
    SELECT * FROM Products
    WHERE Price >= 10
      AND IsActive = 1
  ]]>
</select>
```

---

## Quick Reference

### CDATA Template

```xml
<select id="StatementId" resultType="YourType">
  <![CDATA[
    -- Your SQL here
    -- Can use <, >, & without escaping
  ]]>
</select>
```

### XML Entities Reference

| Character | Entity | Usage |
|-----------|--------|-------|
| `<` | `&lt;` | Less than |
| `>` | `&gt;` | Greater than |
| `&` | `&amp;` | Ampersand |
| `"` | `&quot;` | Quote |
| `'` | `&apos;` | Apostrophe |

### Decision Tree

```
Query có dấu <, >, & ?
├─ YES → Dùng CDATA
└─ NO
   └─ Query phức tạp (>3 dòng) ?
      ├─ YES → Dùng CDATA (cho dễ đọc)
      └─ NO → Không cần CDATA
```

---

## Tổng kết

### Khi nào dùng CDATA:

✅ **LUÔN DÙNG** cho:
- Queries có `<`, `>`, `&`
- Complex queries với nhiều conditions
- Queries với CASE statements
- CTEs và subqueries
- Dynamic queries

⚠️ **CÂN NHẮC** cho:
- Medium complexity queries
- Queries dễ thêm conditions sau này

❌ **KHÔNG CẦN** cho:
- Simple SELECT, INSERT, UPDATE
- Queries chỉ có `=` và `LIKE`

### Remember:

1. CDATA = "Character Data" = Raw text
2. Bắt đầu: `<![CDATA[`
3. Kết thúc: `]]>`
4. Indent cho đẹp
5. Comment SQL khi cần

---

**Happy Coding with CDATA!** 🚀
