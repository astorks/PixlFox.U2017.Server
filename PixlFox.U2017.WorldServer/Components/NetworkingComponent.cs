using PixlFox.Gaming.GameServer.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PixlFox.Gaming.GameServer;
using Lidgren.Network;
using PixlFox.U2017.WorldServer.Services;
using PixlFox.Gaming.GameServer.DependencyInjection;
using NLog;
using System.Net;
using System.Collections.Concurrent;
using PixlFox.Gaming.GameServer.Attributes;

namespace PixlFox.U2017.WorldServer.Components
{
    class NetworkingComponent : GameComponent
    {
        public NetServer Server { get; set; }

        [Inject] private ConfigService Config { get; set; }
        [Inject] private MasterServerConnection MasterServerConnection { get; set; }
        [Inject] private PlayerManager PlayerManager { get; set; }
        [Inject] private ChatService ChatService { get; set; }
        [Inject] private DatabaseService DocumentService { get; set; }
        private NetworkingConfig ConfigData { get; set; }

        public ConcurrentDictionary<ulong, NetConnection> Connections { get; } = new ConcurrentDictionary<ulong, NetConnection>();

        public override void Initialize(Core gameCore)
        {
            Config.Changed += Config_Changed;
            ConfigData = Config.GetSectionAs<NetworkingConfig>("serverNetworking");

            var netConfig = new NetPeerConfiguration(ConfigData.Identifier)
            {
                LocalAddress = IPAddress.Parse(ConfigData.BindAddress),
                Port = ConfigData.BindPort
            };

            netConfig.EnableMessageType(NetIncomingMessageType.ConnectionApproval);

            Server = new NetServer(netConfig);
            Server.Start();

            logger.Debug("Started networking, listening on {0}:{1}.", ConfigData.BindAddress, ConfigData.BindPort);
            logger.Debug("Network identifier {0}.", ConfigData.Identifier);
        }

        private void Config_Changed(object sender, EventArgs e)
        {
            ConfigData = Config.GetSectionAs<NetworkingConfig>("masterServerConnection");
        }

        public override void Shutdown()
        {
            Server.Shutdown("SHUTDOWN");
        }

        public override void Tick(double deltaTime)
        {
            NetIncomingMessage message;
            while ((message = Server.ReadMessage()) != null)
            {
                switch (message.MessageType)
                {
                    case NetIncomingMessageType.StatusChanged:
                        switch ((NetConnectionStatus)message.PeekByte())
                        {
                            case NetConnectionStatus.Connected: logger.Trace("Client from {0} with steam id {1} connected.", message.SenderConnection.RemoteEndPoint, message.GetSteamId()); break;
                            case NetConnectionStatus.Disconnected: HandlePlayerDisconnected(message);  logger.Trace("Client at {0} with steam id {1} disconnected.", message.SenderConnection.RemoteEndPoint, message.GetSteamId()); break;
                        }
                        break;
                    case NetIncomingMessageType.ConnectionApproval: HandleConnectionApproval(message); break;
                    case NetIncomingMessageType.Data: HandleIncomingMessage(message); break;
                }
            }
        }

        public async void HandlePlayerDisconnected(NetIncomingMessage message)
        {
            var player = message.GetPlayer();
            if(player != null)
                await PlayerManager.PlayerDisconnect(player.InstanceId);
        }

        private async void HandleConnectionApproval(NetIncomingMessage message)
        {
            string authKey = message.ReadString();
            var authResult = await MasterServerConnection.VerifyAuthKey(authKey);

            if (authResult.IsVerified)
            {
                message.SenderConnection.Approve();
                message.SenderConnection.Tag = new ConnectionInfo(authResult.SteamId);
                Connections.TryAdd(authResult.SteamId, message.SenderConnection);
            }
            else
                message.SenderConnection.Deny();
        }

        private void HandleIncomingMessage(NetIncomingMessage message)
        {
            PacketId packetId = message.ReadPacketId();

            switch (packetId)
            {
                case PacketId.ACCOUNT_PLAYER_LIST_REQUEST: OnReceivedAccountPlayerListRequest(message); break;
                case PacketId.PLAYER_JOIN_REQUEST: OnReceivedPlayerJoinRequest(message); break;
                case PacketId.PLAYER_INVENTORY_SWAP_REQUEST: OnReceivedPlayerInventorySwapRequest(message); break;
                case PacketId.PLAYER_POSITION_UPDATE_REQUEST: OnReceivedPlayerPositionUpdateRequest(message); break;
                case PacketId.PLAYER_CREATE_REQUEST: OnReceivedPlayerCreateRequest(message); break;
                case PacketId.PLAYER_CHAT_MESSAGE_REQUEST: OnReceivedPlayerChatMessageRequest(message); break;
                default: logger.Trace("Received unknown packet ({0:X}) from {1}.", (uint)packetId, message.SenderConnection.RemoteEndPoint); break;
            }
        }

