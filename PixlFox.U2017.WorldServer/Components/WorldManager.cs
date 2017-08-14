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
using PixlFox.Gaming.GameServer.Attributes;

namespace PixlFox.U2017.WorldServer.Components
{
    class WorldManager : GameComponent
    {
        public ConcurrentDictionary<int, World> Worlds { get; } = new ConcurrentDictionary<int, World>();

        [Inject] private NetworkingComponent Networking { get; set; }
        [Inject] private PlayerManager PlayerManager { get; set; }

        public override void Initialize(Core gameCore)
        {
            World[] worlds = JsonConvert.DeserializeObject<World[]>(File.ReadAllText("../data/worlds.json"));

            foreach (var world in worlds)
            {
                if(Worlds.TryAdd(world.Id, world))
                    logger.Debug("Loaded world {0} (1).", world.InternalName, world.Id);
            }
        }

        public override void Tick(double deltaTime)
        {
            Parallel.For(0, PlayerManager.CurrentPlayerCount, (i) =>
            {
                var connectedPlayer = PlayerManager.Players[i];

                var nearbyPlayers = PlayerManager.GetNearbyPlayers(connectedPlayer, 100f, out int nearbyPlayerCount);
                var playerPositionsMessage = Networking.Server.CreateMessage(PacketId.PLAYER_POSITION_UPDATE, (49 * nearbyPlayerCount) + 4);

                playerPositionsMessage.Write(nearbyPlayerCount);

                for (var j = 0; j < nearbyPlayerCount; j++)
                {
                    var nearbyPlayer = nearbyPlayers[j];
                    if (nearbyPlayer != null)
                    {
                        playerPositionsMessage.Write(nearbyPlayer.Id);
                        playerPositionsMessage.Write(nearbyPlayer.Position);
                        playerPositionsMessage.Write(nearbyPlayer.Rotation);
                        playerPositionsMessage.Write(nearbyPlayer.MotionState);
                        playerPositionsMessage.Write(nearbyPlayer.StrafeDirectionX);
                        playerPositionsMessage.Write(nearbyPlayer.StrafeDirectionZ);
                    }
                    else
                    {
                        playerPositionsMessage.Write(Guid.Empty);
                        playerPositionsMessage.Write(Vector3.zero);
                        playerPositionsMessage.Write(Vector3.zero);
                        playerPositionsMessage.Write((byte)0x00);
                        playerPositionsMessage.Write(0f);
                        playerPositionsMessage.Write(0f);
                    }
                }

                Networking.Server.SendMessage(playerPositionsMessage, connectedPlayer.NetworkConnection, NetDeliveryMethod.Unreliable);
            });
        }
    }

    public class World
    {
        public int Id { get; set; }
        public string InternalName { get; set; }
        public string Name { get; set; }
        public Terrain Terrain { get; set; }
    }

    public class Terrain
    {
        public byte[] RawHeightMapData
        {
            get
            {
                return null;
            }
            set
            {
                if (value != null)
                {
                    var size = (int)Math.Sqrt(value.Length / 2);
                    HeightMapData = new float[size, size];

                    using (var dataStream = new MemoryStream(value))
                    {
                        using (var reader = new BinaryReader(dataStream))
                        {
                            for (int y = 0; y < size; y++)
                            {
                                for (int x = 0; x < size; x++)
                                {
                                    float v = (float)reader.ReadUInt16() / 0xFFFF;
                                    HeightMapData[y, x] = v;
                                }
                            }
                        }
                    }
                }
            }
        }

        [JsonIgnore]
        public float[,] HeightMapData { get; set; }

        public int Width { get; set; }
        public int Length { get; set; }
        public int Height { get; set; }

        public float OffsetX { get; set; }
        public float OffsetY { get; set; }
        public float OffsetZ { get; set; }

        public float GetHeight(int x, int z)
        {
            if (x < 0 || x >= HeightMapData.GetLength(0))
                return 0;
            else if (z < 0 || z >= HeightMapData.GetLength(1))
                return 0;
            else
                return HeightMapData[x, z];
        }

        public float SampleHeight(Vector3 position)
        {
            float x = ((position.x - OffsetX) * (((float)HeightMapData.GetLength(0) - 1f) / (float)Width));
            float z = ((position.z - OffsetZ) * (((float)HeightMapData.GetLength(1) - 1f) / (float)Length));

            var hx0z0 = GetHeight(Mathf.FloorToInt(x), Mathf.FloorToInt(z));
            var hx1z0 = GetHeight(Mathf.CeilToInt(x), Mathf.FloorToInt(z));
            var hx0z1 = GetHeight(Mathf.FloorToInt(x), Mathf.CeilToInt(z));
            var hx1z1 = GetHeight(Mathf.CeilToInt(x), Mathf.CeilToInt(z));

            if (hx0z0 == hx1z0 && hx1z0 == hx0z1 && hx0z1 == hx1z1)
                return ((hx0z0) * Height) + OffsetY;

            var u0v0 = hx0z0 * (Mathf.CeilToInt(x) - x) * (Mathf.CeilToInt(z) - z);
            var u1v0 = hx1z0 * (x - Mathf.FloorToInt(x)) * (Mathf.CeilToInt(z) - z);
            var u0v1 = hx0z1 * (Mathf.CeilToInt(x) - x) * (z - Mathf.FloorToInt(z));
            var u1v1 = hx1z1 * (x - Mathf.FloorToInt(x)) * (z - Mathf.FloorToInt(z));

            return ((u0v0 + u1v0 + u0v1 + u1v1) * Height) + OffsetY;
        }
    }
}
