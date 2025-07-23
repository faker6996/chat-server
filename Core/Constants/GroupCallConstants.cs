namespace ChatServer.Core.Constants;

public static class GroupCallConstants
{
    public static class CallType
    {
        public const string Audio = "audio";
        public const string Video = "video";
    }

    public static class CallStatus
    {
        public const string Active = "active";
        public const string Ended = "ended";
    }

    public static class ConnectionQuality
    {
        public const string Excellent = "excellent";
        public const string Good = "good";
        public const string Poor = "poor";
        public const string Disconnected = "disconnected";
    }

    public static class DefaultValues
    {
        public const int MaxParticipants = 10;
        public const string DefaultConnectionQuality = ConnectionQuality.Good;
        public const string DefaultCallStatus = CallStatus.Active;
    }
}