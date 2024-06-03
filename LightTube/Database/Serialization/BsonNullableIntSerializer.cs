using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace LightTube.Database.Serialization;

public class BsonNullableIntSerializer : SerializerBase<int>
{
	public override int Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
	{
		BsonType type = context.Reader.GetCurrentBsonType();
		switch (type)
		{
			case BsonType.Null:
				context.Reader.ReadNull();
				return 0;
			case BsonType.Int32:
				return context.Reader.ReadInt32();
			default:
				return 0;
		}
	}
}