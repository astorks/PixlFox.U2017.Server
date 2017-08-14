using PixlFox.Gaming.GameServer.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PixlFox.Gaming.GameServer;
using System.Collections.Concurrent;
using System.IO;
using Newtonsoft.Json;
using PixlFox.Gaming.GameServer.Commands;
using NLog;
using System.ComponentModel;
using UnityEngine;
using PixlFox.Gaming.GameServer.DependencyInjection;
using Lidgren.Network;
using PixlFox.U2017.WorldServer.Services;

namespace PixlFox.U2017.WorldServer.Components
{
    class PlayerManager : IGameComponent
    {
        public const int MAX_PLAYERS = 2048;

        private static NLog.Logger logger = LogManager.GetCurrentClassLogger();

        public Player[] Players { get; } = new Player[MAX_PLAYERS];
        public int CurrentPlayerCount { get; private set; } = 0;

        [GameComponentDependency] private NetworkingComponent Networking { get; set; }
        [GameServiceDependency] private DatabaseService Database { get; set; }

        public void Initialize(Core gameCore)
        {
            
        }

        public void Shutdown()
        {
            
        }

        public void Tick(double deltaTime)
        {
            
        }

        public async Task<Player> PlayerJoin(Guid playerId, NetConnection networkConnection)
        {
            if (CurrentPlayerCount < MAX_PLAYERS)
            {
                var player = await GetPlayer(playerId);
                player.NetworkConnection = networkConnection;
                var playerInstanceId = CurrentPlayerCount;

                if (player != null)
                {
                    player.InstanceId = playerInstanceId;
                    Players[playerInstanceId] = player;
                    CurrentPlayerCount++;

                    var playerJoinMessage = Networking.Server.CreateMessage(PacketId.PLAYER_JOIN);
                    player.SerializeToNetworkMessage(ref playerJoinMessage, false);
                    Networking.Server.SendToAll(playerJoinMessage, player.NetworkConnection, NetDeliveryMethod.ReliableOrdered, 0);

                    return player;
                }
            }

            return null;
        }

        public async Task<bool> PlayerDisconnect(int playerInstanceId)
        {
            if (playerInstanceId >= CurrentPlayerCount)
                return false;

            var player = Players[playerInstanceId];

            if (CurrentPlayerCount > 1 && CurrentPlayerCount != playerInstanceId)
            {
                Players[playerInstanceId] = Players[CurrentPlayerCount - 1];
                Players[playerInstanceId].InstanceId = playerInstanceId;
                Players[CurrentPlayerCount - 1] = null;
            }
            else
                Players[playerInstanceId] = null;

            CurrentPlayerCount--;

            await SavePlayer(player);

            player.NetworkConnection = null;
            var playerDisconnectMessage = Networking.Server.CreateMessage(PacketId.PLAYER_DISCONNECT);
            player.SerializeToNetworkMessage(ref playerDisconnectMessage, false);
            Networking.Server.SendToAll(playerDisconnectMessage, NetDeliveryMethod.ReliableOrdered);

            return true;
        }

        public Player[] GetNearbyPlayers(Player player, float maxDistance, out int count)
        {
            count = 0;
            var players = new Player[MAX_PLAYERS];

            for(var i = 0; i < CurrentPlayerCount; i++)
            {
                var otherPlayer = Players[i];
                if (player.Id != otherPlayer.Id && otherPlayer.WorldId == player.WorldId && Vector3.Distance(player.Position, otherPlayer.Position) <= maxDistance)
                {
                    players[count] = otherPlayer;
                    count++;
                }
            }

            return players;
        }

        //public IEnumerable<Player> GetPlayersInWorld(int worldId)
        //{
        //    foreach (var player in ConnectedPlayers.Values)
        //        if (player.WorldId == worldId)
        //            yield return player;
        //}

        public async Task<Player> GetPlayer(Guid id)
        {
            Player player = null;

            for(var i = 0; i < CurrentPlayerCount; i++)
            {
                if (Players[i].Id == id)
                {
                    player = Players[i];
                    break;
                }
            }

            if(player == null)
                player = await Database.GetPlayer(id);

            if(player != null && player.Inventory.AttachedPlayer == null)
                player.Inventory.AttachedPlayer = player;

            return player;
        }

