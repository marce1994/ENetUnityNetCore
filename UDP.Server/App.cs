using ENet;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UDP.Core.Model;
using UDP.Core.Model.Packet;
using UDP.Core.Model.Packet.Enum;
using UDP.Server.Model;

namespace UDP.Server
{
    public class App
    {
        private readonly ILogger<App> _logger;
        private readonly Host _server;
        private readonly ushort _port;
        private readonly int _maxClients;
        private readonly int _ticksPerSecond;

        private List<Game> _games;
        private List<Tuple<uint, Peer, char[]>> _waitingRoom;

        public App(IConfigurationRoot config, ILoggerFactory loggerFactory)
        {
            // Config loading
            _port = config.GetValue<ushort>("Port");
            _maxClients = config.GetValue<int>("MaxPlayers");
            _ticksPerSecond = config.GetValue<int>("TicksPerSecond");

            // Services
            _logger = loggerFactory.CreateLogger<App>();

            // Game initialization
            _games = new List<Game>();
            _waitingRoom = new List<Tuple<uint, Peer, char[]>>();

            // UDP Initialization
            Library.Initialize();

            _server = new Host();
            Address address = new Address();
            address.Port = _port;
            address.SetIP("0.0.0.0");
            _server.Create(address, _maxClients);

            // Other
            Console.WriteLine($"Circle ENet Server started on {_port}");
        }

        public async Task Run(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                bool polled = false;
                while (!polled)
                {
                    if (_server.CheckEvents(out Event netEvent) <= 0)
                    {
                        if (_server.Service(1000 / _ticksPerSecond, out netEvent) <= 0)
                            break;

                        polled = true;
                    }

                    switch (netEvent.Type)
                    {
                        case EventType.None:
                            break;

                        case EventType.Connect:
                            Console.WriteLine("Client connected - ID: " + netEvent.Peer.ID + ", IP: " + netEvent.Peer.IP);
                            netEvent.Peer.Timeout(32, 1000, 4000);
                            break;

                        case EventType.Disconnect:
                            Console.WriteLine("Client disconnected - ID: " + netEvent.Peer.ID + ", IP: " + netEvent.Peer.IP);
                            HandleLogout(netEvent.Peer.ID);
                            break;

                        case EventType.Timeout:
                            Console.WriteLine("Client timeout - ID: " + netEvent.Peer.ID + ", IP: " + netEvent.Peer.IP);
                            HandleLogout(netEvent.Peer.ID);
                            break;

                        case EventType.Receive:
                            Console.WriteLine("Packet received from - ID: " + netEvent.Peer.ID + ", IP: " + netEvent.Peer.IP + ", Data length: " + netEvent.Packet.Length);
                            HandlePacket(ref netEvent);
                            netEvent.Packet.Dispose();
                            break;
                    }
                }
                _server.Flush();
            }

            Library.Deinitialize();
        }

        void HandlePacket(ref Event netEvent)
        {
            var readBuffer = new byte[55];
            var readStream = new MemoryStream(readBuffer);
            var reader = new BinaryReader(readStream);

            readStream.Position = 0;
            netEvent.Packet.CopyTo(readBuffer);
            _logger.LogInformation("{@Buffer}", readBuffer);
            var packetId = (EPacketId)reader.ReadByte();

            if (packetId != EPacketId.BoardUpdateRequest)
                Console.WriteLine($"HandlePacket received: {packetId}");

            if (packetId == EPacketId.LoginRequest)
            {
                var protocol = new Protocol();
                protocol.Deserialize(readBuffer, out Login login);
                login.PlayerId = netEvent.Peer.ID;

                _logger.LogInformation("{@login}", login);

                SendLoginResponse(ref netEvent, ref login);
                Game game = new Game();

                if (_waitingRoom.Count() > 0)
                {
                    var couple = _waitingRoom.First();
                    _waitingRoom.Remove(couple);
                    game.CleanBoard();
                    game.Player1 = couple.Item2;
                    game.Player2 = netEvent.Peer;

                    game.Player1Name = couple.Item3;
                    game.Player2Name = login.PlayerName;
                    
                    var random = new Random();
                    game.PlayerTurn = random.Next(0, 1) > 0 ? couple.Item1 : login.PlayerId;

                    _games.Add(game);

                    login.PacketId = EPacketId.LoginEvent;

                    login.PlayerName = game.Player2Name;
                    login.PlayerId = game.Player2.ID;
                    SendLoginEvent(ref login, game.Player1);

                    login.PlayerName = game.Player1Name;
                    login.PlayerId = game.Player1.ID;
                    SendLoginEvent(ref login, game.Player2);
                }
                else
                {
                    _waitingRoom.Add(new Tuple<uint, Peer, char[]>(login.PlayerId, netEvent.Peer, login.PlayerName));
                }
            }
            else if (packetId == EPacketId.BoardUpdateRequest)
            {
                var protocol = new Protocol();
                protocol.Deserialize(readBuffer, out PlayerInput playerInput);
                playerInput.PlayerId = netEvent.Peer.ID;
                _logger.LogInformation("{@playerInput}", playerInput);
                OnBoardUpdateRequest(playerInput);
            }
        }

