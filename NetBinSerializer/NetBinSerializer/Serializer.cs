using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace NetBinSerializer {

	public interface ISerializeMethods { 

		void serialize(SerializeStream stream, object obj);
		object deserialize(SerializeStream stream);

	}

	public class SerializeMethods : ISerializeMethods {
		public delegate void SerializeMethod(SerializeStream stream, object obj);
		public SerializeMethod serializeMethod { protected set; get; }
		public delegate object DeserializeMethod(SerializeStream stream);
		public DeserializeMethod deserializeMethod { protected set; get; }

		public SerializeMethods(SerializeMethod serializeMethod, DeserializeMethod deserializeMethod) {
			this.serializeMethod = serializeMethod;
			this.deserializeMethod = deserializeMethod;
		}

		public void serialize(SerializeStream stream, object obj) {
			serializeMethod(stream, obj);
		}

		public object deserialize(SerializeStream stream) {
			return deserializeMethod(stream);
		}
	}

	public static class Serializer {

		private static ConcurrentDictionary<Type, ISerializeMethods> serializeMethodsMap = new ConcurrentDictionary<Type, ISerializeMethods>();
		private static SerializeMethodsBuilder simpleSerializeMethodsBuilder = null;
		private static SerializeMethodsBuilder serializeMethodsBuilder = null; 
		public static bool CACHE_DEFAULT { get; set; } = true;
		//public static bool 

		static Serializer() { 
			simpleSerializeMethodsBuilder = new SimpleSerializeMethodsBuilder();
			StandartSerializeMethods.register();
		}

		//Cache managment interface

		public static bool cache(SerializeMethods.SerializeMethod serializeMethod, SerializeMethods.DeserializeMethod deserializeMethod, Type type) {
			return cache(new SerializeMethods(serializeMethod, deserializeMethod), type);
		}

		public static bool cache(this ISerializeMethods methods, Type type) { 
			return serializeMethodsMap.TryAdd(type, methods);
		}


		public static bool buildAndCacheIntegrated(Type type) { 
			ISerializeMethods serializeMethods = simpleSerializeMethodsBuilder.getSerializeMethods(type, true);
			return serializeMethods != null ? cache(serializeMethods, type) : false;
		}

		public static bool buildAndCache(Type type) {
			asserNoMethodsBuilder();
			ISerializeMethods serializeMethods = serializeMethodsBuilder.getSerializeMethods(type, true);
			return serializeMethods != null ? cache(serializeMethods, type) : false;
		}


		private static void asserNoMethodsBuilder() { 
			if(serializeMethodsBuilder == null)
				throw new SerializeException("No serialize methods builder");
		}

		//1.4 Serialize TYPE safe
		public static bool serializeSafe<TYPE>(this SerializeStream stream, TYPE obj, bool? cacheBuiltMethods = null) { 
			 return serializeSafe(stream, obj, typeof(TYPE), cacheBuiltMethods);
		}

		//1.3 Serialize TYPE unsafe
		public static void serialize<TYPE>(this SerializeStream stream, TYPE obj, bool? cacheBuiltMethods = null) { 
			serialize(stream, obj, typeof(TYPE), cacheBuiltMethods);
		}

		//1.2 Serialize object unsafe
		public static void serialize(this SerializeStream stream, object obj, Type type, bool? cacheBuiltMethods = null) {
			if(!serializeSafe(stream, obj, type, cacheBuiltMethods)) {
				throw new SerializeException($"Can't serialize type {type}");
			}
		} 

		//1.1 Serialize object safe
		public static bool serializeSafe(this SerializeStream stream, object obj, Type type, bool? cacheBuiltMethods = null) {
			if(getSerializeMethods(type, out ISerializeMethods serializeMethods, cacheBuiltMethods)) {
				serializeMethods.serialize(stream, obj);
				return true;
			}
			return false;
		} 
		

		//2.4 Deserialize TYPE unsafe
		public static TYPE deserialize<TYPE>(this SerializeStream stream, bool? cacheBuiltMethods = false) { 
			if(stream.deserializeSafe<TYPE>(out TYPE result, cacheBuiltMethods)) {
				return result;
			}

			throw new SerializeException($"Can't deserialize type {typeof(TYPE)}");
		}

		//2.3 Deserialize TYPE safe 
		public static bool deserializeSafe<TYPE>(this SerializeStream stream, out TYPE obj, bool? cacheBuiltMethods = false) { 
			bool boolResult = deserializeSafe(stream, out object result, typeof(TYPE), cacheBuiltMethods);
			obj = (TYPE)result;
			return boolResult;
		}

		//2.2 Deserialize object unsafe
		public static object deserialize(this SerializeStream stream, Type type, bool? cacheBuiltMethods = false) { 
			if(deserializeSafe(stream, out object result, type, cacheBuiltMethods)) {
				return result;
			}

			throw new SerializeException($"Can't deserialize type {type}");
		}

		//2.1 Deserialize object safe with return
		public static bool deserializeSafe(this SerializeStream stream, out object result, Type type, bool? cacheBuiltMethods = false) {
			if(getSerializeMethods(type, out ISerializeMethods serializeMethods, cacheBuiltMethods)) {
				result = serializeMethods.deserialize(stream);
				return true;
			}
			result = default;
			return false;
		} 

		public static bool getSerializeMethods(Type type, out ISerializeMethods methods, bool? cacheBuiltMethods = false) {
			if(serializeMethodsMap.TryGetValue(type, out methods)) { 
				return true;
			//For unknown types
			} else if(typeof(Serializable).IsAssignableFrom(type)) {
				methods = new SerializeMethods(serializeSerializable, (SerializeStream stream) => { return stream.readSerializable(type); });
				if(cacheBuiltMethods.HasValue ? cacheBuiltMethods.Value : CACHE_DEFAULT)
					serializeMethodsMap[type] = methods;
				return true;
			} else {
				if(serializeMethodsBuilder == null) { 
					if((methods = simpleSerializeMethodsBuilder.getSerializeMethods(type, cacheBuiltMethods.HasValue ? cacheBuiltMethods.Value : CACHE_DEFAULT)) != null) {
						return true;
					}
				} else {
					if((methods = serializeMethodsBuilder.getSerializeMethods(type, cacheBuiltMethods.HasValue ? cacheBuiltMethods.Value : CACHE_DEFAULT)) != null) { 
						if(cacheBuiltMethods.HasValue ? cacheBuiltMethods.Value : CACHE_DEFAULT)
							serializeMethodsMap[type] = methods;
						return true;
					} 
				}

				return false;
				
			}
		}

		public static ISerializeMethods getSerializeMethods(Type type, bool cacheBuiltMethods = false) { 
			getSerializeMethods(type, out ISerializeMethods result, cacheBuiltMethods);
			return result;
		}

		public static void serializeSerializable(SerializeStream stream, object serializable) { 
			((Serializable)serializable).writeToStream(stream);
		}

		public static void useMethodsBuilder(SerializeMethodsBuilder methodsBuilder) {
			serializeMethodsBuilder = methodsBuilder;
		}

	}

	public class SimpleSerializeMethodsBuilder : SerializeMethodsBuilder {
		public ISerializeMethods getSerializeMethods(Type type, bool withCache) {
			if(type.IsArray) { 
				return new ArraySerializeMethodsChain(type, withCache);
			} else if(type.GetInterfaces().Any((Type t) => SerializeStream.isCollectionType(t))) { 
				Type elementType = type.GetInterfaces().First((Type interfaceType) => interfaceType.IsGenericType && interfaceType.GetGenericTypeDefinition() == typeof(ICollection<>)).GetGenericArguments()[0];
				return (ISerializeMethods)typeof(CollectionSerializeMethodsChain<,>).MakeGenericType(type, elementType).GetConstructor(new Type[] { typeof(bool) }).Invoke(new object[] { withCache });
			} else if(type.IsGenericType && type.GetGenericTypeDefinition() == typeof(KeyValuePair<,>)) { 
				Type[] genericArgs = type.GetGenericArguments();
				return (ISerializeMethods)typeof(KeyValuePairSerializationMethodsChain<,,>).MakeGenericType(type, genericArgs[0], genericArgs[1]).GetConstructor(new Type[] { typeof(bool) }).Invoke(new object[] { withCache });
			} else {
				return null;
			}
		}
	}

	public abstract class SerializeMethodsChain : ISerializeMethods { 

		public Type thisType { get; protected set; }
		public ISerializeMethods containTypeSerializeMethods { get; protected set; }

		public SerializeMethodsChain(Type thisType, ISerializeMethods containTypeSerializeMethods) {

			this.thisType = thisType;
			this.containTypeSerializeMethods = containTypeSerializeMethods;
		}

		public abstract void serialize(SerializeStream stream, object obj);
		public abstract object deserialize(SerializeStream stream);

	}

	public class ArraySerializeMethodsChain : SerializeMethodsChain {

		public ArraySerializeMethodsChain(Type arrayType,  bool cacheBuiltMethods = false) : base(arrayType, Serializer.getSerializeMethods(arrayType.GetElementType(), cacheBuiltMethods)) { 
		}

		public override object deserialize(SerializeStream stream) {
			int rank = stream.readInt32();
			if(rank == 1) { 
				int length = stream.readInt32();
				Array result = Array.CreateInstance(thisType.GetElementType(), length);
			
				for(int index = 0; index < length; index++)
					result.SetValue(containTypeSerializeMethods.deserialize(stream), index);
				
				return result;
			} else { 
				int[] dimensions = new int[rank];
				for(int dimensionIndex = 0; dimensionIndex < rank; dimensionIndex++) { 
					dimensions[dimensionIndex] = stream.readInt32();
				}

				Array result = Array.CreateInstance(thisType.GetElementType(), dimensions);
				foreach(int[] currentPos in SerializeStream.ndArrayWalker(dimensions))
					result.SetValue(containTypeSerializeMethods.deserialize(stream), currentPos);

				return result;
			}
		}

		public override void serialize(SerializeStream stream, object obj) {
			Array arr = (Array)obj;
			int rank = arr.Rank;
			stream.write(rank);
			if(rank == 1) {
				stream.write(arr.Length);
				for(int index = 0; index < arr.Length; index++)
					containTypeSerializeMethods.serialize(stream, arr.GetValue(index));
			} else { 
				int[] dimensions = new int[rank];
				
				for(int dimensionIndex = 0; dimensionIndex < rank; dimensionIndex++) {
					stream.write(dimensions[dimensionIndex] = arr.GetLength(dimensionIndex));
				}

				foreach(int[] currentPos in SerializeStream.ndArrayWalker(dimensions))
					containTypeSerializeMethods.serialize(stream, arr.GetValue(currentPos));
			}
		}
	}

	public class CollectionSerializeMethodsChain<COLLECTION_TYPE, ELEMENT_TYPE> : SerializeMethodsChain where COLLECTION_TYPE : ICollection<ELEMENT_TYPE>, new() {

		public CollectionSerializeMethodsChain(bool cacheBuiltMethods = false)
			: base(
				  typeof(COLLECTION_TYPE), 
				  Serializer.getSerializeMethods(typeof(ELEMENT_TYPE), cacheBuiltMethods)
			) 
		{ }

		public override object deserialize(SerializeStream stream) {
			COLLECTION_TYPE result = new COLLECTION_TYPE();
			int length = stream.readInt32();
			for(int index = 0; index < length; index++)
				result.Add((ELEMENT_TYPE)containTypeSerializeMethods.deserialize(stream));
			return result;
		}

		public override void serialize(SerializeStream stream, object obj) {
			COLLECTION_TYPE collectionObj = (COLLECTION_TYPE)obj;
			stream.write(collectionObj.Count);
			foreach(ELEMENT_TYPE element in collectionObj)
				containTypeSerializeMethods.serialize(stream, element);
		}
	}

	public class KeyValuePairSerializationMethodsChain<KEY_VALUE_PAIR_TYPE, KEY_TYPE, VALUE_TYPE> : SerializeMethodsChain {

		ISerializeMethods keySerializeMethods;
		ISerializeMethods valueSerializeMethods;

		public KeyValuePairSerializationMethodsChain(bool cacheBuiltMethods = false) : base(typeof(KEY_VALUE_PAIR_TYPE), null) { 
			keySerializeMethods = Serializer.getSerializeMethods(typeof(KEY_TYPE));
			valueSerializeMethods = Serializer.getSerializeMethods(typeof(VALUE_TYPE));
		}

		public override object deserialize(SerializeStream stream) {
			KEY_TYPE key = (KEY_TYPE)keySerializeMethods.deserialize(stream);
			VALUE_TYPE value = (VALUE_TYPE)valueSerializeMethods.deserialize(stream);
			return new KeyValuePair<KEY_TYPE, VALUE_TYPE>(key, value);
		}

		public override void serialize(SerializeStream stream, object obj) {
			KeyValuePair<KEY_TYPE, VALUE_TYPE> keyValuePair = (KeyValuePair<KEY_TYPE, VALUE_TYPE>)obj;
			keySerializeMethods.serialize(stream, keyValuePair.Key);
			valueSerializeMethods.serialize(stream, keyValuePair.Value);
		}
	}

	public static class StandartSerializeMethods {

		/*				Numeric types				*/

		public static void serializeInt64(SerializeStream stream, object value) { stream.write((Int64)value); }
		public static void serializeInt32(SerializeStream stream, object value) { stream.write((Int32)value); }
		public static void serializeInt16(SerializeStream stream, object value) { stream.write((Int16)value); }
		public static void serializeInt8(SerializeStream stream, object value) { stream.write((sbyte)value); }


		public static void serializeUInt64(SerializeStream stream, object value) { stream.write((UInt64)value); }
		public static void serializeUInt32(SerializeStream stream, object value) { stream.write((UInt32)value); }
		public static void serializeUInt16(SerializeStream stream, object value) { stream.write((UInt16)value); }
		public static void serializeUInt8(SerializeStream stream, object value) { stream.write((byte)value); }



		public static void serializeFloat(SerializeStream stream, object value) { stream.write((Single)value); }
		public static void serializeDouble(SerializeStream stream, object value) { stream.write((Double)value); }



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

		public static void serializeByteArray(SerializeStream stream, object value) { stream.write((byte[])value); }
		public static object deserializeByteArray(SerializeStream stream) { return stream.readBytes(); }


		public static void serializeString(SerializeStream stream, object value) { stream.write((string)value); }
		public static object deserializeString(SerializeStream stream) { return stream.readString(); }



		internal static void register() { 
			Serializer.cache(serializeInt64, deserializeInt64, typeof(Int64));
			Serializer.cache(serializeInt32, deserializeInt32, typeof(Int32));
			Serializer.cache(serializeInt16, deserializeInt16, typeof(Int16));
			Serializer.cache(serializeInt8,  deserializeInt8, typeof(sbyte));

			Serializer.cache(serializeUInt64, deserializeUInt64, typeof(UInt64));
			Serializer.cache(serializeUInt32, deserializeUInt32, typeof(UInt32));
			Serializer.cache(serializeUInt16, deserializeUInt16, typeof(UInt16));
			Serializer.cache(serializeUInt8,  deserializeUInt8, typeof(byte));

			Serializer.cache(serializeFloat, deserializeFloat, typeof(Single));
			Serializer.cache(serializeDouble, deserializeDouble, typeof(Double));

			Serializer.cache(serializeByteArray, deserializeByteArray, typeof(byte[]));
			Serializer.cache(serializeString, deserializeString, typeof(string));
		}
	}

	public interface SerializeMethodsBuilder { 

		ISerializeMethods getSerializeMethods(Type type, bool withCache);

	}

	public class SerializeException : Exception { 

		public SerializeException(string message) : base(message) {}

	}
	

}
