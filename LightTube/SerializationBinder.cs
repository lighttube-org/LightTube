using System;
using Newtonsoft.Json.Serialization;

namespace LightTube
{
	public class SerializationBinder : ISerializationBinder
	{
		private const string NAMESPACE_TO_REMOVE = "InnerTube.Models.";

		private readonly ISerializationBinder _binder;

		public SerializationBinder() : this(new DefaultSerializationBinder()) { }

		public SerializationBinder(ISerializationBinder binder)
		{
			_binder = binder ?? throw new ArgumentNullException();
		}

		#region ISerializationBinder Members

		public void BindToName(Type serializedType, out string assemblyName, out string typeName)
		{
			_binder.BindToName(serializedType, out assemblyName, out typeName);
			if (typeName != null && typeName.StartsWith(NAMESPACE_TO_REMOVE))
				typeName = typeName[NAMESPACE_TO_REMOVE.Length..];

			assemblyName = null;
		}

		public Type BindToType(string assemblyName, string typeName) => throw new NotImplementedException();

		#endregion
	}
}