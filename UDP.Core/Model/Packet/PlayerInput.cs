using UDP.Core.Model.Packet.Contract;
using UDP.Core.Model.Packet.Enum;

namespace UDP.Core.Model.Packet
{
    public struct PlayerInput : IPacketData
    {
        public EPacketId PacketId { get; set; }
        public uint PlayerId { get; set; }
        public uint BoardPosition { get; set; }
    }
}
