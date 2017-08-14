using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PixlFox.U2017.WorldServer.DataModels
{
    public class Item
    {
        public int Id { get; set; }
        public string InternalName { get; set; }
        public string AttachmentPoint { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int MaxStackSize { get; set; } = 1;
        public ItemType Type { get; set; } = ItemType.DEFAULT;
        public ItemRarity Rarity { get; set; } = ItemRarity.DEFAULT;
        public PlayerGender Gender { get; set; } = PlayerGender.ANY;
        public int RequiredMinimumLevel { get; set; } = 0;
        public PlayerClass RequiredClass { get; set; } = 0;
        public float CooldownTime { get; set; } = 0;
        public float BaseDamage { get; set; }
        public float BaseDefense { get; set; }
    }

    public enum ItemType
    {
        DEFAULT,
        DELETED,
        QUEST,
        ARMOR_HEAD,
        ARMOR_CHEST,
        ARMOR_LEGGINGS,
        ARMOR_BOOTS,
        EQUIP_PRIMARY,
        EQUIP_SECONDARY,
        CONSUMABLE
    }

    public enum ItemRarity
    {
        DEFAULT,
        COMMON,
        UNCOMMON,
        LEGENDARY,
        ULTIMATE,
        GODLY
    }
}
