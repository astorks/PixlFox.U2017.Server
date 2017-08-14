using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PixlFox.U2017.WorldServer.DataModels
{
    public class Account
    {
        public ulong SteamId { get; set; }
        public bool IsAdmin { get; set; }
        public List<Guid> PlayerIds { get; set; } = new List<Guid>();
    }
}
