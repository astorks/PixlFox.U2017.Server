using Lidgren.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PixlFox.U2017
{
    public static class NetworkExtensions
    {
#if WORLD_SERVER
        public static ulong GetSteamId(this NetConnection connection)
        {
            if (connection.Tag is ConnectionInfo connectionInfo)
                return connectionInfo.SteamId;

            return 0;
        }

        public static ulong GetSteamId(this NetIncomingMessage message)
        {
            return message.SenderConnection.GetSteamId();
        }

        public static Player GetPlayer(this NetConnection connection)
        {
            var connectionInfo = connection.Tag as ConnectionInfo;
            return connectionInfo?.Player;
        }

        public static Player GetPlayer(this NetIncomingMessage message)
        {
            return message.SenderConnection.GetPlayer();
        }

        public static void SetPlayer(this NetConnection connection, Player player)
        {
            if (connection.Tag is ConnectionInfo connectionInfo)
                connectionInfo.Player = player;
        }

        public static void SetPlayer(this NetIncomingMessage message, Player player)
        {
            message.SenderConnection.SetPlayer(player);
        }
#endif

        public static Guid ReadGuid(this NetIncomingMessage message)
        {
            return new Guid(message.ReadBytes(16));
        }

        public static void Write(this NetOutgoingMessage message, Guid value)
        {
            message.Write(value.ToByteArray());
        }

        public static PacketId ReadPacketId(this NetIncomingMessage message)
        {
            return (PacketId)message.ReadUInt32();
        }

        public static NetOutgoingMessage CreateMessage(this NetServer server, PacketId packetId)
        {
            var message = server.CreateMessage();
            message.Write((uint)packetId);
            return message;
        }

        public static NetOutgoingMessage CreateMessage(this NetServer server, PacketId packetId, int initialCapacity)
        {
            var message = server.CreateMessage(initialCapacity + 4);
            message.Write((uint)packetId);
            return message;
        }

        public static NetOutgoingMessage CreateMessage(this NetClient client, PacketId packetId)
        {
            var message = client.CreateMessage();
            message.Write((uint)packetId);
            return message;
        }

        public static NetOutgoingMessage CreateMessage(this NetClient client, PacketId packetId, int initialCapacity)
        {
            var message = client.CreateMessage(initialCapacity + 4);
            message.Write((uint)packetId);
            return message;
        }
    }

    public enum PacketId : uint
    {
        ACCOUNT_PLAYER_LIST_REQUEST = 0xFF000001,
        ACCOUNT_PLAYER_LIST_RESPONSE = 0xFF000002,


        PLAYER_JOIN_REQUEST = 0xEE000001,
        PLAYER_JOIN_RESPONSE = 0xEE000002,
        PLAYER_JOIN = 0xEE000003,
        PLAYER_DISCONNECT = 0xEE000004,
        PLAYER_CREATE_REQUEST = 0xEE000005,
        PLAYER_IDENTIFIER_CHANGED = 0xEE000006,

        PLAYER_INVENTORY_SWAP_REQUEST = 0xEE010001,
        PLAYER_INVENTORY_UPDATE = 0xEE010002,

        PLAYER_CHAT_MESSAGE_REQUEST = 0xEE020001,
        PLAYER_CHAT_MESSAGE = 0xEE020002,

        PLAYER_POSITION_UPDATE_REQUEST = 0xCC01,
        PLAYER_POSITION_UPDATE = 0xCC02,
        LOCAL_PLAYER_POSITION_UPDATE = 0xCC03,
        PLAYER_UPDATE = 0xCC04,

        MOVER_POSITION_UPDATE = 0xCD02,
        MOVER_UPDATE = 0xCD04,
    }

#if WORLD_SERVER
    class ConnectionInfo
    {
        public ulong SteamId { get; }
        public Player Player { get; set; }

        public ConnectionInfo(ulong steamId)
        {
            SteamId = steamId;
        }
    }
#endif
}
