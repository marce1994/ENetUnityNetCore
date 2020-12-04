using UDP.Core.Model.Packet.Enum;

namespace UDP.Core.Model.Packet.Contract
{
    public interface IPacketData
    {
        EPacketId PacketId { get; set; }
    }
}
