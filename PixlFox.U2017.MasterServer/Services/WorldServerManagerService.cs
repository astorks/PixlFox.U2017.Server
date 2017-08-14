using PixlFox.Gaming.GameServer.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PixlFox.Gaming.GameServer;
using System.Collections.Concurrent;
using Lidgren.Network;
using NLog;

namespace PixlFox.U2017.MasterServer.Services
{
    class WorldServerManagerService : IGameService
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();

        public ConcurrentDictionary<NetConnection, WorldServer> WorldServers { get; } = new ConcurrentDictionary<NetConnection, WorldServer>();

        public void Initialize(Core gameCore)
        {
            
        }

        public void Shutdown()
        {
            
        }

        public void OnWorldServerConnected(NetConnection interServerConnection, int identifier, string name, string address, int port)
        {
            WorldServers.TryAdd(interServerConnection, new WorldServer
            {
                Connection = interServerConnection,
                Identifier = identifier,
                Name = name,
                Address = address,
                Port = port
            });

            logger.Info("World server ({0}) {1} connected -> {2}:{3}", identifier, name, address, port);
        }

        public void OnWorldServerDisconnected(NetConnection interServerConnection)
        {
            if(WorldServers.TryRemove(interServerConnection, out WorldServer worldServer))
                logger.Warn("World server ({0}) {1} disconnected -> {2}:{3}", worldServer.Identifier, worldServer.Name, worldServer.Address, worldServer.Port);
        }
    }

    public class WorldServer
    {
        public NetConnection Connection { get; set; }

        public int Identifier { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
        public int Port { get; set; }
    }
}
