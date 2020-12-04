namespace UDP.Core.Model.Packet.Enum
{
    public enum EPacketId : byte
    {
        LoginRequest = 1,
        LoginResponse,
        LoginEvent,
        BoardUpdateRequest,
        BoardUpdateEvent,
        GameStartedEvent,
        LogoutEvent
    }
}
