using UDP.Core.Model.Packet.Contract;
using UDP.Core.Model.Packet.Enum;

namespace UDP.Core.Model.Packet
{
    public struct PacketInfo : IPacketData
    {
        public EPacketId PacketId { get; set; }
    }
}
