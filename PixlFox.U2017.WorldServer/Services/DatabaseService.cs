using MongoDB.Driver;
using NLog;
using PixlFox.Gaming.GameServer;
using PixlFox.Gaming.GameServer.Attributes;
using PixlFox.Gaming.GameServer.DependencyInjection;
using PixlFox.Gaming.GameServer.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PixlFox.U2017.WorldServer.Services
{
    class DatabaseService : GameService
    {
        private MongoClient Client { get; set; }
        public IMongoDatabase Database { get; private set; }

        public IMongoCollection<Player> PlayersCollection { get; private set; }

        [Inject] private ConfigService Config { get; set; }
        private DatabaseConfig ConfigData { get; set; }

        public override void Initialize(Core gameCore)
        {
            Config.Changed += Config_Changed;
            ConfigData = Config.GetSectionAs<DatabaseConfig>("db");

            Client = new MongoClient(ConfigData.ConnectionString);
            Database = Client.GetDatabase(ConfigData.Database);
            PlayersCollection = Database.GetCollection<Player>("Players");
        }

        private void Config_Changed(object sender, EventArgs e)
        {
            ConfigData = Config.GetSectionAs<DatabaseConfig>("db");

            Client = new MongoClient(ConfigData.ConnectionString);
            Database = Client.GetDatabase(ConfigData.Database);
            PlayersCollection = Database.GetCollection<Player>("Players");
        }

        public async Task<List<Player>> GetAccountPlayers(ulong steamId, int worldServerIdentifier = 101)
        {
            try
            {
                var builder = Builders<Player>.Filter;
                var filter = builder.Eq(e => e.SteamId, steamId) & builder.Eq(e => e.WorldServerIdentifier, worldServerIdentifier);

                return await PlayersCollection.Find(filter).ToListAsync();
            }
            catch (Exception e)
            {
                logger.Error(e, "Failed to get account players for {0}:{1}.", steamId, worldServerIdentifier);
                return new List<Player>();
            }
        }

        public async Task<Player> GetPlayer(Guid playerId)
        {
            try
            {
                var builder = Builders<Player>.Filter;
                var filter = builder.Eq(e => e.Id, playerId);

                return await PlayersCollection.Find(filter).FirstOrDefaultAsync();
            }
            catch (Exception e)
            {
                logger.Error(e, "Failed to get player {0}.", playerId);
                return null;
            }
        }

        public async Task UpdatePlayer(Player player)
        {
            try
            {
                var builder = Builders<Player>.Filter;
                var filter = builder.Eq(e => e.Id, player.Id);

                await PlayersCollection.ReplaceOneAsync(filter, player);
            }
            catch (Exception e)
            {
                logger.Error(e, "Failed to save player {0}.", player.Id);
            }
        }

        public async Task CreatePlayer(Player player)
        {
            try
            {
                await PlayersCollection.InsertOneAsync(player);
            }
            catch (Exception e)
            {
                logger.Error(e, "Failed to create player {0}.", player.Id);
            }
        }
    }

    public class DatabaseConfig
    {
        public string ConnectionString { get; set; }
        public string Database { get; set; }
    }
}
