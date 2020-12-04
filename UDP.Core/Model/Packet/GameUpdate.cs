using UDP.Core.Model.Packet.Contract;
using UDP.Core.Model.Packet.Enum;

namespace UDP.Core.Model.Packet
{
    public struct GameUpdate : IPacketData
    {
        public EPacketId PacketId { get; set; }
        public EValue[] Board { get; set; }


        public uint Player1ID { get; set; }
        public uint Player2ID { get; set; }
        public uint Player1Score { get; set; }
        public uint Player2Score { get; set; }
    }
}
