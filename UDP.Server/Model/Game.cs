using ENet;
using UDP.Core.Model.Packet.Enum;

namespace UDP.Server.Model
{
    public class Game
    {
        public Peer Player1 { get; set; }
        public Peer Player2 { get; set; }

        public uint PlayerTurn { get; set; }

        public uint Player1Score { get; set; }
        public uint Player2Score { get; set; }

        public char[] Player1Name { get; set; }
        public char[] Player2Name { get; set; }

        public EValue[] Board;

        public void CleanBoard()
        {
            Board = new EValue[]
            {
                    EValue.EMPTY, EValue.EMPTY, EValue.EMPTY,
                    EValue.EMPTY, EValue.EMPTY, EValue.EMPTY,
                    EValue.EMPTY, EValue.EMPTY, EValue.EMPTY,
            };
        }
    }
}
