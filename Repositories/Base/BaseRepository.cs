using System.Data;
using System.Reflection;
using System.Text;
using ChatServer.Repositories.Attributes;
using Dapper;

namespace ChatServer.Repositories.Base;

public abstract class BaseRepository<T> : IBaseRepository<T> where T : class
{
    protected readonly IDbConnection _dbConnection;
    protected readonly string _tableName;

    protected BaseRepository(IDbConnection dbConnection)
    {
        _dbConnection = dbConnection;
        _tableName = GetTableName();
    }

    private string GetTableName()
    {
        var type = typeof(T);
        var tableAttr = type.GetCustomAttribute<TableAttribute>();
        if (tableAttr != null)
        {
            return tableAttr.TableName;
        }
        // Nếu không có attribute, tự động chuyển tên class thành snake_case
        // Ví dụ: "ChatMessage" -> "chat_messages"
        return ToSnakeCase(type.Name) + "s";
    }

    // Helper để lấy danh sách các thuộc tính có thể map với cột DB
    private static IEnumerable<PropertyInfo> GetMappableProperties()
    {
        return typeof(T).GetProperties().Where(p => p.GetCustomAttribute<NotMappedAttribute>() == null);
    }

    // Helper để lấy tên cột khóa chính
    private static string GetKeyColumnName()
    {
        var keyProperty = GetMappableProperties().FirstOrDefault(p => p.GetCustomAttribute<KeyAttribute>() != null);
        return keyProperty?.Name ?? "id"; // Mặc định là 'id'
    }

    public virtual async Task<T?> GetByIdAsync(long id)
    {
        var sql = $"SELECT * FROM {_tableName} WHERE {GetKeyColumnName()} = @Id";
        return await _dbConnection.QueryFirstOrDefaultAsync<T>(sql, new { Id = id });
    }

    public virtual async Task<IEnumerable<T>> GetAllAsync()
    {
        var sql = $"SELECT * FROM {_tableName}";
        return await _dbConnection.QueryAsync<T>(sql);
    }

    public virtual async Task<long> InsertAsync(T entity)
    {
        var properties = GetMappableProperties().Where(p => p.GetCustomAttribute<KeyAttribute>() == null);
        var columns = string.Join(", ", properties.Select(p => p.Name));
        var values = string.Join(", ", properties.Select(p => $"@{p.Name}"));

        var sql = $"INSERT INTO {_tableName} ({columns}) VALUES ({values}) RETURNING {GetKeyColumnName()};";

        return await _dbConnection.ExecuteScalarAsync<long>(sql, entity);
    }

    public virtual async Task<bool> UpdateAsync(T entity)
    {
        var keyColumn = GetKeyColumnName();
        var keyProperty = typeof(T).GetProperty(keyColumn)
            ?? throw new Exception($"{typeof(T).Name} does not have a property named {keyColumn}.");

        var properties = GetMappableProperties().Where(p => p.Name != keyColumn);
        var setClauses = string.Join(", ", properties.Select(p => $"{p.Name} = @{p.Name}"));

        var sql = $"UPDATE {_tableName} SET {setClauses} WHERE {keyColumn} = @{keyColumn}";

        var rowsAffected = await _dbConnection.ExecuteAsync(sql, entity);
        return rowsAffected > 0;
    }

    public virtual async Task<bool> DeleteAsync(long id)
    {
        var sql = $"DELETE FROM {_tableName} WHERE {GetKeyColumnName()} = @Id";
        var rowsAffected = await _dbConnection.ExecuteAsync(sql, new { Id = id });
        return rowsAffected > 0;
    }

    // Tiện ích chuyển đổi PascalCase sang snake_case
    private static string ToSnakeCase(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;

        var builder = new StringBuilder();
        builder.Append(char.ToLower(text[0]));
        for (int i = 1; i < text.Length; i++)
        {
            if (char.IsUpper(text[i]))
            {
                builder.Append('_');
                builder.Append(char.ToLower(text[i]));
            }
            else
            {
                builder.Append(text[i]);
            }
        }
        return builder.ToString();
    }
}