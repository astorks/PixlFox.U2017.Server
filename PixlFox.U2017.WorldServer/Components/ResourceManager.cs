using PixlFox.Gaming.GameServer.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PixlFox.Gaming.GameServer;
using PixlFox.U2017.WorldServer.DataModels;
using System.IO;
using Newtonsoft.Json;

namespace PixlFox.U2017.WorldServer.Components
{
    class ResourceManager : IGameService
    {
        public static ResourceManager Instance { get; private set; }

        private Dictionary<int, Item> Items { get; } = new Dictionary<int, Item>();
        private Dictionary<int, Mover> Movers { get; } = new Dictionary<int, Mover>();

        public void Initialize(Core gameCore)
        {
            Instance = this;

            var items = JsonConvert.DeserializeObject<Item[]>(File.ReadAllText("../data/items.json"));
            foreach (var item in items)
                Items.Add(item.Id, item);

            var movers = JsonConvert.DeserializeObject<Mover[]>(File.ReadAllText("../data/movers.json"));
            foreach (var mover in movers)
                Movers.Add(mover.Id, mover);
        }

        public void Shutdown()
        {
            
        }

        public Item GetItem(int itemId)
        {
            if (Items.ContainsKey(itemId))
                return Items[itemId];

            return null;
        }

        public Mover GetMover(int moverId)
        {
            if (Movers.ContainsKey(moverId))
                return Movers[moverId];

            return null;
        }
    }
}
