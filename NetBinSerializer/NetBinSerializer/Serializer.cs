using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace NetBinSerializer {

	public class SerializationContext { 
		
		Dictionary<object, int> serializedObjects = new Dictionary<object, int>();
		int currentObjectIndex = 0;

		//Returns -1 if object isn't found
		public int tryToWriteObject(object obj) {
			if(serializedObjects.TryGetValue(obj, out int result)) { 
				return result;
			} else { 
				serializedObjects.Add(obj, currentObjectIndex++);
				return -1;
			}
		}
	}

	public class DeserializationContext { 
		
		List<object> deserializedObjects = new List<object>();

		public void writeObject(object obj) {
			deserializedObjects.Add(obj);
		}

		//Returns object or null if object isn't found
		//Negative values arn't allowed. If negative value was passed, it's program error
		public object getObject(int index) {
			if(index >= deserializedObjects.Count)
				return null;
			
			return deserializedObjects[index];
		}
	}

	public abstract class SerializationMethodsBase { 

		public abstract void serialize(SerializeStream stream, object obj, SerializationContext context);

		public abstract object deserialize(SerializeStream stream, DeserializationContext context);

		public void serialize(SerializeStream stream, object obj) { 
			serialize(stream, obj, new SerializationContext());
		}

		public object deserialize(SerializeStream stream) { 
			return deserialize(stream, new DeserializationContext());
		}

	}

	public class SerializationMethods : SerializationMethodsBase {
		public delegate void SerializeMethod(SerializeStream stream, object obj, SerializationContext context);
		public SerializeMethod serializeMethod { protected set; get; }
		public delegate object DeserializeMethod(SerializeStream stream, DeserializationContext context);
		public DeserializeMethod deserializeMethod { protected set; get; }

		public SerializationMethods(SerializeMethod serializeMethod, DeserializeMethod deserializeMethod) {
			this.serializeMethod = serializeMethod;
			this.deserializeMethod = deserializeMethod;
		}

		public override void serialize(SerializeStream stream, object obj, SerializationContext context) {
			serializeMethod(stream, obj, context);
		}

		public override object deserialize(SerializeStream stream, DeserializationContext context) {
			return deserializeMethod(stream, context);
		}
	}

	public static class Serializer {

		private static ConcurrentDictionary<Type, SerializationMethodsBase> serializationMethodsMap = new ConcurrentDictionary<Type, SerializationMethodsBase>();
		private static ISerializationMethodsBuilder simpleSerializationMethodsBuilder = null;
		private static ISerializationMethodsBuilder serializationMethodsBuilder = null; 
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

		public static bool getCached(Type type, out SerializationMethodsBase result) { 
			return serializationMethodsMap.TryGetValue(type, out result);
		}

		public static bool cache(SerializationMethods.SerializeMethod serializeMethod, SerializationMethods.DeserializeMethod deserializeMethod, Type type) {
			return cache(new SerializationMethods(serializeMethod, deserializeMethod), type);
		}

		public static bool cache(this SerializationMethodsBase methods, Type type) { 
			return serializationMethodsMap.TryAdd(type, methods);
		}


		public static bool buildAndCacheIntegrated(Type type) { 
			SerializationMethodsBase serializationMethods = simpleSerializationMethodsBuilder.getSerializationMethods(type, true);
			return serializationMethods != null ? cache(serializationMethods, type) : false;
		}

		public static bool buildAndCache(Type type) {
			asserNoMethodsBuilder();
			SerializationMethodsBase serializationMethods = serializationMethodsBuilder.getSerializationMethods(type, true);
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
			if(getSerializationMethods(type, out SerializationMethodsBase serializationMethods, cacheBuiltMethods)) {
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
			if(getSerializationMethods(type, out SerializationMethodsBase serializationMethods, cacheBuiltMethods)) {
				result = serializationMethods.deserialize(stream);
				return true;
			}
			result = default;
			return false;
		} 

		public static bool getSerializationMethods(Type type, out SerializationMethodsBase methods, bool? cacheBuiltMethods = null) {
			if(getCached(type, out methods)) { 
				return true;
			//For unknown types
			} else if(typeof(ISerializable).IsAssignableFrom(type)) {
				methods = new SerializationMethods(serializeSerializable, (SerializeStream stream, DeserializationContext context) => { return stream.readSerializable(type); });
				if(cacheBuiltMethods.HasValue ? cacheBuiltMethods.Value : CACHE_DEFAULT)
					serializationMethodsMap[type] = methods;
				return true;
			}  else if(typeof(IContextSerializable).IsAssignableFrom(type)) {
				methods = new SerializationMethods(serializeContextSerializable, (SerializeStream stream, DeserializationContext context) => { return stream.deserializeContextSerializable(type, context); });
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

		public static SerializationMethodsBase getSerializationMethods(Type type, bool? cacheBuiltMethods = null) { 
			getSerializationMethods(type, out SerializationMethodsBase result, cacheBuiltMethods);
			return result;
		}

		public static void serializeSerializable(SerializeStream stream, object serializable, SerializationContext context) { 
			((ISerializable)serializable).writeToStream(stream);
		}

		public static void serializeContextSerializable(this SerializeStream stream, object serializable, SerializationContext context) { 
			((IContextSerializable)serializable).writeToStream(stream, context);
		}

		public static IContextSerializable deserializeContextSerializable(this SerializeStream stream, Type type, DeserializationContext context) { 
			IContextSerializable result = (IContextSerializable)FormatterServices.GetUninitializedObject(type);
			result.readFromStream(stream, context);
			return result;
		}

		public static void useMethodsBuilder(ISerializationMethodsBuilder methodsBuilder) {
			if(serializationMethodsBuilder != null)
				throw new Exception($"SerializationMethodsBuilder is already in use ({serializationMethodsBuilder.GetType()})");
			serializationMethodsBuilder = methodsBuilder;
		}

	}

	public class SimpleSerializationMethodsBuilder : ISerializationMethodsBuilder {
		public SerializationMethodsBase getSerializationMethods(Type type, bool withCache) {
			if(type.IsArray) { 
				return new ArraySerializationMethodsChain(type, withCache);
			} else if(type.GetInterfaces().Any((Type t) => SerializeStream.isCollectionType(t))) { 
				Type elementType = type.GetInterfaces().First((Type interfaceType) => interfaceType.IsGenericType && interfaceType.GetGenericTypeDefinition() == typeof(ICollection<>)).GetGenericArguments()[0];
				return (SerializationMethodsBase)typeof(CollectionSerializationMethodsChain<,>).MakeGenericType(type, elementType).GetConstructor(new Type[] { typeof(bool) }).Invoke(new object[] { withCache });
			} else if(type.IsGenericType && type.GetGenericTypeDefinition() == typeof(KeyValuePair<,>)) { 
				Type[] genericArgs = type.GetGenericArguments();
				return (SerializationMethodsBase)typeof(KeyValuePairSerializationMethodsChain<,,>).MakeGenericType(type, genericArgs[0], genericArgs[1]).GetConstructor(new Type[] { typeof(bool) }).Invoke(new object[] { withCache });
			} else {
				return null;
			}
		}
	}

	public abstract class SerializationMethodsChain : SerializationMethodsBase { 

		public Type thisType { get; protected set; }
		public SerializationMethodsBase containTypeSerializationMethods { get; protected set; }

		public SerializationMethodsChain(Type thisType, SerializationMethodsBase containTypeSerializationMethods) {

			this.thisType = thisType;
			this.containTypeSerializationMethods = containTypeSerializationMethods;
		}
	}

	public class ArraySerializationMethodsChain : SerializationMethodsChain {

		public ArraySerializationMethodsChain(Type arrayType,  bool? cacheBuiltMethods = null) : this(arrayType, Serializer.getSerializationMethods(arrayType.GetElementType(), cacheBuiltMethods), cacheBuiltMethods) { 
		}

		public ArraySerializationMethodsChain(Type arrayType, SerializationMethodsBase serializationMethods, bool? cacheBuiltMethods = null) : base(arrayType, serializationMethods) { 
		}

		public override object deserialize(SerializeStream stream, DeserializationContext context) {
			int rank = stream.readInt32();
			if(rank == 1) { 
				int length = stream.readInt32();
				Array result = Array.CreateInstance(thisType.GetElementType(), length);
			
				for(int index = 0; index < length; index++)
					result.SetValue(containTypeSerializationMethods.deserialize(stream, context), index);
				
				return result;
			} else { 
				int[] dimensions = new int[rank];
				for(int dimensionIndex = 0; dimensionIndex < rank; dimensionIndex++) { 
					dimensions[dimensionIndex] = stream.readInt32();
				}

				Array result = Array.CreateInstance(thisType.GetElementType(), dimensions);
				foreach(int[] currentPos in SerializeStream.ndArrayWalker(dimensions))
					result.SetValue(containTypeSerializationMethods.deserialize(stream, context), currentPos);

				return result;
			}
		}

		public override void serialize(SerializeStream stream, object obj, SerializationContext context) {
			Array arr = (Array)obj;
			int rank = arr.Rank;
			stream.write(rank);
			if(rank == 1) {
				stream.write(arr.Length);
				for(int index = 0; index < arr.Length; index++)
					containTypeSerializationMethods.serialize(stream, arr.GetValue(index), context);
			} else { 
				int[] dimensions = new int[rank];
				
				for(int dimensionIndex = 0; dimensionIndex < rank; dimensionIndex++) {
					stream.write(dimensions[dimensionIndex] = arr.GetLength(dimensionIndex));
				}

				foreach(int[] currentPos in SerializeStream.ndArrayWalker(dimensions))
					containTypeSerializationMethods.serialize(stream, arr.GetValue(currentPos), context);
			}
		}
	}

	public class CollectionSerializationMethodsChain<COLLECTION_TYPE, ELEMENT_TYPE> : SerializationMethodsChain where COLLECTION_TYPE : ICollection<ELEMENT_TYPE>, new() {

		public CollectionSerializationMethodsChain(bool? cacheBuiltMethods = null) : this(Serializer.getSerializationMethods(typeof(ELEMENT_TYPE), cacheBuiltMethods), cacheBuiltMethods) { }

		public CollectionSerializationMethodsChain(SerializationMethodsBase serializationMethods, bool? cacheBuiltMethods = null) : base(typeof(COLLECTION_TYPE), serializationMethods) { }

		public override object deserialize(SerializeStream stream, DeserializationContext context) {
			COLLECTION_TYPE result = new COLLECTION_TYPE();
			int length = stream.readInt32();
			for(int index = 0; index < length; index++)
				result.Add((ELEMENT_TYPE)containTypeSerializationMethods.deserialize(stream, context));
			return result;
		}

		public override void serialize(SerializeStream stream, object obj, SerializationContext context) {
			COLLECTION_TYPE collectionObj = (COLLECTION_TYPE)obj;
			stream.write(collectionObj.Count);
			foreach(ELEMENT_TYPE element in collectionObj)
				containTypeSerializationMethods.serialize(stream, element, context);
		}
	}

	public class KeyValuePairSerializationMethodsChain<KEY_VALUE_PAIR_TYPE, KEY_TYPE, VALUE_TYPE> : SerializationMethodsChain {

		SerializationMethodsBase keySerializationMethods;
		SerializationMethodsBase valueSerializationMethods;

		public KeyValuePairSerializationMethodsChain(bool? cacheBuiltMethods = null) : this(Serializer.getSerializationMethods(typeof(KEY_TYPE)), Serializer.getSerializationMethods(typeof(VALUE_TYPE)),  cacheBuiltMethods) { }

		public KeyValuePairSerializationMethodsChain(SerializationMethodsBase keySerializationMethods, SerializationMethodsBase valueSerializationMethods, bool? cacheBuiltMethods = null) : base(typeof(KEY_VALUE_PAIR_TYPE), null) { 
			this.keySerializationMethods = keySerializationMethods;
			this.valueSerializationMethods = valueSerializationMethods;
		}

		public override object deserialize(SerializeStream stream, DeserializationContext context) {
			KEY_TYPE key = (KEY_TYPE)keySerializationMethods.deserialize(stream, context);
			VALUE_TYPE value = (VALUE_TYPE)valueSerializationMethods.deserialize(stream, context);
			return new KeyValuePair<KEY_TYPE, VALUE_TYPE>(key, value);
		}

		public override void serialize(SerializeStream stream, object obj, SerializationContext context) {
			KeyValuePair<KEY_TYPE, VALUE_TYPE> keyValuePair = (KeyValuePair<KEY_TYPE, VALUE_TYPE>)obj;
			keySerializationMethods.serialize(stream, keyValuePair.Key, context);
			valueSerializationMethods.serialize(stream, keyValuePair.Value, context);
		}
	}

	public static class StandartSerializationMethods {

		/*				Numeric types				*/

		public static void serializeInt64(SerializeStream stream, object value, SerializationContext context) { stream.write((Int64)value); }
		public static void serializeInt32(SerializeStream stream, object value, SerializationContext context) { stream.write((Int32)value); }
		public static void serializeInt16(SerializeStream stream, object value, SerializationContext context) { stream.write((Int16)value); }
		public static void serializeInt8(SerializeStream stream, object value, SerializationContext context) { stream.write((sbyte)value); }


		public static void serializeUInt64(SerializeStream stream, object value, SerializationContext context) { stream.write((UInt64)value); }
		public static void serializeUInt32(SerializeStream stream, object value, SerializationContext context) { stream.write((UInt32)value); }
		public static void serializeUInt16(SerializeStream stream, object value, SerializationContext context) { stream.write((UInt16)value); }
		public static void serializeUInt8(SerializeStream stream, object value, SerializationContext context) { stream.write((byte)value); }



		public static void serializeFloat(SerializeStream stream, object value, SerializationContext context) { stream.write((Single)value); }
		public static void serializeDouble(SerializeStream stream, object value, SerializationContext context) { stream.write((Double)value); }



		public static object deserializeInt64(SerializeStream stream, DeserializationContext context) { return stream.readInt64(); }
		public static object deserializeInt32(SerializeStream stream, DeserializationContext context) { return stream.readInt32(); }
		public static object deserializeInt16(SerializeStream stream, DeserializationContext context) { return stream.readInt16(); }
		public static object deserializeInt8(SerializeStream stream, DeserializationContext context) { return stream.readSByte(); }


		public static object deserializeUInt64(SerializeStream stream, DeserializationContext context) { return stream.readUInt64(); }
		public static object deserializeUInt32(SerializeStream stream, DeserializationContext context) { return stream.readUInt32(); }
		public static object deserializeUInt16(SerializeStream stream, DeserializationContext context) { return stream.readUInt16(); }
		public static object deserializeUInt8(SerializeStream stream, DeserializationContext context) { return stream.readByte(); }

		


		public static object deserializeFloat(SerializeStream stream, DeserializationContext context) { return stream.readFloat(); }
		public static object deserializeDouble(SerializeStream stream, DeserializationContext context) { return stream.readDouble(); }

		/*				Extended types				*/

		public static void serializeByteArray(SerializeStream stream, object value, SerializationContext context) { stream.write((byte[])value); }
		public static object deserializeByteArray(SerializeStream stream, DeserializationContext context) { return stream.readBytes(); }


		public static void serializeString(SerializeStream stream, object value, SerializationContext context) { stream.write((string)value); }
		public static object deserializeString(SerializeStream stream, DeserializationContext context) { return stream.readString(); }



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

	public interface ISerializationMethodsBuilder { 

		SerializationMethodsBase getSerializationMethods(Type type, bool withCache);

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
	
	public interface IContextSerializable { 
		void writeToStream(SerializeStream stream, SerializationContext context);

		void readFromStream(SerializeStream stream, DeserializationContext context);
	}

}
