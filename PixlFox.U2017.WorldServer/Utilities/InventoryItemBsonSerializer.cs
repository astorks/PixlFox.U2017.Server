using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace PixlFox.U2017.WorldServer.Utilities
{
    public class InventoryItemBsonSerializer : SerializerBase<InventoryItem>
    {
        public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, InventoryItem value)
        {
            context.Writer.WriteStartArray();
            context.Writer.WriteInt32(value.Id);
            context.Writer.WriteInt32(value.Count);
            context.Writer.WriteEndArray();
        }

        public override InventoryItem Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            int itemId = 0, itemCount = 0;

            context.Reader.ReadStartArray();
            itemId = context.Reader.ReadInt32();
            itemCount = context.Reader.ReadInt32();
            context.Reader.ReadEndArray();

            return new InventoryItem { Id = itemId, Count = itemCount };
        }
    }
}
