namespace ChatServer.Infrastructure.Repositories.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public class TableAttribute : Attribute
{
    public string TableName { get; }
    public TableAttribute(string tableName)
    {
        TableName = tableName;
    }
}