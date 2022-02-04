using System;
using System.Collections.Generic;
using System.Text;

namespace NetBinSerializer {
	public partial class HighSerializeStream : SerializeStream {
		
		Serializer serializer;
		bool? cacheBuiltMethods;

		public HighSerializeStream(Serializer serializer, bool? cacheBuiltMethods = null) : base() {
			this.serializer = serializer;
			this.cacheBuiltMethods = cacheBuiltMethods;
		}

		public HighSerializeStream(Serializer serializer, byte[] bytes, bool? cacheBuiltMethods = null) : base(bytes) {
			this.serializer = serializer;
			this.cacheBuiltMethods = cacheBuiltMethods;
		}

		
		public void Serialize<T>(T obj, bool? cacheBuiltMethods = null, SerializationContext serializationContext = null) => Serialize(obj, typeof(T), cacheBuiltMethods, serializationContext);
		public void Serialize(object obj, Type type, bool? cacheBuiltMethods, SerializationContext serializationContext = null) => serializer.Serialize(this, obj, type ?? obj.GetType(), cacheBuiltMethods, serializationContext);

		public T Deserialize<T>(bool? cacheBuiltMethods = null, DeserializationContext deserializationContext = null) => (T)Deserialize(typeof(T), cacheBuiltMethods, deserializationContext);
		public object Deserialize(Type type, bool? cacheBuiltMethods, DeserializationContext deserializationContext = null) => serializer.Deserialize(this, type, cacheBuiltMethods, deserializationContext);

		//Special Serialization methods access interface methods

		public void Serialize(object obj, Type type = null) => Serialize(obj, type, this.cacheBuiltMethods);

		public object Deserialize(Type type) => Deserialize(type, this.cacheBuiltMethods);


		//Arrays
		public override Array ReadArray(Type arrayType) => (Array)Deserialize(arrayType);

		public override void WriteArray(Array arr) => Serialize(arr);

		//Collections
		public override TCollection ReadCollection<TCollection, TElement>() => (TCollection)Deserialize(typeof(TCollection));

		public override void WriteCollection<TCollection, TElement>(TCollection collection) => Serialize(typeof(ICollection<TElement>));

		
	}
}
