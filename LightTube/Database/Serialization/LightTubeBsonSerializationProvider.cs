using MongoDB.Bson.Serialization;

namespace LightTube.Database.Serialization;

public class LightTubeBsonSerializationProvider : IBsonSerializationProvider
{
	public IBsonSerializer? GetSerializer(Type type)
	{
		return type == typeof(int) ? new BsonNullableIntSerializer() :
			type == typeof(string) ? new BsonNullableStringSerializer() : null;
	}
}