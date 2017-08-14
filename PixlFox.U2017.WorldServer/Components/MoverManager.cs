using PixlFox.Gaming.GameServer.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using PixlFox.Gaming.GameServer;
using System.Collections.Concurrent;
using Newtonsoft.Json;
using System.IO;
using NLog;
using PixlFox.Gaming.GameServer.DependencyInjection;
using Lidgren.Network;
using PixlFox.U2017.WorldServer.DataModels;

namespace PixlFox.U2017.WorldServer.Components
{
    class MoverManager : IGameComponent
    {
        private static NLog.Logger logger = LogManager.GetCurrentClassLogger();

        public SpawnedMover[] SpawnedMovers { get; } = new SpawnedMover[20480];
        public int CurrentMaxSpawnedMover { get; set; } = 0;


        [GameComponentDependency] private NetworkingComponent Networking { get; set; }
        [GameComponentDependency] private PlayerManager PlayerManager { get; set; }

        public void Initialize(Core gameCore)
        {
            SpawnMovers(1, new Vector3(5, 70, 5), 10, 25);
        }

        public void Shutdown()
        {
            
        }

        public void SpawnMovers(int moverId, Vector3 position, float radius, int count)
        {
            for(var i = 0; i < count; i++)
            {
                var spawnedMover = new SpawnedMover
                {
                    Id = Guid.NewGuid(),
                    Position = position,
                    MoverId = moverId,
                    WorldId = 1
                };

                SpawnedMovers[CurrentMaxSpawnedMover] = spawnedMover;
                CurrentMaxSpawnedMover++;
            }
        }

        public void Tick(double deltaTime)
        {
            Parallel.For(0, PlayerManager.CurrentPlayerCount, (i) =>
            {
                var connectedPlayer = PlayerManager.Players[i];
                var nearbyMovers = GetNearbyMovers(connectedPlayer, 100f, out int moverCount);
                var moverPositionUpdateMessage = Networking.Server.CreateMessage(PacketId.MOVER_POSITION_UPDATE, (moverCount * 40) + 4);

                moverPositionUpdateMessage.Write(moverCount);

                for (var j = 0; j < moverCount; j++)
                {
                    var nearbyMover = nearbyMovers[j];

                    if (nearbyMover.CurrentTarget == connectedPlayer && Vector3.Distance(connectedPlayer.Position, nearbyMover.Position) > nearbyMover.Mover.MaxAgroDistance)
                        nearbyMover.CurrentTarget = null;
                    if (nearbyMover.CurrentTarget == null && Vector3.Distance(connectedPlayer.Position, nearbyMover.Position) <= nearbyMover.Mover.MinAgroDistance)
                        nearbyMover.CurrentTarget = connectedPlayer;

                    nearbyMover.TimeSinceLastAttack += (float)deltaTime;

                    moverPositionUpdateMessage.Write(nearbyMover.Id);
                    moverPositionUpdateMessage.Write(nearbyMover.Position);
                    moverPositionUpdateMessage.Write(nearbyMover.Rotation);
                }

                Networking.Server.SendMessage(moverPositionUpdateMessage, connectedPlayer.NetworkConnection, NetDeliveryMethod.Unreliable);
            });
        }

        public SpawnedMover[] GetNearbyMovers(Player player, float distance, out int moverCount)
        {
            moverCount = 0;
            var spawnedMovers = new SpawnedMover[20480];

            for (var i = 0; i < CurrentMaxSpawnedMover; i++)
            {
                var spawnedMover = SpawnedMovers[i];

                if (player.WorldId == spawnedMover.WorldId && Vector3.Distance(player.Position, spawnedMover.Position) <= distance)
                {
                    spawnedMovers[moverCount] = spawnedMover;
                    moverCount++;
                }
            }

            return spawnedMovers;
        }
    }

    public class SpawnedMover
    {
        public Guid Id { get; set; }
        public int MoverId { get; set; }
        public Mover Mover
        {
            get
            {
                return Program.GameCore.GetService<ResourceManager>().GetMover(MoverId);
            }
        }
        public Vector3 Position { get; set; }
        public Vector3 Rotation { get; set; }
        public int WorldId { get; set; }

        public Player CurrentTarget { get; set; }

        public float CurrentHealth { get; set; }

        public float TimeSinceLastAttack { get; set; } = 0f;
    }
}
