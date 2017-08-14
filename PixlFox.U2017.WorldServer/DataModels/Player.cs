using Lidgren.Network;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.IdGenerators;
using Newtonsoft.Json;
using PixlFox.U2017.WorldServer;
using PixlFox.U2017.WorldServer.Components;
using PixlFox.U2017.WorldServer.DataModels;
using PixlFox.U2017.WorldServer.Services;
using PixlFox.U2017.WorldServer.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PixlFox.U2017
{
    public class Player
    {
        [JsonIgnore]
        [BsonIgnore]
        public NetConnection NetworkConnection { get; set; }

        [BsonIgnore]
        public int InstanceId { get; set; }

        [BsonId(IdGenerator = typeof(CombGuidGenerator))]
        public Guid Id { get; set; }

        [BsonElement("steamId")]
        public UInt64 SteamId { get; set; }

        [BsonElement("name")]
        public string Name { get; set; }

        [BsonElement("gender")]
        public PlayerGender Gender { get; set; } = PlayerGender.FEMALE;

        [BsonElement("hairStyle")]
        public string HairStyle { get; set; }
        //public Color HairColor { get; set; }

        [BsonElement("worldId")]
        public int WorldId { get; set; } = 1;

        [BsonElement("worldServerIdentifier")]
        public int WorldServerIdentifier { get; set; } = 101;

        [BsonElement("position")]
        [BsonSerializer(typeof(Vector3BsonSerializer))]
        public Vector3 Position { get; set; } = Vector3.zero;

        [BsonElement("rotation")]
        [BsonSerializer(typeof(Vector3BsonSerializer))]
        public Vector3 Rotation { get; set; } = Vector3.zero;

        [BsonElement("currentExp")]
        public float CurrentExp { get; set; } = 0;

        [BsonElement("strengthPoints")]
        public int StrengthPoints { get; set; } = 1;

        [BsonElement("defensePoints")]
        public int DefensePoints { get; set; } = 1;

        [BsonElement("staminaPoints")]
        public int StaminaPoints { get; set; } = 1;

        [BsonElement("maxHealthStat")]
        public float MaxHealthStat { get; set; } = 0f;

        [BsonElement("healthMultiplyerStat")]
        public float HealthMultiplyerStat { get; set; } = 1f;

        [BsonElement("maxDamageStat")]
        public float MaxDamageStat { get; set; } = 0f;

        [BsonElement("damageMiltiplierStat")]
        public float DamageMiltiplierStat { get; set; } = 1f;

        [BsonElement("maxDefenseStat")]
        public float MaxDefenseStat { get; set; } = 0f;

        [BsonElement("defenseMiltiplierStat")]
        public float DefenseMiltiplierStat { get; set; } = 1f;

        [BsonElement("currentHealth")]
        public float CurrentHealth { get; set; } = 0;

        [BsonElement("availableStatPoints")]
        public int AvailableStatPoints { get; set; } = 0;

        [BsonIgnore]
        public byte MotionState { get; set; }

        [BsonIgnore]
        public float StrafeDirectionX { get; set; }

        [BsonIgnore]
        public float StrafeDirectionZ { get; set; }

        [BsonElement("inventory")]
        public PlayerInventory Inventory { get; set; }


        public void SerializeToNetworkMessage(ref NetOutgoingMessage message, bool fullInventory = false)
        {
            message.Write(this.Id);
            message.Write(this.SteamId);
            message.Write(this.Name);
            message.Write((byte)this.Gender);
            message.Write(this.CurrentExp);
            message.Write(this.StrengthPoints);
            message.Write(this.DefensePoints);
            message.Write(this.StaminaPoints);
            message.Write(this.MaxHealthStat);
            message.Write(this.HealthMultiplyerStat);
            message.Write(this.MaxDamageStat);
            message.Write(this.DamageMiltiplierStat);
            message.Write(this.MaxDefenseStat);
            message.Write(this.DefenseMiltiplierStat);
            message.Write(this.CurrentHealth);
            message.Write(this.AvailableStatPoints);
            message.Write(this.HairStyle);

            message.Write(this.Position);
            message.Write(this.Rotation);

            Inventory.SerializeToNetworkMessage(ref message, fullInventory);
        }
    }

    public class PlayerInventory
    {
        public const int MAX_INVENTORY_SIZE = 31;
        public const int STARTING_GENERAL_INVENTORY_SLOT = 6;

        public PlayerInventory(Player attachedPlayer)
        {
            AttachedPlayer = attachedPlayer;
        }

        [BsonIgnore]
        public Player AttachedPlayer { get; set; }

        public bool GiveItem(int itemId, int count = 1)
        {
            var emptySlot = GetAvailableGeneralInventorySlot();

            if (emptySlot == -1) // Inventory full
                return false;

            InventorySlots[emptySlot] = new InventoryItem { Id = itemId, Count = count };

            var networking = Program.GameCore.GetComponent<NetworkingComponent>();

            var localInventoryUpdateMessage = networking.Server.CreateMessage(PacketId.PLAYER_INVENTORY_UPDATE);
            localInventoryUpdateMessage.Write(this.AttachedPlayer.Id);
            this.SerializeToNetworkMessage(ref localInventoryUpdateMessage, true);
            networking.Server.SendMessage(localInventoryUpdateMessage, this.AttachedPlayer.NetworkConnection, NetDeliveryMethod.ReliableOrdered);

            return true;
        }

        [BsonIgnore]
        public int ArmorHead
        {
            get
            {
                return InventorySlots[0].Id;
            }

            set
            {
                InventorySlots[0] = new InventoryItem { Id = value, Count = 1 };
            }
        }

        [BsonIgnore]
        public int ArmorChest
        {
            get
            {
                return InventorySlots[1].Id;
            }

            set
            {
                InventorySlots[1] = new InventoryItem { Id = value, Count = 1 };
            }
        }

        [BsonIgnore]
        public int ArmorLeggings
        {
            get
            {
                return InventorySlots[2].Id;
            }

            set
            {
                InventorySlots[2] = new InventoryItem { Id = value, Count = 1 };
            }
        }

        [BsonIgnore]
        public int ArmorBoots
        {
            get
            {
                return InventorySlots[3].Id;
            }

            set
            {
                InventorySlots[3] = new InventoryItem { Id = value, Count = 1 };
            }
        }

        [BsonIgnore]
        public int PrimaryEquip
        {
            get
            {
                return InventorySlots[4].Id;
            }

            set
            {
                InventorySlots[4] = new InventoryItem { Id = value, Count = 1 };
            }
        }

        [BsonIgnore]
        public int SecondaryEquip
        {
            get
            {
                return InventorySlots[5].Id;
            }

            set
            {
                InventorySlots[5] = new InventoryItem { Id = value, Count = 1 };
            }
        }

        [BsonElement("inventorySlots")]
        public InventoryItem[] InventorySlots { get; set; } = new InventoryItem[MAX_INVENTORY_SIZE];

        [BsonElement("actionBarSlots")]
        public int[] ActionBarSlots { get; set; } = new int[9];

        public int GetAvailableGeneralInventorySlot()
        {
            for (var slot = STARTING_GENERAL_INVENTORY_SLOT; slot < MAX_INVENTORY_SIZE; slot++)
            {
                if (InventorySlots[slot].Id == 0)
                    return slot;
            }

            return -1;
        }

        public void SwapInventorySlots(int slot1, int slot2)
        {
            if (slot1 == slot2 || slot1 < 0 || slot2 < 0 || slot1 >= MAX_INVENTORY_SIZE || slot2 >= MAX_INVENTORY_SIZE)
                return;

            var entityInSlot1 = InventorySlots[slot1];
            var entityInSlot2 = InventorySlots[slot2];

            if (entityInSlot1.Item != null)
            {
                if (slot2 < STARTING_GENERAL_INVENTORY_SLOT && ((entityInSlot1.Item.Gender == PlayerGender.MALE && AttachedPlayer.Gender != PlayerGender.MALE) || (entityInSlot1.Item.Gender == PlayerGender.FEMALE && AttachedPlayer.Gender != PlayerGender.FEMALE)))
                    return;

                //if (slot2 < STARTING_GENERAL_INVENTORY_SLOT && entityInSlot1.Item.RequiredMinimumLevel > AttachedPlayer.CurrentLevel)
                //    return;

                if ((slot2 < STARTING_GENERAL_INVENTORY_SLOT && slot2 == 0) && entityInSlot1.Item.Type != ItemType.ARMOR_HEAD)
                    return;
                if ((slot2 < STARTING_GENERAL_INVENTORY_SLOT && slot2 == 1) && entityInSlot1.Item.Type != ItemType.ARMOR_CHEST)
                    return;
                if ((slot2 < STARTING_GENERAL_INVENTORY_SLOT && slot2 == 2) && entityInSlot1.Item.Type != ItemType.ARMOR_LEGGINGS)
                    return;
                if ((slot2 < STARTING_GENERAL_INVENTORY_SLOT && slot2 == 3) && entityInSlot1.Item.Type != ItemType.ARMOR_BOOTS)
                    return;
                if ((slot2 < STARTING_GENERAL_INVENTORY_SLOT && slot2 == 4) && entityInSlot1.Item.Type != ItemType.EQUIP_PRIMARY)
                    return;
                if ((slot2 < STARTING_GENERAL_INVENTORY_SLOT && slot2 == 5) && entityInSlot1.Item.Type != ItemType.EQUIP_SECONDARY)
                    return;
            }

            if (entityInSlot2.Item != null)
            {
                if (slot1 < STARTING_GENERAL_INVENTORY_SLOT && ((entityInSlot2.Item.Gender == PlayerGender.MALE && AttachedPlayer.Gender != PlayerGender.MALE) || (entityInSlot2.Item.Gender == PlayerGender.FEMALE && AttachedPlayer.Gender != PlayerGender.FEMALE)))
                    return;

                //if (slot1 < STARTING_GENERAL_INVENTORY_SLOT && entityInSlot2.Item.RequiredMinimumLevel > AttachedPlayer.CurrentLevel)
                //    return;

                if ((slot1 < STARTING_GENERAL_INVENTORY_SLOT && slot1 == 0) && entityInSlot2.Item.Type != ItemType.ARMOR_HEAD)
                    return;
                if ((slot1 < STARTING_GENERAL_INVENTORY_SLOT && slot1 == 1) && entityInSlot2.Item.Type != ItemType.ARMOR_CHEST)
                    return;
                if ((slot1 < STARTING_GENERAL_INVENTORY_SLOT && slot1 == 2) && entityInSlot2.Item.Type != ItemType.ARMOR_LEGGINGS)
                    return;
                if ((slot1 < STARTING_GENERAL_INVENTORY_SLOT && slot1 == 3) && entityInSlot2.Item.Type != ItemType.ARMOR_BOOTS)
                    return;
                if ((slot1 < STARTING_GENERAL_INVENTORY_SLOT && slot1 == 4) && entityInSlot2.Item.Type != ItemType.EQUIP_PRIMARY)
                    return;
                if ((slot1 < STARTING_GENERAL_INVENTORY_SLOT && slot1 == 5) && entityInSlot2.Item.Type != ItemType.EQUIP_SECONDARY)
                    return;
            }

            InventorySlots[slot1] = entityInSlot2;
            InventorySlots[slot2] = entityInSlot1;

            var networking = Program.GameCore.GetComponent<NetworkingComponent>();

            if ((slot1 < STARTING_GENERAL_INVENTORY_SLOT || slot2 < STARTING_GENERAL_INVENTORY_SLOT))
            {
                var inventoryUpdateMessage = networking.Server.CreateMessage(PacketId.PLAYER_INVENTORY_UPDATE);
                inventoryUpdateMessage.Write(this.AttachedPlayer.Id);
                this.SerializeToNetworkMessage(ref inventoryUpdateMessage, false);
                networking.Server.SendToAll(inventoryUpdateMessage, this.AttachedPlayer.NetworkConnection, NetDeliveryMethod.ReliableOrdered, 0);
            }

            var localInventoryUpdateMessage = networking.Server.CreateMessage(PacketId.PLAYER_INVENTORY_UPDATE);
            localInventoryUpdateMessage.Write(this.AttachedPlayer.Id);
            this.SerializeToNetworkMessage(ref localInventoryUpdateMessage, true);
            networking.Server.SendMessage(localInventoryUpdateMessage, this.AttachedPlayer.NetworkConnection, NetDeliveryMethod.ReliableOrdered);
        }

        public int GetCountOfItems(int itemId)
        {
            var countOfItems = 0;

            for (var slot = 0; slot < MAX_INVENTORY_SIZE; slot++)
            {
                if (InventorySlots[slot].Id == itemId)
                    countOfItems += InventorySlots[slot].Count;
            }

            return countOfItems;
        }

        public int GetFirstSlotContainingItem(int itemId)
        {
            for (var slot = 0; slot < MAX_INVENTORY_SIZE; slot++)
            {
                if (InventorySlots[slot].Id == itemId)
                    return slot;
            }

            return -1;
        }

        //public void DropItemInSlot(int slot)
        //{
        //    if (slot < 0 || slot >= MAX_INVENTORY_SIZE)
        //        return;

        //    var entityInSlot1 = InventorySlots[slot];

        //    if (entityInSlot1 != null)
        //    {
        //        // TODO: Spawn dropped item
        //        InventorySlots[slot] = null;
        //    }
        //}

        //public bool PickupDroppedItem(Item item)
        //{
        //    var availableSlot = GetAvailableGeneralInventorySlot();
        //    if (item == null || availableSlot == -1)
        //        return false;

        //    InventorySlots[availableSlot] = new InventoryItem { Item = item, Count = 1 };
        //    return true;
        //}

        public void SerializeToNetworkMessage(ref NetOutgoingMessage message, bool fullInventory = false)
        {
            if(fullInventory)
            {
                message.Write(MAX_INVENTORY_SIZE);

                for (var i = 0; i < MAX_INVENTORY_SIZE; i++)
                {
                    message.Write(InventorySlots[i].Id);
                    message.Write(InventorySlots[i].Count);
                }
            }
            else
            {
                message.Write(STARTING_GENERAL_INVENTORY_SLOT);
                for (var i = 0; i < STARTING_GENERAL_INVENTORY_SLOT; i++)
                {
                    message.Write(InventorySlots[i].Id);
                    message.Write(InventorySlots[i].Count);
                }
            }
        }
    }

    [BsonSerializer(typeof(InventoryItemBsonSerializer))]
    public struct InventoryItem
    {
        [BsonElement("itemId")]
        public int Id { get; set; }

        [BsonElement("itemCount")]
        public int Count { get; set; }

        [BsonIgnore]
        public Item Item
        {
            get
            {
                return Program.GameCore.GetService<ResourceManager>().GetItem(this.Id);
            }
        }
    }

    public enum PlayerGender : byte
    {
        ANY,
        MALE,
        FEMALE
    }

    public enum PlayerClass : byte
    {
        ANY,
        NOOB,
        ARCHER,
        MERCENARY,
        MAGICIAN,
        HEALER
    }
}