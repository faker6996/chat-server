using System.Data;
using ChatServer.Infrastructure.Repositories.Base;
using Dapper;

namespace ChatServer.Infrastructure.Repositories.CallParticipant;

public class CallParticipantRepo : BaseRepository<Core.Models.CallParticipant>, ICallParticipantRepo
{
    public CallParticipantRepo(IDbConnection dbConnection) : base(dbConnection)
    {
    }

    public async Task<List<Core.Models.CallParticipant>> GetActiveParticipantsAsync(string callId)
    {
        var sql = $"SELECT * FROM {_tableName} WHERE call_id = @CallId AND left_at IS NULL ORDER BY joined_at";
        var result = await _dbConnection.QueryAsync<Core.Models.CallParticipant>(sql, new { CallId = callId });
        return result.ToList();
    }

    public async Task<List<Core.Models.CallParticipant>> GetAllParticipantsAsync(string callId)
    {
        var sql = $"SELECT * FROM {_tableName} WHERE call_id = @CallId ORDER BY joined_at";
        var result = await _dbConnection.QueryAsync<Core.Models.CallParticipant>(sql, new { CallId = callId });
        return result.ToList();
    }

    public async Task<Core.Models.CallParticipant?> GetParticipantAsync(string callId, int userId)
    {
        var sql = $"SELECT * FROM {_tableName} WHERE call_id = @CallId AND user_id = @UserId";
        return await _dbConnection.QueryFirstOrDefaultAsync<Core.Models.CallParticipant>(sql, new { CallId = callId, UserId = userId });
    }

    public async Task<bool> IsUserInCallAsync(string callId, int userId)
    {
        var sql = $"SELECT COUNT(*) FROM {_tableName} WHERE call_id = @CallId AND user_id = @UserId AND left_at IS NULL";
        var count = await _dbConnection.ExecuteScalarAsync<int>(sql, new { CallId = callId, UserId = userId });
        return count > 0;
    }

    public async Task<bool> LeaveCallAsync(string callId, int userId)
    {
        var sql = $"UPDATE {_tableName} SET left_at = @LeftAt, updated_at = @UpdatedAt WHERE call_id = @CallId AND user_id = @UserId AND left_at IS NULL";
        var rowsAffected = await _dbConnection.ExecuteAsync(sql, new 
        { 
            CallId = callId, 
            UserId = userId, 
            LeftAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        return rowsAffected > 0;
    }

    public async Task<bool> ToggleMediaAsync(string callId, int userId, string mediaType, bool enabled)
    {
        string sql;
        if (mediaType.ToLower() == "audio")
        {
            sql = $"UPDATE {_tableName} SET is_audio_enabled = @Enabled, updated_at = @UpdatedAt WHERE call_id = @CallId AND user_id = @UserId AND left_at IS NULL";
        }
        else if (mediaType.ToLower() == "video")
        {
            sql = $"UPDATE {_tableName} SET is_video_enabled = @Enabled, updated_at = @UpdatedAt WHERE call_id = @CallId AND user_id = @UserId AND left_at IS NULL";
        }
        else
        {
            return false;
        }

        var rowsAffected = await _dbConnection.ExecuteAsync(sql, new 
        { 
            CallId = callId, 
            UserId = userId, 
            Enabled = enabled,
            UpdatedAt = DateTime.UtcNow
        });
        return rowsAffected > 0;
    }

    public async Task<bool> UpdateConnectionQualityAsync(string callId, int userId, string quality)
    {
        var sql = $"UPDATE {_tableName} SET connection_quality = @Quality, updated_at = @UpdatedAt WHERE call_id = @CallId AND user_id = @UserId AND left_at IS NULL";
        var rowsAffected = await _dbConnection.ExecuteAsync(sql, new 
        { 
            CallId = callId, 
            UserId = userId, 
            Quality = quality,
            UpdatedAt = DateTime.UtcNow
        });
        return rowsAffected > 0;
    }

    public async Task<int> GetActiveParticipantsCountAsync(string callId)
    {
        var sql = $"SELECT COUNT(*) FROM {_tableName} WHERE call_id = @CallId AND left_at IS NULL";
        return await _dbConnection.ExecuteScalarAsync<int>(sql, new { CallId = callId });
    }
}