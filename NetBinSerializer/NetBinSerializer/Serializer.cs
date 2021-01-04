using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace NetBinSerializer {

	public interface ISerializationMethods { 

		void serialize(SerializeStream stream, object obj);
		object deserialize(SerializeStream stream);

	}

	public class SerializationMethods : ISerializationMethods {
		public delegate void SerializeMethod(SerializeStream stream, object obj);
		public SerializeMethod serializeMethod { protected set; get; }
		public delegate object DeserializeMethod(SerializeStream stream);
		public DeserializeMethod deserializeMethod { protected set; get; }

		public SerializationMethods(SerializeMethod serializeMethod, DeserializeMethod deserializeMethod) {
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

		private static ConcurrentDictionary<Type, ISerializationMethods> serializationMethodsMap = new ConcurrentDictionary<Type, ISerializationMethods>();
		private static SerializationMethodsBuilder simpleSerializationMethodsBuilder = null;
		private static SerializationMethodsBuilder serializationMethodsBuilder = null; 
		public static bool CACHE_DEFAULT { get; set; } = true;
		//public static bool 

		static Serializer() { 
			simpleSerializationMethodsBuilder = new SimpleSerializationMethodsBuilder();
			StandartSerializationMethods.register();
		}

		//Cache managment interface

		public static bool isCached(Type type) { 
			return serializationMethodsMap.ContainsKey(type);
		}

		public static bool getCached(Type type, out ISerializationMethods result) { 
			return serializationMethodsMap.TryGetValue(type, out result);
		}

		public static bool cache(SerializationMethods.SerializeMethod serializeMethod, SerializationMethods.DeserializeMethod deserializeMethod, Type type) {
			return cache(new SerializationMethods(serializeMethod, deserializeMethod), type);
		}

		public static bool cache(this ISerializationMethods methods, Type type) { 
			return serializationMethodsMap.TryAdd(type, methods);
		}


		public static bool buildAndCacheIntegrated(Type type) { 
			ISerializationMethods serializationMethods = simpleSerializationMethodsBuilder.getSerializationMethods(type, true);
			return serializationMethods != null ? cache(serializationMethods, type) : false;
		}

		public static bool buildAndCache(Type type) {
			asserNoMethodsBuilder();
			ISerializationMethods serializationMethods = serializationMethodsBuilder.getSerializationMethods(type, true);
			return serializationMethods != null ? cache(serializationMethods, type) : false;
		}


		private static void asserNoMethodsBuilder() { 
			if(serializationMethodsBuilder == null)
				throw new SerializationException("No serialization methods builder");
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
				throw new SerializationException($"Can't serialize type {type}");
			}
		} 

		//1.1 Serialize object safe
		public static bool serializeSafe(this SerializeStream stream, object obj, Type type, bool? cacheBuiltMethods = null) {
			if(getSerializationMethods(type, out ISerializationMethods serializationMethods, cacheBuiltMethods)) {
				serializationMethods.serialize(stream, obj);
				return true;
			}
			return false;
		} 
		

		//2.4 Deserialize TYPE unsafe
		public static TYPE deserialize<TYPE>(this SerializeStream stream, bool? cacheBuiltMethods = null) { 
			if(stream.deserializeSafe<TYPE>(out TYPE result, cacheBuiltMethods)) {
				return result;
			}

			throw new SerializationException($"Can't deserialize type {typeof(TYPE)}");
		}

		//2.3 Deserialize TYPE safe 
		public static bool deserializeSafe<TYPE>(this SerializeStream stream, out TYPE obj, bool? cacheBuiltMethods = null) { 
			bool boolResult = deserializeSafe(stream, out object result, typeof(TYPE), cacheBuiltMethods);
			obj = (TYPE)result;
			return boolResult;
		}

		//2.2 Deserialize object unsafe
		public static object deserialize(this SerializeStream stream, Type type, bool? cacheBuiltMethods = null) { 
			if(deserializeSafe(stream, out object result, type, cacheBuiltMethods)) {
				return result;
			}

			throw new SerializationException($"Can't deserialize type {type}");
		}

		//2.1 Deserialize object safe with return
		public static bool deserializeSafe(this SerializeStream stream, out object result, Type type, bool? cacheBuiltMethods = null) {
			if(getSerializationMethods(type, out ISerializationMethods serializationMethods, cacheBuiltMethods)) {
				result = serializationMethods.deserialize(stream);
				return true;
			}
			result = default;
			return false;
		} 

		public static bool getSerializationMethods(Type type, out ISerializationMethods methods, bool? cacheBuiltMethods = null) {
			if(getCached(type, out methods)) { 
				return true;
			//For unknown types
			} else if(typeof(ISerializable).IsAssignableFrom(type)) {
				methods = new SerializationMethods(serializeSerializable, (SerializeStream stream) => { return stream.readSerializable(type); });
				if(cacheBuiltMethods.HasValue ? cacheBuiltMethods.Value : CACHE_DEFAULT)
					serializationMethodsMap[type] = methods;
				return true;
			} else {
				if(serializationMethodsBuilder == null) { 
					if((methods = simpleSerializationMethodsBuilder.getSerializationMethods(type, cacheBuiltMethods.HasValue ? cacheBuiltMethods.Value : CACHE_DEFAULT)) != null) {
						return true;
					}
				} else {
					if((methods = serializationMethodsBuilder.getSerializationMethods(type, cacheBuiltMethods.HasValue ? cacheBuiltMethods.Value : CACHE_DEFAULT)) != null) { 
						if(cacheBuiltMethods.HasValue ? cacheBuiltMethods.Value : CACHE_DEFAULT)
							serializationMethodsMap[type] = methods;
						return true;
					} 
				}

				return false;
				
			}
		}

		public static ISerializationMethods getSerializationMethods(Type type, bool? cacheBuiltMethods = null) { 
			getSerializationMethods(type, out ISerializationMethods result, cacheBuiltMethods);
			return result;
		}

		public static void serializeSerializable(SerializeStream stream, object serializable) { 
			((ISerializable)serializable).writeToStream(stream);
		}

		public static void useMethodsBuilder(SerializationMethodsBuilder methodsBuilder) {
			if(serializationMethodsBuilder != null)
				throw new Exception($"SerializationMethodsBuilder is already in use ({serializationMethodsBuilder.GetType()})");
			serializationMethodsBuilder = methodsBuilder;
		}

	}

	public class SimpleSerializationMethodsBuilder : SerializationMethodsBuilder {
		public ISerializationMethods getSerializationMethods(Type type, bool withCache) {
			if(type.IsArray) { 
				return new ArraySerializationMethodsChain(type, withCache);
			} else if(type.GetInterfaces().Any((Type t) => SerializeStream.isCollectionType(t))) { 
				Type elementType = type.GetInterfaces().First((Type interfaceType) => interfaceType.IsGenericType && interfaceType.GetGenericTypeDefinition() == typeof(ICollection<>)).GetGenericArguments()[0];
				return (ISerializationMethods)typeof(CollectionSerializationMethodsChain<,>).MakeGenericType(type, elementType).GetConstructor(new Type[] { typeof(bool) }).Invoke(new object[] { withCache });
			} else if(type.IsGenericType && type.GetGenericTypeDefinition() == typeof(KeyValuePair<,>)) { 
				Type[] genericArgs = type.GetGenericArguments();
				return (ISerializationMethods)typeof(KeyValuePairSerializationMethodsChain<,,>).MakeGenericType(type, genericArgs[0], genericArgs[1]).GetConstructor(new Type[] { typeof(bool) }).Invoke(new object[] { withCache });
			} else {
				return null;
			}
		}
	}

	public abstract class SerializationMethodsChain : ISerializationMethods { 

		public Type thisType { get; protected set; }
		public ISerializationMethods containTypeSerializationMethods { get; protected set; }

		public SerializationMethodsChain(Type thisType, ISerializationMethods containTypeSerializationMethods) {

			this.thisType = thisType;
			this.containTypeSerializationMethods = containTypeSerializationMethods;
		}

		public abstract void serialize(SerializeStream stream, object obj);
		public abstract object deserialize(SerializeStream stream);

	}

	public class ArraySerializationMethodsChain : SerializationMethodsChain {

		public ArraySerializationMethodsChain(Type arrayType,  bool? cacheBuiltMethods = null) : this(arrayType, Serializer.getSerializationMethods(arrayType.GetElementType(), cacheBuiltMethods), cacheBuiltMethods) { 
		}

		public ArraySerializationMethodsChain(Type arrayType, ISerializationMethods serializationMethods, bool? cacheBuiltMethods = null) : base(arrayType, serializationMethods) { 
		}

		public override object deserialize(SerializeStream stream) {
			int rank = stream.readInt32();
			if(rank == 1) { 
				int length = stream.readInt32();
				Array result = Array.CreateInstance(thisType.GetElementType(), length);
			
				for(int index = 0; index < length; index++)
					result.SetValue(containTypeSerializationMethods.deserialize(stream), index);
				
				return result;
			} else { 
				int[] dimensions = new int[rank];
				for(int dimensionIndex = 0; dimensionIndex < rank; dimensionIndex++) { 
					dimensions[dimensionIndex] = stream.readInt32();
				}

				Array result = Array.CreateInstance(thisType.GetElementType(), dimensions);
				foreach(int[] currentPos in SerializeStream.ndArrayWalker(dimensions))
					result.SetValue(containTypeSerializationMethods.deserialize(stream), currentPos);

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
					containTypeSerializationMethods.serialize(stream, arr.GetValue(index));
			} else { 
				int[] dimensions = new int[rank];
				
				for(int dimensionIndex = 0; dimensionIndex < rank; dimensionIndex++) {
					stream.write(dimensions[dimensionIndex] = arr.GetLength(dimensionIndex));
				}

				foreach(int[] currentPos in SerializeStream.ndArrayWalker(dimensions))
					containTypeSerializationMethods.serialize(stream, arr.GetValue(currentPos));
			}
		}
	}

	public class CollectionSerializationMethodsChain<COLLECTION_TYPE, ELEMENT_TYPE> : SerializationMethodsChain where COLLECTION_TYPE : ICollection<ELEMENT_TYPE>, new() {

		public CollectionSerializationMethodsChain(bool? cacheBuiltMethods = null) : this(Serializer.getSerializationMethods(typeof(ELEMENT_TYPE), cacheBuiltMethods), cacheBuiltMethods) { }

		public CollectionSerializationMethodsChain(ISerializationMethods serializationMethods, bool? cacheBuiltMethods = null) : base(typeof(COLLECTION_TYPE), serializationMethods) { }

		public override object deserialize(SerializeStream stream) {
			COLLECTION_TYPE result = new COLLECTION_TYPE();
			int length = stream.readInt32();
			for(int index = 0; index < length; index++)
				result.Add((ELEMENT_TYPE)containTypeSerializationMethods.deserialize(stream));
			return result;
		}

		public override void serialize(SerializeStream stream, object obj) {
			COLLECTION_TYPE collectionObj = (COLLECTION_TYPE)obj;
			stream.write(collectionObj.Count);
			foreach(ELEMENT_TYPE element in collectionObj)
				containTypeSerializationMethods.serialize(stream, element);
		}
	}

	public class KeyValuePairSerializationMethodsChain<KEY_VALUE_PAIR_TYPE, KEY_TYPE, VALUE_TYPE> : SerializationMethodsChain {

		ISerializationMethods keySerializationMethods;
		ISerializationMethods valueSerializationMethods;

		public KeyValuePairSerializationMethodsChain(bool? cacheBuiltMethods = null) : this(Serializer.getSerializationMethods(typeof(KEY_TYPE)), Serializer.getSerializationMethods(typeof(VALUE_TYPE)),  cacheBuiltMethods) { }

		public KeyValuePairSerializationMethodsChain(ISerializationMethods keySerializationMethods, ISerializationMethods valueSerializationMethods, bool? cacheBuiltMethods = null) : base(typeof(KEY_VALUE_PAIR_TYPE), null) { 
			this.keySerializationMethods = keySerializationMethods;
			this.valueSerializationMethods = valueSerializationMethods;
		}

		public override object deserialize(SerializeStream stream) {
			KEY_TYPE key = (KEY_TYPE)keySerializationMethods.deserialize(stream);
			VALUE_TYPE value = (VALUE_TYPE)valueSerializationMethods.deserialize(stream);
			return new KeyValuePair<KEY_TYPE, VALUE_TYPE>(key, value);
		}

		public override void serialize(SerializeStream stream, object obj) {
			KeyValuePair<KEY_TYPE, VALUE_TYPE> keyValuePair = (KeyValuePair<KEY_TYPE, VALUE_TYPE>)obj;
			keySerializationMethods.serialize(stream, keyValuePair.Key);
			valueSerializationMethods.serialize(stream, keyValuePair.Value);
		}
	}

	public static class StandartSerializationMethods {

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

	public interface SerializationMethodsBuilder { 

		ISerializationMethods getSerializationMethods(Type type, bool withCache);

	}

	public class SerializationException : Exception { 

		public SerializationException(string message) : base(message) {}

	}

	public class SerializationRule : Attribute { 

		public bool isSerializable { get; private set; }

		public SerializationRule(bool isSerializable) { 
			this.isSerializable = isSerializable;
		}

	}
	

}
