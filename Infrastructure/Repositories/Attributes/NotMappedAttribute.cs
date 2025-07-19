namespace ChatServer.Infrastructure.Repositories.Attributes;

// Dùng để bỏ qua một thuộc tính khi INSERT hoặc UPDATE
[AttributeUsage(AttributeTargets.Property)]
public class NotMappedAttribute : Attribute { }