        private async void OnReceivedAccountPlayerListRequest(NetIncomingMessage message)
        {
            try
            {
                var steamId = message.GetSteamId();
                var players = await PlayerManager.GetAccountPlayers(steamId);

                var responsePacket = Server.CreateMessage(PacketId.ACCOUNT_PLAYER_LIST_RESPONSE);
                responsePacket.Write(players.Length);

                foreach (var player in players)
                    player.SerializeToNetworkMessage(ref responsePacket, false);

                Server.SendMessage(responsePacket, message.SenderConnection, NetDeliveryMethod.ReliableOrdered);
            }
            catch { message.SenderConnection.Disconnect("ERROR"); }
        }

        private async void OnReceivedPlayerJoinRequest(NetIncomingMessage message)
        {
            var requestedPlayerId = message.ReadGuid();
            var steamId = message.GetSteamId();
            var players = await PlayerManager.GetAccountPlayers(steamId);

            var playerId = players.Where(e => e.Id == requestedPlayerId).Select(e => e.Id).FirstOrDefault();
            if (playerId != null)
            {
                var player = await PlayerManager.PlayerJoin(playerId, message.SenderConnection);
                if (player != null)
                {
                    message.SetPlayer(player);
                    var responseMessage = Server.CreateMessage(PacketId.PLAYER_JOIN_RESPONSE);
                    responseMessage.Write(true);
                    player.SerializeToNetworkMessage(ref responseMessage, true);
                    responseMessage.Write(PlayerManager.CurrentPlayerCount);
                    for (var i = 0; i < PlayerManager.CurrentPlayerCount; i++)
                    {
                        var connectedPlayer = PlayerManager.Players[i];
                        if (connectedPlayer.Id != player.Id)
                            connectedPlayer.SerializeToNetworkMessage(ref responseMessage, false);
                    }
                    Server.SendMessage(responseMessage, message.SenderConnection, NetDeliveryMethod.ReliableOrdered);
                }
                else
                {
                    var responseMessage = Server.CreateMessage(PacketId.PLAYER_JOIN_RESPONSE);
                    responseMessage.Write(false);
                    Server.SendMessage(responseMessage, message.SenderConnection, NetDeliveryMethod.ReliableOrdered);
                }
            }
            else
            {
                var responseMessage = Server.CreateMessage(PacketId.PLAYER_JOIN_RESPONSE);
                responseMessage.Write(false);
                Server.SendMessage(responseMessage, message.SenderConnection, NetDeliveryMethod.ReliableOrdered);
            }
        }

        private async void OnReceivedPlayerCreateRequest(NetIncomingMessage message)
        {
            var steamId = message.GetSteamId();
            string playerName = message.ReadString();

            var player = new Player
            {
                Id = Guid.NewGuid(),
                SteamId = steamId,
                WorldServerIdentifier = 101,
                HairStyle = "female_hair_2",
                Name = playerName,
                Inventory = new PlayerInventory(null),
                Position = new UnityEngine.Vector3(0, 70, 0),
                CurrentHealth = 100
            };
            player.Inventory.InventorySlots[1] = new InventoryItem { Id = 4, Count = 1 };
            player.Inventory.InventorySlots[2] = new InventoryItem { Id = 5, Count = 1 };
            player.Inventory.InventorySlots[3] = new InventoryItem { Id = 6, Count = 1 };
            player.Inventory.InventorySlots[4] = new InventoryItem { Id = 2, Count = 1 };
            await DocumentService.CreatePlayer(player);

            var players = await PlayerManager.GetAccountPlayers(steamId);
            var playerListResponsePacket = Server.CreateMessage(PacketId.ACCOUNT_PLAYER_LIST_RESPONSE);
            playerListResponsePacket.Write(players.Length);

            foreach (var _player in players)
                _player.SerializeToNetworkMessage(ref playerListResponsePacket, false);

            Server.SendMessage(playerListResponsePacket, message.SenderConnection, NetDeliveryMethod.ReliableOrdered);
        }

        private void OnReceivedPlayerInventorySwapRequest(NetIncomingMessage message)
        {
            var player = message.GetPlayer();
            if (player != null)
            {
                var slot1 = message.ReadInt32();
                var slot2 = message.ReadInt32();

                player.Inventory.SwapInventorySlots(slot1, slot2);
            }
        }

        private void OnReceivedPlayerChatMessageRequest(NetIncomingMessage message)
        {
            var player = message.GetPlayer();
            if (player != null)
            {
                ChatService.OnReceivedChatMessage(player, message.ReadString());
            }
        }

        private void OnReceivedPlayerPositionUpdateRequest(NetIncomingMessage message)
        {
            var player = message.GetPlayer();
            if (player != null)
            {
                player.Position = message.ReadVector3();
                player.Rotation = message.ReadVector3();
                player.MotionState = message.ReadByte();
                player.StrafeDirectionX = message.ReadFloat();
                player.StrafeDirectionZ = message.ReadFloat();

                // TODO: Server side distance checking to prevent speed and teleport cheats
            }
        }
    }

    public class NetworkingConfig
    {
        public string Identifier { get; set; } = "PixlFox.U2017.WorldServer";
        public string BindAddress { get; set; } = "127.0.0.1";
        public int BindPort { get; set; } = 35560;
    }
}
