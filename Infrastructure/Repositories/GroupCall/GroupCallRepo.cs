using System.Data;
using ChatServer.Core.Constants;
using ChatServer.Infrastructure.Repositories.Base;
using Dapper;

namespace ChatServer.Infrastructure.Repositories.GroupCall;

public class GroupCallRepo : BaseRepository<Core.Models.GroupCall>, IGroupCallRepo
{
    public GroupCallRepo(IDbConnection dbConnection) : base(dbConnection)
    {
    }

    public async Task<Core.Models.GroupCall?> GetByStringIdAsync(string id)
    {
        var sql = $"SELECT * FROM {_tableName} WHERE id = @Id";
        return await _dbConnection.QueryFirstOrDefaultAsync<Core.Models.GroupCall>(sql, new { Id = id });
    }

    public async Task<string> InsertGroupCallAsync(Core.Models.GroupCall groupCall)
    {
        var sql = @"INSERT INTO group_calls (id, group_id, initiator_id, call_type, started_at, status, max_participants, created_at, updated_at) 
                   VALUES (@id, @group_id, @initiator_id, @call_type, @started_at, @status, @max_participants, @created_at, @updated_at)";
        
        await _dbConnection.ExecuteAsync(sql, groupCall);
        return groupCall.id;
    }

    public async Task<Core.Models.GroupCall?> GetActiveGroupCallAsync(int groupId)
    {
        var sql = $"SELECT * FROM {_tableName} WHERE group_id = @GroupId AND status = @Status ORDER BY started_at DESC LIMIT 1";
        return await _dbConnection.QueryFirstOrDefaultAsync<Core.Models.GroupCall>(sql, new { GroupId = groupId, Status = GroupCallConstants.CallStatus.Active });
    }

    public async Task<List<Core.Models.GroupCall>> GetGroupCallHistoryAsync(int groupId, int offset, int limit)
    {
        var sql = $"SELECT * FROM {_tableName} WHERE group_id = @GroupId ORDER BY started_at DESC LIMIT @Limit OFFSET @Offset";
        var result = await _dbConnection.QueryAsync<Core.Models.GroupCall>(sql, new { GroupId = groupId, Limit = limit, Offset = offset });
        return result.ToList();
    }

    public async Task<bool> UpdateGroupCallStatusAsync(string callId, string status, DateTime? endedAt = null)
    {
        var sql = $"UPDATE {_tableName} SET status = @Status, ended_at = @EndedAt, updated_at = @UpdatedAt WHERE id = @CallId";
        var rowsAffected = await _dbConnection.ExecuteAsync(sql, new 
        { 
            Status = status, 
            EndedAt = endedAt, 
            CallId = callId,
            UpdatedAt = DateTime.UtcNow
        });
        return rowsAffected > 0;
    }

    public async Task<bool> HasActiveCallAsync(int groupId)
    {
        var sql = $"SELECT COUNT(*) FROM {_tableName} WHERE group_id = @GroupId AND status = @Status";
        var count = await _dbConnection.ExecuteScalarAsync<int>(sql, new { GroupId = groupId, Status = GroupCallConstants.CallStatus.Active });
        return count > 0;
    }
}