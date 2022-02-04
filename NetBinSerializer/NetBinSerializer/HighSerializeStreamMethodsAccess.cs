using System;
using System.Collections.Generic;
using System.Text;

namespace NetBinSerializer {

	public struct SerializableRWMethods : IRWMethods { 

		public ReadMethod ReadMethod => ReadObject;
		public WriteMethod WriteMethod => WriteObject;

		private SerializeStream stream;

		private SerializationMethodsBase serializationMethods;

		public SerializableRWMethods(SerializeStream stream, SerializationMethodsBase serializationMethods) { 
			this.stream = stream;
			this.serializationMethods = serializationMethods;
		}

		private object ReadObject() => serializationMethods.Deserialize(stream);

		private void WriteObject(object obj) => serializationMethods.Serialize(stream, obj);

	}

	public partial class HighSerializeStream {

		public override IRWMethods GetRWMethods(Type type) {
			IRWMethods rwMethods = base.GetRWMethods(type);
			if(rwMethods != null) {
				return rwMethods;
			} else if(serializer.GetSerializationMethods(type, out SerializationMethodsBase serializationMethods)) {
				return new SerializableRWMethods(this, serializationMethods);
			} else {
				return null;
			}
		}

	}
}
