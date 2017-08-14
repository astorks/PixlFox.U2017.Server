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
using System.Collections.Concurrent;
using PixlFox.Gaming.GameServer.Attributes;

namespace PixlFox.U2017.WorldServer.Components
{
    class MasterServerConnection : GameComponent
    {
        public NetClient Client { get; set; }

        private bool Connected { get; set; }
        private MasterServerConnectionConfig ConfigData { get; set; }
        [Inject] private ConfigService Config { get; set; }

        private ConcurrentDictionary<string, VerificationResult> VerificationResults { get; } = new ConcurrentDictionary<string, VerificationResult>();

        public override void Initialize(Core gameCore)
        {
            Config.Changed += Config_Changed;
            ConfigData = Config.GetSectionAs<MasterServerConnectionConfig>("masterServerConnection");

            ConnectToServer();
        }

        private void Config_Changed(object sender, EventArgs e)
        {
            ConfigData = Config.GetSectionAs<MasterServerConnectionConfig>("masterServerConnection");
        }

        public void ConnectToServer()
        {
            var netConfig = new NetPeerConfiguration(ConfigData.Identifier);
            netConfig.EnableMessageType(NetIncomingMessageType.ConnectionApproval);
            Client = new NetClient(netConfig);

            Client.Start();
            var authRequestMessage = Client.CreateMessage();
            authRequestMessage.Write(ConfigData.AuthKey);
            authRequestMessage.Write(Config.GetValue<int>("serverInfo:identifier"));
            authRequestMessage.Write(Config.GetValue<string>("serverInfo:name"));
            authRequestMessage.Write(Config.GetValue<string>("serverInfo:publicAddress"));
            authRequestMessage.Write(Config.GetValue<int>("serverNetworking:bindPort"));
            Client.Connect(NetUtility.Resolve(ConfigData.Address, ConfigData.Port), authRequestMessage);
        }

        public async void ConnectToServerDelayed()
        {
            await Task.Delay(ConfigData.RetryDelay);
            ConnectToServer();
        }

        public override void Shutdown()
        {
            if (Client != null && Client.Status == NetPeerStatus.Running)
                Client.Shutdown("SHUTDOWN");
        }

        public override void Tick(double deltaTime)
        {
            NetIncomingMessage message;
            while ((message = Client.ReadMessage()) != null)
            {
                switch (message.MessageType)
                {
                    case NetIncomingMessageType.StatusChanged:
                        switch ((NetConnectionStatus)message.PeekByte())
                        {
                            case NetConnectionStatus.Connected:
                                Connected = true;
                                logger.Info("Connected and authenticated with account server at {0}:{1}.", ConfigData.Address, ConfigData.Port);
                                break;
                            case NetConnectionStatus.Disconnected:
                                ConnectToServerDelayed();
                                if(Connected)
                                    logger.Warn("Lost connection to account server at {0}:{1}, retrying in {2} seconds...", ConfigData.Address, ConfigData.Port, (int)ConfigData.RetryDelay.TotalSeconds);
                                else
                                    logger.Warn("Failed to connect or authenticate with account server at {0}:{1}, retrying in {2} seconds...", ConfigData.Address, ConfigData.Port, (int)ConfigData.RetryDelay.TotalSeconds);

                                Connected = false;
                                break;
                        }
                        break;
                    case NetIncomingMessageType.Data: HandleIncomingMessage(message); break;
                }
            }
        }

        private void HandleIncomingMessage(NetIncomingMessage message)
        {
            uint packetId = message.ReadUInt32();

            switch (packetId)
            {
                case 0xFFFF0002: OnReceivedAuthKeyVerification(message); break;
            }
        }

        private void OnReceivedAuthKeyVerification(NetIncomingMessage message)
        {
            string authKey = message.ReadString();
            bool isVerified = message.ReadBoolean();
            if (isVerified)
            {
                ulong steamId = message.ReadUInt64();
                VerificationResults.TryAdd(authKey, new VerificationResult { AuthKey = authKey, IsVerified = true, SteamId = steamId });
            }
            else
            {
                VerificationResults.TryAdd(authKey, new VerificationResult { AuthKey = authKey, IsVerified = false });
            }
        }

        public async Task<VerificationResult> VerifyAuthKey(string authKey)
        {
            if (Client.Status != NetPeerStatus.Running)
                return new VerificationResult { AuthKey = authKey, IsVerified = false };

            var verifyAuthKeyMessage = Client.CreateMessage();
            verifyAuthKeyMessage.Write(0xFFFF0001);
            verifyAuthKeyMessage.Write(authKey);
            Client.SendMessage(verifyAuthKeyMessage, NetDeliveryMethod.ReliableOrdered);

            while (!VerificationResults.ContainsKey(authKey))
                await Task.Delay(100);

            VerificationResults.TryRemove(authKey, out VerificationResult verificationResult);
            return verificationResult;
        }
    }

    public class VerificationResult
    {
        public ulong SteamId { get; set; }
        public bool IsVerified { get; set; }
        public string AuthKey { get; set; }
    }

    public class MasterServerConnectionConfig
    {
        public string Identifier { get; set; } = "PixlFox.U2017.MasterServer.InterCommunication";
        public string Address { get; set; } = "127.0.0.1";
        public int Port { get; set; } = 35560;
        public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(10);
        public string AuthKey { get; set; } = "";
    }
}
