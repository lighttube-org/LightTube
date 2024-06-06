using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace LightTube.Database.Serialization;

public class BsonNullableStringSerializer : SerializerBase<string>
{
	public override string Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
	{
		BsonType type = context.Reader.GetCurrentBsonType();
		switch (type)
		{
			case BsonType.Null:
				context.Reader.ReadNull();
				return "";
			case BsonType.String:
				return context.Reader.ReadString();
			default:
				return "";
		}
	}

	public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, string value)
	{
		context.Writer.WriteString(value ?? "");
	}
}