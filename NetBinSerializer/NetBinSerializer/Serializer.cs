using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace NetBinSerializer {

	public class SerializeMethods {
		public delegate void SerializeMethod(object obj, SerializeStream stream);
		public SerializeMethod serializeMethod;
		public delegate object DeserializeMethod(SerializeStream stream);
		public DeserializeMethod deserializeMethod;

		public SerializeMethods(SerializeMethod serializeMethod, DeserializeMethod deserializeMethod) {
			this.serializeMethod = serializeMethod;
			this.deserializeMethod = deserializeMethod;
		}
	}

	public static class Serializer {

		private static ConcurrentDictionary<Type, SerializeMethods> serializeMethodsMap = new ConcurrentDictionary<Type, SerializeMethods>();

		static Serializer() { 
			StandartSerializeMethods.register();
		}

		public static bool addSerializeMethods(Type type, SerializeMethods.SerializeMethod serializeMethod, SerializeMethods.DeserializeMethod deserializeMethod) {
			return serializeMethodsMap.TryAdd(type, new SerializeMethods(serializeMethod, deserializeMethod));
		}

		public static bool serialize(object obj, SerializeStream stream) {
			SerializeMethods serializeMethods;
			if(serializeMethodsMap.TryGetValue(obj.GetType(), out serializeMethods)) {
				serializeMethods.serializeMethod(obj, stream);
				return true;
			}
			return false;
		} 

		public static bool deserialize(SerializeStream stream, Type type, out object result) {
			SerializeMethods serializeMethods;
			if(serializeMethodsMap.TryGetValue(type, out serializeMethods)) {
				result = ((SerializeMethods)serializeMethods).deserializeMethod(stream);
				return true;
			}
			result = default;
			return false;
		} 

		public static T deserialize<T>(SerializeStream stream) {
			return (T)((SerializeMethods)serializeMethodsMap[typeof(T)]).deserializeMethod(stream);
		}

		public static bool getSerializeMethods(Type type, out SerializeMethods methods) {
			if(serializeMethodsMap.TryGetValue(type, out methods)) { 
				return true;
			//For unknown types
			} else if(typeof(Serializable).IsAssignableFrom(type)) {
				methods = new SerializeMethods(serializeSerializable, (SerializeStream stream) => { return stream.readSerializable(type); });
				serializeMethodsMap[type] = methods;
				return true;
			} else { 
				return false;
			}
		}

		public static SerializeMethods getSerializeMethods(Type type) { 
			SerializeMethods result;
			if(serializeMethodsMap.TryGetValue(type, out result)) { 
				return result;
			} else { 
				return null;
			}
		}

		public static void serializeSerializable(object serializable, SerializeStream stream) { 
			((Serializable)serializable).writeToStream(stream);
		}

		/*
		public void serialize<T>(T obj, SerializeStream stream) {
			((SerializeMethods<T>)serializeMethodsMap[typeof(T)]).serializeMethod(obj, stream);
		} */
	}

	public static class StandartSerializeMethods {

		/*				Numeric types				*/

		public static void serializeInt64(object value, SerializeStream stream) { stream.write((Int64)value); }
		public static void serializeInt32(object value, SerializeStream stream) { stream.write((Int32)value); }
		public static void serializeInt16(object value, SerializeStream stream) { stream.write((Int16)value); }
		public static void serializeInt8(object value, SerializeStream stream) { stream.write((sbyte)value); }


		public static void serializeUInt64(object value, SerializeStream stream) { stream.write((UInt64)value); }
		public static void serializeUInt32(object value, SerializeStream stream) { stream.write((UInt32)value); }
		public static void serializeUInt16(object value, SerializeStream stream) { stream.write((UInt16)value); }
		public static void serializeUInt8(object value, SerializeStream stream) { stream.write((byte)value); }



		public static void serializeFloat(object value, SerializeStream stream) { stream.write((Single)value); }
		public static void serializeDouble(object value, SerializeStream stream) { stream.write((Double)value); }



		public static object deserializeInt64(SerializeStream stream) { return stream.readInt64(); }
		public static object deserializeInt32(SerializeStream stream) { return stream.readInt32(); }
		public static object deserializeInt16(SerializeStream stream) { return stream.readInt16(); }
		public static object deserializeInt8(SerializeStream stream) { return stream.readSByte(); }


		public static object deserializeUInt64(SerializeStream stream) { return stream.readUInt64(); }
		public static object deserializeUInt32(SerializeStream stream) { return stream.readUInt32(); }
		public static object deserializeUInt16(SerializeStream stream) { return stream.readUInt16(); }
		public static object deserializeUInt8(SerializeStream stream) { return stream.readByte(); }

		


		public static object deserializeFloat(SerializeStream stream) { return stream.readFloat(); }
		public static object deserializeDouble(SerializeStream stream) { return stream.readDouble(); }

		/*				Extended types				*/

		public static void serializeByteArray(object value, SerializeStream stream) { stream.write((byte[])value); }
		public static object deserializeByteArray(SerializeStream stream) { return stream.readBytes(); }


		public static void serializeString(object value, SerializeStream stream) { stream.write((string)value); }
		public static object deserializeString(SerializeStream stream) { return stream.readString(); }



		internal static void register() { 
			Serializer.addSerializeMethods(typeof(Int64), serializeInt64, deserializeInt64);
			Serializer.addSerializeMethods(typeof(Int32), serializeInt32, deserializeInt32);
			Serializer.addSerializeMethods(typeof(Int16), serializeInt16, deserializeInt16);
			Serializer.addSerializeMethods(typeof(sbyte), serializeInt8,  deserializeInt8);

			Serializer.addSerializeMethods(typeof(UInt64), serializeUInt64, deserializeUInt64);
			Serializer.addSerializeMethods(typeof(UInt32), serializeUInt32, deserializeUInt32);
			Serializer.addSerializeMethods(typeof(UInt16), serializeUInt16, deserializeUInt16);
			Serializer.addSerializeMethods(typeof(byte), serializeUInt8,  deserializeUInt8);

			Serializer.addSerializeMethods(typeof(Single), serializeFloat, deserializeFloat);
			Serializer.addSerializeMethods(typeof(Double), serializeDouble, deserializeDouble);

			Serializer.addSerializeMethods(typeof(byte[]), serializeByteArray, deserializeByteArray);
			Serializer.addSerializeMethods(typeof(string), serializeString, deserializeString);
		}
	}


	

}