        public async Task<Player[]> GetAccountPlayers(ulong steamId, int worldServerIdentifier = 101)
        {
            var accountPlayers = await Database.GetAccountPlayers(steamId, worldServerIdentifier);
            return accountPlayers.ToArray();
        }

        public async Task SavePlayer(Player player)
        {
            await Database.UpdatePlayer(player);
        }

        //[RegisteredCommand("refetchPlayerFromDataStore")]
        //[Description("Re-fetches a player by id from the data store.")]
        //public void RefetchPlayerFromDataStore([Description("The player id as a GUID format.")]string playerIdString)
        //{
        //    if (Guid.TryParse(playerIdString, out Guid playerId))
        //    {
        //        if (File.Exists("../data/players/" + playerId + ".json"))
        //        {
        //            var player = JsonConvert.DeserializeObject<Player>(File.ReadAllText("../data/players/" + playerId + ".json"));

        //            if (Players.ContainsKey(playerId))
        //                Players[playerId] = player;
        //            else
        //                Players.TryAdd(playerId, player);

        //            logger.Info("Re-fetched player {0} from data store.", playerId);
        //        }
        //        else
        //            logger.Error("Failed to re-fetched player {0} from data store, player data missing.", playerId);
        //    }
        //    else
        //        logger.Error("playerIdString \"{0}\" is not valid format for a GUID", playerIdString);
        //}

        [RegisteredCommand("givePlayerItem")]
        [Description("Gives a player an item by item id.")]
        public async void GivePlayerItem([Description("The player id as a GUID format.")]string playerIdString, int itemId, int count = 1)
        {
            if (Guid.TryParse(playerIdString, out Guid playerId))
            {
                var player = await GetPlayer(playerId);
                if (player != null)
                    player.Inventory.GiveItem(itemId, count);
                else
                    logger.Error("Failed to find player player {0}.", playerId);
            }
            else
                logger.Error("playerIdString \"{0}\" is not valid format for a GUID", playerIdString);
        }

        [RegisteredCommand("getPlayerPosition")]
        [Description("Gets a players position by id.")]
        public Vector3 GetPlayerPosition([Description("The player id as a GUID format or the players name.")]string playerIdString)
        {
            Guid.TryParse(playerIdString, out Guid playerId);
            var player = Players.Take(CurrentPlayerCount).Where(e => e.Id == playerId || e.Name.Equals(playerIdString, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();

            if (player != null)
                return player.Position;
            else
                logger.Error("Failed to find player player {0}.", playerId);

            return Vector3.zero;
        }

        [RegisteredCommand("getPlayer")]
        [Description("Gets a players by id.")]
        public Player GetPlayer([Description("The player id as a GUID format.")]string playerIdString)
        {
            Guid.TryParse(playerIdString, out Guid playerId);
            var player = Players.Take(CurrentPlayerCount).Where(e => e.Id == playerId || e.Name.Equals(playerIdString, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();

            if (player != null)
                return player;
            else
                logger.Error("Failed to find player player {0}.", playerId);

            return null;
        }

        [RegisteredCommand("teleportPlayer")]
        [Description("Teleports a player to coordinates.")]
        public void TeleportPlayer([Description("The player id as a GUID format.")]string playerIdString, float x, float y, float z)
        {
            Guid.TryParse(playerIdString, out Guid playerId);
            var player = Players.Take(CurrentPlayerCount).Where(e => e.Id == playerId || e.Name.Equals(playerIdString, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();

            if (player != null)
            {
                player.Position = new Vector3(x, y, z);
                var positionUpdateMessage = Networking.Server.CreateMessage(PacketId.LOCAL_PLAYER_POSITION_UPDATE);
                positionUpdateMessage.Write(player.Position);
                Networking.Server.SendMessage(positionUpdateMessage, player.NetworkConnection, NetDeliveryMethod.ReliableOrdered);
            }
            else
                logger.Error("Failed to find player player {0}.", playerId);
        }
    }
}
