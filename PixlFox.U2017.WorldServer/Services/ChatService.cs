using PixlFox.Gaming.GameServer.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PixlFox.Gaming.GameServer;
using PixlFox.Gaming.GameServer.DependencyInjection;
using Lidgren.Network;
using PixlFox.U2017.WorldServer.Components;
using PixlFox.Gaming.GameServer.Attributes;

namespace PixlFox.U2017.WorldServer.Services
{
    class ChatService : GameService
    {
        [Inject] private PlayerManager PlayerManager { get; set; }
        [Inject] private NetworkingComponent Networking { get; set; }

        public void OnReceivedChatMessage(Player sender, string message)
        {
            OnReceivedGeneralChatMessage(sender, message);
        }

        //public void OnReceivedChatCommand(Player sender, string message)
        //{
        //    var chatDataMessage = Networking.Server.CreateMessage(PacketId.PLAYER_CHAT_MESSAGE);
        //    chatDataMessage.Write(sender.Id);
        //    chatDataMessage.Write(sender.Name);
        //    chatDataMessage.Write((byte)ChatChannel.COMMAND);
        //    chatDataMessage.Write(message);

        //    Networking.Server.SendMessage(chatDataMessage, sender.NetworkConnection, NetDeliveryMethod.ReliableOrdered, 0);
        //}

        public void OnReceivedGeneralChatMessage(Player sender, string message)
        {
            //var chatDataMessage = Networking.Server.CreateMessage(PacketId.PLAYER_CHAT_MESSAGE);
            //chatDataMessage.Write(sender.Id);
            //chatDataMessage.Write(sender.Name);
            //chatDataMessage.Write(message);

            //var playerConnections = PlayerManager.GetNearbyPlayers(sender).Select(e => e.NetworkConnection).ToList();
            //playerConnections.Add(sender.NetworkConnection);
            //Networking.Server.SendMessage(chatDataMessage, playerConnections, NetDeliveryMethod.ReliableOrdered, 0);
        }

        //public void OnReceivedShoutChatMessage(Player sender, string message)
        //{
        //    var chatDataMessage = Networking.Server.CreateMessage(PacketId.PLAYER_CHAT_MESSAGE);
        //    chatDataMessage.Write(sender.Id);
        //    chatDataMessage.Write(sender.Name);
        //    chatDataMessage.Write((byte)ChatChannel.SHOUT);
        //    chatDataMessage.Write(message);

        //    var playerConnections = PlayerManager.GetPlayersInWorld(sender.WorldId).Select(e => e.NetworkConnection).ToList();
        //    Networking.Server.SendMessage(chatDataMessage, playerConnections, NetDeliveryMethod.ReliableOrdered, 0);
        //}
    }

    //public enum ChatChannel : byte
    //{
    //    COMMAND,
    //    GENERAL,
    //    SHOUT,
    //    PARTY,
    //    GUILD,
    //}
}
