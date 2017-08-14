using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PixlFox.U2017.WorldServer.DataModels
{
    public class Mover
    {
        public int Id { get; set; }
        public string InternalName { get; set; }
        public string Name { get; set; }

        public int Level { get; set; }
        public float BaseDefense { get; set; }
        public float BaseAttackDamage { get; set; }
        public float AttackDelay { get; set; }
        public float MaxHealth { get; set; }
        public float MaxAttackDistance { get; set; } = 3f;

        public float MinAgroDistance { get; set; } = 3f;
        public float MaxAgroDistance { get; set; } = 15f;

        public float MovementSpeed { get; set; } = 3f;

        public float BaseExp { get; set; } = 1f;
    }
}
