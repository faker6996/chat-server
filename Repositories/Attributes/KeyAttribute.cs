namespace ChatServer.Repositories.Attributes;

// Dùng để đánh dấu thuộc tính nào là khóa chính
[AttributeUsage(AttributeTargets.Property)]
public class KeyAttribute : Attribute { }