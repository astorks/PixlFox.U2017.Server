using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lidgren.Network;
using PixlFox.Gaming.GameServer.Interfaces;
using PixlFox.Gaming.GameServer;
using PixlFox.U2017.MasterServer.Services;
using NLog;
using PixlFox.Gaming.GameServer.DependencyInjection;
using System.Net;
using PixlFox.Gaming.GameServer.Commands;
using PixlFox.Gaming.GameServer.Attributes;

namespace PixlFox.U2017.MasterServer.Components
{
    public class InterCommunicationServer : GameComponent
    {
        private DateTime blockConnectionsAfter;

        private NetServer Server { get; set; }
        private InterCommunicationServerConfig ConfigData { get; set; }
        [Inject] private ConfigService Config { get; set; }
        [Inject] private WorldServerManagerService WorldServerManager { get; set; }
        [Inject] private AccountManagerService AccountManager { get; set; }

        public override void Initialize(Core gameCore)
        {
            Config.Changed += Config_Changed;
            ConfigData = Config.GetSectionAs<InterCommunicationServerConfig>("interCommunicationServer");

            blockConnectionsAfter = DateTime.UtcNow + ConfigData.AllowConnectionsDuration;
            logger.Info("Allowing world server connections until {0}.", blockConnectionsAfter.ToLocalTime());

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
            ConfigData = Config.GetSectionAs<InterCommunicationServerConfig>("interCommunicationServer");
        }

        public override void Shutdown()
        {
            Server.Shutdown("SHUTDOWN");
            logger.Debug("Shutdown networking.");
        }

        public override void Tick(double deltaTime)
        {
            NetIncomingMessage message;
            while ((message = Server.ReadMessage()) != null)
            {
                switch (message.MessageType)
                {
                    case NetIncomingMessageType.ConnectionApproval: HandleConnectionApproval(message); break;
                    case NetIncomingMessageType.Data: HandleIncomingMessage(message); break;
                    case NetIncomingMessageType.StatusChanged:
                        {
                            switch ((NetConnectionStatus)message.PeekByte())
                            {
                                case NetConnectionStatus.Disconnected: WorldServerManager.OnWorldServerDisconnected(message.SenderConnection); break;
                            }
                        }
                        break;
                }
            }
        }

        private void HandleConnectionApproval(NetIncomingMessage message)
        {
            if(!ConfigData.AllowConnections || DateTime.UtcNow > blockConnectionsAfter)
            {
                message.SenderConnection.Deny();
                return;
            }

            string authKey = message.ReadString();
            if (authKey == ConfigData.AuthKey)
            {
                message.SenderConnection.Approve();

                int worldServerIdentifier = message.ReadInt32();
                string worldServerName = message.ReadString();
                string worldServerAddress = message.ReadString();
                int worldServerPort = message.ReadInt32();

                WorldServerManager.OnWorldServerConnected(message.SenderConnection, worldServerIdentifier, worldServerName, worldServerAddress, worldServerPort);
            }
            else
                message.SenderConnection.Deny();
        }

        private void HandleIncomingMessage(NetIncomingMessage message)
        {
            uint packetId = message.ReadUInt32();

            switch(packetId)
            {
                case 0xFFFF0001: OnReceivedAuthVerificationRequest(message); break;
            }
        }

        private void OnReceivedAuthVerificationRequest(NetIncomingMessage message)
        {
            string authKey = message.ReadString();
            var accountInfo = AccountManager.GetAccount(authKey);

            if(accountInfo != null)
            {
                var authResponseMessage = Server.CreateMessage();
                authResponseMessage.Write(0xFFFF0002);
                authResponseMessage.Write(authKey);
                authResponseMessage.Write(true);
                authResponseMessage.Write(accountInfo.SteamId);
                Server.SendMessage(authResponseMessage, message.SenderConnection, NetDeliveryMethod.ReliableOrdered);
            }
            else
            {
                var authResponseMessage = Server.CreateMessage();
                authResponseMessage.Write(0xFFFF0002);
                authResponseMessage.Write(authKey);
                authResponseMessage.Write(false);
                Server.SendMessage(authResponseMessage, message.SenderConnection, NetDeliveryMethod.ReliableOrdered);
            }
        }

        [Command("allowWorldServerConnections")]
        public void AllowWorldServerConnections(string duration = "")
        {
            if(TimeSpan.TryParse(duration, out TimeSpan allowConnectionsDuration))
                blockConnectionsAfter = DateTime.UtcNow + allowConnectionsDuration;
            else
                blockConnectionsAfter = DateTime.UtcNow + ConfigData.AllowConnectionsDuration;

            logger.Info("Allowing world server connections until {0}.", blockConnectionsAfter.ToLocalTime());
        }
    }

    public class InterCommunicationServerConfig
    {
        public string Identifier { get; set; } = "PixlFox.U2017.MasterServer.InterCommunication";
        public string BindAddress { get; set; } = "0.0.0.0";
        public int BindPort { get; set; } = 35560;
        public bool AllowConnections { get; set; } = true;
        public TimeSpan AllowConnectionsDuration { get; set; } = TimeSpan.FromMinutes(15);
        public string AuthKey { get; set; } = "";
    }
}
