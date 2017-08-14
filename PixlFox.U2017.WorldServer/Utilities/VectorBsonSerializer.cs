using MongoDB.Bson.IO;
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
    public class Vector3BsonSerializer : SerializerBase<Vector3>
    {
        public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, Vector3 value)
        {
            context.Writer.WriteStartArray();
            context.Writer.WriteDouble(value.x);
            context.Writer.WriteDouble(value.y);
            context.Writer.WriteDouble(value.z);
            context.Writer.WriteEndArray();
        }

        public override Vector3 Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            float x = 0, y = 0, z = 0;

            context.Reader.ReadStartArray();
            x = (float)context.Reader.ReadDouble();
            y = (float)context.Reader.ReadDouble();
            z = (float)context.Reader.ReadDouble();
            context.Reader.ReadEndArray();

            return new Vector3(x, y, z);
        }
    }

    public class Vector2BsonSerializer : SerializerBase<Vector2>
    {
        public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, Vector2 value)
        {
            context.Writer.WriteStartArray();
            context.Writer.WriteDouble(value.x);
            context.Writer.WriteDouble(value.y);
            context.Writer.WriteEndArray();
        }

        public override Vector2 Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            float x = 0, y = 0;

            context.Reader.ReadStartArray();
            x = (float)context.Reader.ReadDouble();
            y = (float)context.Reader.ReadDouble();
            context.Reader.ReadEndArray();

            return new Vector2(x, y);
        }
    }

    
}
