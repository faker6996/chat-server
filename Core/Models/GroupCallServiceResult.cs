namespace ChatServer.Core.Models;

public class GroupCallServiceResult
{
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public object? Data { get; set; }

    public GroupCallServiceResult(bool isSuccess, string? errorMessage = null, object? data = null)
    {
        IsSuccess = isSuccess;
        ErrorMessage = errorMessage;
        Data = data;
    }

    // Static factory methods for convenience
    public static GroupCallServiceResult Success(object? data = null)
        => new(true, null, data);

    public static GroupCallServiceResult Failure(string errorMessage)
        => new(false, errorMessage);
}