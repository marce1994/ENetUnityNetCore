using System.IO;
using UDP.Core.Model.Packet;
using UDP.Core.Model.Packet.Enum;

namespace UDP.Core.Model
{
    public class Protocol
    {
        private void InitWriter(int size)
        {
            m_buffer = new byte[size];
            m_stream = new MemoryStream(m_buffer);
            m_writer = new BinaryWriter(m_stream);
        }

        private void InitReader(byte[] buffer)
        {
            m_stream = new MemoryStream(buffer);
            m_reader = new BinaryReader(m_stream);
        }

        public byte[] Serialize(GameUpdate gameUpdate)
        {
            const int bufSize = sizeof(byte) + sizeof(byte) * 9 + sizeof(uint) + sizeof(uint) + sizeof(uint) + sizeof(uint) + sizeof(uint);
            InitWriter(bufSize);
            m_writer.Write((byte)gameUpdate.PacketId);

            for (int i = 0; i < 9; i++)
                m_writer.Write((byte)gameUpdate.Board[i]);

            m_writer.Write(gameUpdate.PlayerTurn);
            m_writer.Write(gameUpdate.Player1ID);
            m_writer.Write(gameUpdate.Player2ID);
            m_writer.Write(gameUpdate.Player1Score);
            m_writer.Write(gameUpdate.Player2Score);

            return m_buffer;
        }

        public byte[] Serialize(Login loginPacket)
        {
            int bufSize = sizeof(byte) + sizeof(uint) + sizeof(uint) + sizeof(char) * loginPacket.PlayerName.Length;
            InitWriter(bufSize);

            m_writer.Write((byte)loginPacket.PacketId);
            m_writer.Write(loginPacket.PlayerId);
            m_writer.Write((uint)loginPacket.PlayerName.Length);

            for (int i = 0; i < loginPacket.PlayerName.Length; i++)
                m_writer.Write(loginPacket.PlayerName[i]);

            return m_buffer;
        }

        public byte[] Serialize(PlayerInput playerInput)
        {
            const int bufSize = sizeof(byte) + sizeof(uint) + sizeof(uint) + sizeof(byte);
            InitWriter(bufSize);

            m_writer.Write((byte)playerInput.PacketId);
            m_writer.Write(playerInput.PlayerId);
            m_writer.Write(playerInput.BoardPosition);

            return m_buffer;
        }

        public void Deserialize(byte[] buf, out GameUpdate gameUpdate)
        {
            InitReader(buf);

            m_stream.Write(buf, 0, buf.Length);
            m_stream.Position = 0;

            gameUpdate = default;
            gameUpdate.PacketId = (EPacketId)m_reader.ReadByte();
            gameUpdate.Board = new EValue[9];

            for (int i = 0; i < 9; i++)
                gameUpdate.Board[i] = (EValue)m_reader.ReadByte();

            gameUpdate.PlayerTurn = m_reader.ReadUInt32();
            gameUpdate.Player1ID = m_reader.ReadUInt32();
            gameUpdate.Player2ID = m_reader.ReadUInt32();
            gameUpdate.Player1Score = m_reader.ReadUInt32();
            gameUpdate.Player2Score = m_reader.ReadUInt32();
        }

        public void Deserialize(byte[] buf, out PacketInfo packetInfo)
        {
            InitReader(buf);

            m_stream.Write(buf, 0, buf.Length);
            m_stream.Position = 0;

            packetInfo = default;
            packetInfo.PacketId = (EPacketId)m_reader.ReadByte();
        }

        public void Deserialize(byte[] buf, out Login login)
        {
            InitReader(buf);

            m_stream.Write(buf, 0, buf.Length);
            m_stream.Position = 0;

            login = default;
            login.PacketId = (EPacketId)m_reader.ReadByte();
            login.PlayerId = m_reader.ReadUInt32();
            var nameLenght = m_reader.ReadUInt32();
            login.PlayerName = m_reader.ReadChars((int)nameLenght);
        }

        public void Deserialize(byte[] buf, out PlayerInput playerInput)
        {

            InitReader(buf);

            m_stream.Write(buf, 0, buf.Length);
            m_stream.Position = 0;

            playerInput = default;
            playerInput.PacketId = (EPacketId)m_reader.ReadByte();
            playerInput.PlayerId = m_reader.ReadUInt32();
            playerInput.BoardPosition = m_reader.ReadUInt32();
        }

        private BinaryWriter m_writer;
        private BinaryReader m_reader;
        private MemoryStream m_stream;
        private byte[] m_buffer;
    }
}