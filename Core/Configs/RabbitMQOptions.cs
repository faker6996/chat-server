namespace ChatServer.Configs
{
    public sealed class RabbitMQOptions
    {
        public string HostName { get; init; } = default!;
        public string UserName { get; init; } = default!;
        public string Password { get; init; } = default!;
    }
}