        void OnBoardUpdateRequest(PlayerInput playerInput)
        {
            var games = _games.Where(x => x.Player1.ID == playerInput.PlayerId || x.Player2.ID == playerInput.PlayerId);
            if (!games.Any())return;

            var game = games.Single();

            if (playerInput.PlayerId != game.PlayerTurn) return;

            var first_player = game.Player1.ID == playerInput.PlayerId;

            var result = XO.Evaluate(ref game.Board, new Tuple<uint, EValue>(playerInput.BoardPosition, first_player ? EValue.O : EValue.X));

            _logger.LogInformation("XO evaluation result: {result}", result);

            if (result < 0) return;

            if (result == (int)EValue.O)
                game.Player2Score++;

            if (result == (int)EValue.X)
                game.Player1Score++;

            uint nextTurnPlayerId = game.Player1.ID == game.PlayerTurn ? game.Player2.ID : game.Player1.ID;
            game.PlayerTurn = nextTurnPlayerId;

            SendGameStateChange(game.Player1, game);
            SendGameStateChange(game.Player2, game);

            if (result > 0 || !game.Board.Any(x => x == EValue.EMPTY))
            {
                game.CleanBoard();
                Task.Delay(1000).ContinueWith(t => SendGameStateChange(game.Player2, game));
                Task.Delay(1000).ContinueWith(t => SendGameStateChange(game.Player1, game));
            }
        }

        void SendGameStateChange(Peer player, Game game)
        {
            Protocol protocol = new Protocol();
            GameUpdate gameUpdate = default;
            gameUpdate.Board = game.Board;
            gameUpdate.PlayerTurn = game.PlayerTurn;
            gameUpdate.Player1ID = game.Player1.ID;
            gameUpdate.Player1Score = game.Player1Score;
            gameUpdate.Player2ID = game.Player2.ID;
            gameUpdate.Player2Score = game.Player2Score;
            gameUpdate.PacketId = EPacketId.BoardUpdateEvent;

            byte[] buffer = protocol.Serialize(gameUpdate);

            Packet packet = default;
            packet.Create(buffer);
            
            _logger.LogInformation("{@gameUpdate}",gameUpdate);
            player.Send(0,ref packet);
        }

        void SendLoginResponse(ref Event netEvent, ref Login login)
        {
            var protocol = new Protocol();
            login.PacketId = EPacketId.LoginResponse;

            var buffer = protocol.Serialize(login);
            var packet = default(Packet);
            packet.Create(buffer);

            _logger.LogInformation("{@login}", login);
            netEvent.Peer.Send(0, ref packet);
        }

        void SendLoginEvent(ref Login login, Peer peer)
        {
            var protocol = new Protocol();
            var buffer = protocol.Serialize(login);
            var packet = default(Packet);
            packet.Create(buffer);

            _logger.LogInformation("{@login}", login);
            peer.Send(0, ref packet);
        }

        void HandleLogout(uint playerId)
        {
            var peers = _games.Where(x => x.Player1.ID == playerId || x.Player2.ID == playerId).SelectMany(x => new Peer[] { x.Player1, x.Player2 });
            foreach (var peer in peers)
            {
                if (peer.ID == playerId) continue;
                peer.Reset();
            }
            _games.RemoveAll(x => x.Player1.ID == playerId || x.Player2.ID == playerId);
            _waitingRoom.RemoveAll(x => x.Item1 == playerId);
        }
    }
}