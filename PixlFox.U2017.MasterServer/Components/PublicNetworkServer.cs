using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lidgren.Network;
using System.Net;
using System.Threading;
using PixlFox.Gaming.GameServer.Interfaces;
using PixlFox.Gaming.GameServer;
using PixlFox.Gaming.GameServer.Commands;
using PixlFox.Gaming.GameServer.DependencyInjection;
using PixlFox.U2017.MasterServer.Services;
using NLog;

namespace PixlFox.U2017.MasterServer.Components
{
    public class PublicNetworkServer : IGameComponent
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();

        private PublicNetworkServerConfig ConfigData { get; set; }
        private NetServer Server { get; set; }

        [GameServiceDependency] private ConfigService Config { get; set; }
        [GameServiceDependency] private WorldServerManagerService WorldServerManager { get; set; }
        [GameServiceDependency] private AccountManagerService AccountManager { get; set; }

        public void Initialize(Core gameCore)
        {
            Config.Changed += Config_Changed;
            ConfigData = Config.GetSectionAs<PublicNetworkServerConfig>("publicNetworkServer");

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
            ConfigData = Config.GetSectionAs<PublicNetworkServerConfig>("publicNetworkServer");
        }

        public void Shutdown()
        {
            Server.Shutdown("SHUTDOWN");
            logger.Debug("Shutdown networking.");
        }

        public void Tick(double deltaTime)
        {
            NetIncomingMessage message;
            while ((message = Server.ReadMessage()) != null)
            {
                switch (message.MessageType)
                {
                    case NetIncomingMessageType.ConnectionApproval: HandleConnectionApproval(message); break;
                }
            }
        }

        private async void HandleConnectionApproval(NetIncomingMessage message)
        {
            if (!ConfigData.AllowConnections)
            {
                message.SenderConnection.Deny();
                return;
            }

            var ticket = message.ReadBytes(1024);
            var ticketLen = message.ReadUInt32();
            var userTicketAuthResponse = await Steamworks.SteamworksWebApi.ISteamUserAuth.AuthenticateUserTicket(ticket, ticketLen);

            if (!userTicketAuthResponse.IsError && userTicketAuthResponse.Params.IsAuthenticated)
            {
                var steamId = userTicketAuthResponse.Params.SteamId64.Value;
                string authKey = AccountManager.AddAccount(steamId);

                var authResponseMessage = Server.CreateMessage();
                authResponseMessage.Write(authKey);
                authResponseMessage.Write(steamId);

                authResponseMessage.Write(WorldServerManager.WorldServers.Count);
                foreach(var worldServer in WorldServerManager.WorldServers.Values)
                {
                    authResponseMessage.Write(worldServer.Identifier);
                    authResponseMessage.Write(worldServer.Name);
                    authResponseMessage.Write(worldServer.Address);
                    authResponseMessage.Write(worldServer.Port);
                }
                
                message.SenderConnection.Approve(authResponseMessage);
            }
            else
                message.SenderConnection.Deny();
        }

        #region Commands
        [RegisteredCommand("ccu")]
        public Dictionary<string, int> CCU()
        {
            return new Dictionary<string, int>()
            {
                { "Master Server",  Server.ConnectionsCount }
            };
        }
        #endregion
    }

    public class PublicNetworkServerConfig
    {
        public string Identifier { get; set; } = "PixlFox.U2017.MasterServer.Public";
        public string BindAddress { get; set; } = "0.0.0.0";
        public int BindPort { get; set; } = 35570;
        public bool AllowConnections { get; set; } = true;
    }
}
