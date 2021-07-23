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

	

	public abstract class SerializationMethodsBase { 

		public abstract void Serialize(SerializeStream stream, object obj, SerializationContext context);

		public abstract object Deserialize(SerializeStream stream, DeserializationContext context);

		public void Serialize(SerializeStream stream, object obj) { 
			Serialize(stream, obj, new SerializationContext());
		}

		public object Deserialize(SerializeStream stream) { 
			return Deserialize(stream, new DeserializationContext());
		}

	}

	public class SerializationMethods : SerializationMethodsBase {
		public delegate void SerializeMethod(SerializeStream stream, object obj, SerializationContext context);
		public SerializeMethod TypeSerializeMethod { protected set; get; }
		public delegate object DeserializeMethod(SerializeStream stream, DeserializationContext context);
		public DeserializeMethod TypeDeserializeMethod { protected set; get; }

		public SerializationMethods(SerializeMethod serializeMethod, DeserializeMethod deserializeMethod) {
			this.TypeSerializeMethod = serializeMethod;
			this.TypeDeserializeMethod = deserializeMethod;
		}

		public override void Serialize(SerializeStream stream, object obj, SerializationContext context) {
			TypeSerializeMethod(stream, obj, context);
		}

		public override object Deserialize(SerializeStream stream, DeserializationContext context) {
			return TypeDeserializeMethod(stream, context);
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
			StandartSerializationMethods.Register();
		}

		//Cache managment interface

		public static bool IsCached<TYPE>() { return IsCached(typeof(TYPE)); }
		public static bool IsCached(Type type) { 
			return serializationMethodsMap.ContainsKey(type);
		}


		public static bool GetCached<TYPE>(out SerializationMethodsBase result) { return GetCached(typeof(TYPE), out result); }
		public static bool GetCached(Type type, out SerializationMethodsBase result) { 
			return serializationMethodsMap.TryGetValue(type, out result);
		}


		public static bool Cache<TYPE>(SerializationMethods.SerializeMethod serializeMethod, SerializationMethods.DeserializeMethod deserializeMethod) { return Cache(serializeMethod, deserializeMethod, typeof(TYPE)); }
		public static bool Cache(SerializationMethods.SerializeMethod serializeMethod, SerializationMethods.DeserializeMethod deserializeMethod, Type type) {
			return Cache(new SerializationMethods(serializeMethod, deserializeMethod), type);
		}


		public static bool Cache<TYPE>(this SerializationMethodsBase methods) { return Cache(methods, typeof(TYPE)); }
		public static bool Cache(this SerializationMethodsBase methods, Type type) { 
			return serializationMethodsMap.TryAdd(type, methods);
		}


		public static bool BuildAndCacheIntegrated<TYPE>() { return BuildAndCacheIntegrated(typeof(TYPE)); }
		public static bool BuildAndCacheIntegrated(Type type) { 
			SerializationMethodsBase serializationMethods = simpleSerializationMethodsBuilder.GetSerializationMethods(type, true);
			return serializationMethods != null ? Cache(serializationMethods, type) : false;
		}

		public static bool BuildAndCache<TYPE>() { return BuildAndCache(typeof(TYPE)); }
		public static bool BuildAndCache(Type type) {
			AsserNoMethodsBuilder();
			SerializationMethodsBase serializationMethods = serializationMethodsBuilder.GetSerializationMethods(type, true);
			return serializationMethods != null ? Cache(serializationMethods, type) : false;
		}


		private static void AsserNoMethodsBuilder() { 
			if(serializationMethodsBuilder == null)
				throw new SerializationException("No serialization methods builder");
		}

		//1.4 Serialize TYPE safe
		public static bool SerializeSafe<TYPE>(this SerializeStream stream, TYPE obj, bool? cacheBuiltMethods = null, SerializationContext context = null) { 
			 return SerializeSafe(stream, obj, typeof(TYPE), cacheBuiltMethods, context);
		}

		//1.3 Serialize TYPE unsafe
		public static void Serialize<TYPE>(this SerializeStream stream, TYPE obj, bool? cacheBuiltMethods = null, SerializationContext context = null) { 
			Serialize(stream, obj, typeof(TYPE), cacheBuiltMethods, context);
		}

		//1.2 Serialize object unsafe
		public static void Serialize(this SerializeStream stream, object obj, Type type, bool? cacheBuiltMethods = null, SerializationContext context = null) {
			if(!SerializeSafe(stream, obj, type, cacheBuiltMethods, context)) {
				throw new SerializationException($"Can't serialize type {type}");
			}
		} 

		//1.1 Serialize object safe
		public static bool SerializeSafe(this SerializeStream stream, object obj, Type type, bool? cacheBuiltMethods = null, SerializationContext context = null) {
			if(GetSerializationMethods(type, out SerializationMethodsBase serializationMethods, cacheBuiltMethods)) {
				if(context == null)
					serializationMethods.Serialize(stream, obj);
				else
					serializationMethods.Serialize(stream, obj, context);
				return true;
			}
			return false;
		} 
		

		//2.4 Deserialize TYPE unsafe
		public static TYPE Deserialize<TYPE>(this SerializeStream stream, bool? cacheBuiltMethods = null, DeserializationContext context = null) { 
			if(stream.DeserializeSafe<TYPE>(out TYPE result, cacheBuiltMethods, context)) {
				return result;
			}

			throw new SerializationException($"Can't deserialize type {typeof(TYPE)}");
		}

		//2.3 Deserialize TYPE safe 
		public static bool DeserializeSafe<TYPE>(this SerializeStream stream, out TYPE obj, bool? cacheBuiltMethods = null, DeserializationContext context = null) { 
			bool boolResult = DeserializeSafe(stream, out object result, typeof(TYPE), cacheBuiltMethods, context);
			obj = (TYPE)result;
			return boolResult;
		}

		//2.2 Deserialize object unsafe
		public static object Deserialize(this SerializeStream stream, Type type, bool? cacheBuiltMethods = null, DeserializationContext context = null) { 
			if(DeserializeSafe(stream, out object result, type, cacheBuiltMethods, context)) {
				return result;
			}

			throw new SerializationException($"Can't deserialize type {type}");
		}

		//2.1 Deserialize object safe with return
		public static bool DeserializeSafe(this SerializeStream stream, out object result, Type type, bool? cacheBuiltMethods = null, DeserializationContext context = null) {
			if(GetSerializationMethods(type, out SerializationMethodsBase serializationMethods, cacheBuiltMethods)) {
				if(context == null)
					result = serializationMethods.Deserialize(stream);
				else
					result = serializationMethods.Deserialize(stream, context);
				return true;
			}
			result = default;
			return false;
		} 

		public static bool GetSerializationMethods(Type type, out SerializationMethodsBase methods, bool? cacheBuiltMethods = null) {
			if(GetCached(type, out methods)) { 
				return true;
			//For unknown types
			} else if(typeof(ISerializable).IsAssignableFrom(type)) {
				methods = new SerializationMethods(SerializeSerializable, (SerializeStream stream, DeserializationContext context) => { return stream.ReadSerializable(type); });
				if(cacheBuiltMethods.HasValue ? cacheBuiltMethods.Value : CACHE_DEFAULT)
					serializationMethodsMap[type] = methods;
				return true;
			}  else if(typeof(IContextSerializable).IsAssignableFrom(type)) {
				methods = new SerializationMethods(SerializeContextSerializable, (SerializeStream stream, DeserializationContext context) => { return stream.DeserializeContextSerializable(type, context); });
				if(cacheBuiltMethods.HasValue ? cacheBuiltMethods.Value : CACHE_DEFAULT)
					serializationMethodsMap[type] = methods;
				return true;
			} else {
				if(serializationMethodsBuilder == null) { 
					if((methods = simpleSerializationMethodsBuilder.GetSerializationMethods(type, cacheBuiltMethods.HasValue ? cacheBuiltMethods.Value : CACHE_DEFAULT)) != null) {
						return true;
					}
				} else {
					if((methods = serializationMethodsBuilder.GetSerializationMethods(type, cacheBuiltMethods.HasValue ? cacheBuiltMethods.Value : CACHE_DEFAULT)) != null) { 
						if(cacheBuiltMethods.HasValue ? cacheBuiltMethods.Value : CACHE_DEFAULT)
							serializationMethodsMap[type] = methods;
						return true;
					} 
				}

				return false;
				
			}
		}

		public static SerializationMethodsBase GetSerializationMethods(Type type, bool? cacheBuiltMethods = null) { 
			GetSerializationMethods(type, out SerializationMethodsBase result, cacheBuiltMethods);
			return result;
		}

		public static void SerializeSerializable(SerializeStream stream, object serializable, SerializationContext context) { 
			((ISerializable)serializable).WriteToStream(stream);
		}

		public static void SerializeContextSerializable(this SerializeStream stream, object serializable, SerializationContext context) { 
			((IContextSerializable)serializable).WriteToStream(stream, context);
		}

		public static IContextSerializable DeserializeContextSerializable(this SerializeStream stream, Type type, DeserializationContext context) { 
			IContextSerializable result = (IContextSerializable)FormatterServices.GetUninitializedObject(type);
			result.ReadFromStream(stream, context);
			return result;
		}

		public static void UseMethodsBuilder(ISerializationMethodsBuilder methodsBuilder) {
			if(serializationMethodsBuilder != null)
				throw new Exception($"SerializationMethodsBuilder is already in use ({serializationMethodsBuilder.GetType()})");
			serializationMethodsBuilder = methodsBuilder;
		}

	}

	public class SimpleSerializationMethodsBuilder : ISerializationMethodsBuilder {
		public SerializationMethodsBase GetSerializationMethods(Type type, bool withCache) {
			if(type.IsArray) { 
				return new ArraySerializationMethodsChain(type, withCache);
			} else if(type.GetInterfaces().Any((Type t) => SerializeStream.IsCollectionType(t))) { 
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

		public Type ThisType { get; protected set; }
		public SerializationMethodsBase ContainTypeSerializationMethods { get; protected set; }

		public SerializationMethodsChain(Type thisType, SerializationMethodsBase containTypeSerializationMethods) {

			this.ThisType = thisType;
			this.ContainTypeSerializationMethods = containTypeSerializationMethods;
		}
	}

	public class ArraySerializationMethodsChain : SerializationMethodsChain {

		public ArraySerializationMethodsChain(Type arrayType,  bool? cacheBuiltMethods = null) : this(arrayType, Serializer.GetSerializationMethods(arrayType.GetElementType(), cacheBuiltMethods), cacheBuiltMethods) { 
		}

		public ArraySerializationMethodsChain(Type arrayType, SerializationMethodsBase serializationMethods, bool? cacheBuiltMethods = null) : base(arrayType, serializationMethods) { 
		}

		public override void Serialize(SerializeStream stream, object obj, SerializationContext context) {
			if(context.Optimize(stream, obj))
				return;

			Array arr = (Array)obj;
			int rank = arr.Rank;
			stream.Write(rank);
			if(rank == 1) {
				stream.Write(arr.Length);
				for(int index = 0; index < arr.Length; index++)
					ContainTypeSerializationMethods.Serialize(stream, arr.GetValue(index), context);
			} else { 
				int[] dimensions = new int[rank];
				
				for(int dimensionIndex = 0; dimensionIndex < rank; dimensionIndex++) {
					stream.Write(dimensions[dimensionIndex] = arr.GetLength(dimensionIndex));
				}

				foreach(int[] currentPos in SerializeStream.NDArrayWalker(dimensions))
					ContainTypeSerializationMethods.Serialize(stream, arr.GetValue(currentPos), context);
			}
		}

		public override object Deserialize(SerializeStream stream, DeserializationContext context) {
			(bool, object) optimizationResult = context.Optimize(stream);
			if(optimizationResult.Item1) { 
				return optimizationResult.Item2;
			}

			int rank = stream.ReadInt32();

			Array result;
			if(rank == 1) { 
				int length = stream.ReadInt32();
				result = Array.CreateInstance(ThisType.GetElementType(), length);
				context.AddObject(result);
			
				for(int index = 0; index < length; index++)
					result.SetValue(ContainTypeSerializationMethods.Deserialize(stream, context), index);
			} else { 
				int[] dimensions = new int[rank];
				for(int dimensionIndex = 0; dimensionIndex < rank; dimensionIndex++) { 
					dimensions[dimensionIndex] = stream.ReadInt32();
				}

				result = Array.CreateInstance(ThisType.GetElementType(), dimensions);
				context.AddObject(result);
				foreach(int[] currentPos in SerializeStream.NDArrayWalker(dimensions))
					result.SetValue(ContainTypeSerializationMethods.Deserialize(stream, context), currentPos);
			}

			return result;
		}
	}

	public class CollectionSerializationMethodsChain<COLLECTION_TYPE, ELEMENT_TYPE> : SerializationMethodsChain where COLLECTION_TYPE : ICollection<ELEMENT_TYPE>, new() {

		public CollectionSerializationMethodsChain(bool? cacheBuiltMethods = null) : this(Serializer.GetSerializationMethods(typeof(ELEMENT_TYPE), cacheBuiltMethods), cacheBuiltMethods) { }

		public CollectionSerializationMethodsChain(SerializationMethodsBase serializationMethods, bool? cacheBuiltMethods = null) : base(typeof(COLLECTION_TYPE), serializationMethods) { }

		public override void Serialize(SerializeStream stream, object obj, SerializationContext context) {
			if(context.Optimize(stream, obj))
				return;
			COLLECTION_TYPE collectionObj = (COLLECTION_TYPE)obj;
			
			long countPosition = stream.RememberAndSeek(sizeof(Int32));
			Int32 elementsCounter = 0;
			foreach(ELEMENT_TYPE element in collectionObj) {
				ContainTypeSerializationMethods.Serialize(stream, element, context);
				elementsCounter++;
			}
			long posBuffer = stream.Position;
			stream.Position = countPosition;
			stream.WriteInt32(elementsCounter);
			stream.Position = posBuffer;
		}

		public override object Deserialize(SerializeStream stream, DeserializationContext context) {
			(bool, object) optimizationResult = context.Optimize(stream);
			if(optimizationResult.Item1) { 
				return optimizationResult.Item2;
			}

			COLLECTION_TYPE result = new COLLECTION_TYPE();
			context.AddObject(result);
			int length = stream.ReadInt32();
			for(int index = 0; index < length; index++)
				result.Add((ELEMENT_TYPE)ContainTypeSerializationMethods.Deserialize(stream, context));

			return result;
		}
	}

	public class KeyValuePairSerializationMethodsChain<KEY_VALUE_PAIR_TYPE, KEY_TYPE, VALUE_TYPE> : SerializationMethodsChain {

		SerializationMethodsBase keySerializationMethods;
		SerializationMethodsBase valueSerializationMethods;

		public KeyValuePairSerializationMethodsChain(bool? cacheBuiltMethods = null) : this(Serializer.GetSerializationMethods(typeof(KEY_TYPE)), Serializer.GetSerializationMethods(typeof(VALUE_TYPE)),  cacheBuiltMethods) { }

		public KeyValuePairSerializationMethodsChain(SerializationMethodsBase keySerializationMethods, SerializationMethodsBase valueSerializationMethods, bool? cacheBuiltMethods = null) : base(typeof(KEY_VALUE_PAIR_TYPE), null) { 
			this.keySerializationMethods = keySerializationMethods;
			this.valueSerializationMethods = valueSerializationMethods;
		}

		public override void Serialize(SerializeStream stream, object obj, SerializationContext context) {
			KeyValuePair<KEY_TYPE, VALUE_TYPE> keyValuePair = (KeyValuePair<KEY_TYPE, VALUE_TYPE>)obj;
			keySerializationMethods.Serialize(stream, keyValuePair.Key, context);
			valueSerializationMethods.Serialize(stream, keyValuePair.Value, context);
		}

		public override object Deserialize(SerializeStream stream, DeserializationContext context) {
			KEY_TYPE key = (KEY_TYPE)keySerializationMethods.Deserialize(stream, context);
			VALUE_TYPE value = (VALUE_TYPE)valueSerializationMethods.Deserialize(stream, context);
			return new KeyValuePair<KEY_TYPE, VALUE_TYPE>(key, value);
		}
	}

	public static class StandartSerializationMethods {

		/*				Numeric types				*/

		public static void SerializeInt64(SerializeStream stream, object value, SerializationContext context) { stream.WriteInt64((Int64)value); }
		public static void SerializeInt32(SerializeStream stream, object value, SerializationContext context) { stream.WriteInt32((Int32)value); }
		public static void SerializeInt16(SerializeStream stream, object value, SerializationContext context) { stream.WriteInt16((Int16)value); }
		public static void SerializeInt8(SerializeStream stream, object value, SerializationContext context) { stream.WriteSByte((sbyte)value); }


		public static void SerializeUInt64(SerializeStream stream, object value, SerializationContext context) { stream.WriteUInt64((UInt64)value); }
		public static void SerializeUInt32(SerializeStream stream, object value, SerializationContext context) { stream.WriteUInt32((UInt32)value); }
		public static void SerializeUInt16(SerializeStream stream, object value, SerializationContext context) { stream.WriteUInt16((UInt16)value); }
		public static void SerializeUInt8(SerializeStream stream, object value, SerializationContext context) { stream.WriteByte((byte)value); }



		public static void SerializeFloat(SerializeStream stream, object value, SerializationContext context) { stream.WriteFloat((Single)value); }
		public static void SerializeDouble(SerializeStream stream, object value, SerializationContext context) { stream.WriteDouble((Double)value); }



		public static object DeserializeInt64(SerializeStream stream, DeserializationContext context) { return stream.ReadInt64(); }
		public static object DeserializeInt32(SerializeStream stream, DeserializationContext context) { return stream.ReadInt32(); }
		public static object DeserializeInt16(SerializeStream stream, DeserializationContext context) { return stream.ReadInt16(); }
		public static object DeserializeInt8(SerializeStream stream, DeserializationContext context) { return stream.ReadSByte(); }


		public static object DeserializeUInt64(SerializeStream stream, DeserializationContext context) { return stream.ReadUInt64(); }
		public static object DeserializeUInt32(SerializeStream stream, DeserializationContext context) { return stream.ReadUInt32(); }
		public static object DeserializeUInt16(SerializeStream stream, DeserializationContext context) { return stream.ReadUInt16(); }
		public static object DeserializeUInt8(SerializeStream stream, DeserializationContext context) { return stream.ReadByte(); }

		


		public static object DeserializeFloat(SerializeStream stream, DeserializationContext context) { return stream.ReadFloat(); }
		public static object DeserializeDouble(SerializeStream stream, DeserializationContext context) { return stream.ReadDouble(); }

		/*				Extended types				*/

		public static void SerializeByteArray(SerializeStream stream, object value, SerializationContext context) { 
			if(context.Optimize(stream, value)) { 
				return;
			}
			stream.Write((byte[])value); 
		}

		public static object DeserializeByteArray(SerializeStream stream, DeserializationContext context) { 
			(bool, object) optimizationResult = context.Optimize(stream);
			if(optimizationResult.Item1)
				return optimizationResult.Item2;
			return context.AddObject(stream.ReadBytes()); 
		}


		public static void SerializeString(SerializeStream stream, object value, SerializationContext context) { 
			if(context.Optimize(stream, value)) { 
				return;
			}
			stream.Write((string)value); 
		}
		public static object DeserializeString(SerializeStream stream, DeserializationContext context) { 
			(bool, object) optimizationResult = context.Optimize(stream);
			if(optimizationResult.Item1)
				return optimizationResult.Item2;
			return context.AddObject(stream.ReadString());
		}



		internal static void Register() { 
			Serializer.Cache(SerializeInt64, DeserializeInt64, typeof(Int64));
			Serializer.Cache(SerializeInt32, DeserializeInt32, typeof(Int32));
			Serializer.Cache(SerializeInt16, DeserializeInt16, typeof(Int16));
			Serializer.Cache(SerializeInt8,  DeserializeInt8, typeof(sbyte));

			Serializer.Cache(SerializeUInt64, DeserializeUInt64, typeof(UInt64));
			Serializer.Cache(SerializeUInt32, DeserializeUInt32, typeof(UInt32));
			Serializer.Cache(SerializeUInt16, DeserializeUInt16, typeof(UInt16));
			Serializer.Cache(SerializeUInt8,  DeserializeUInt8, typeof(byte));

			Serializer.Cache(SerializeFloat, DeserializeFloat, typeof(Single));
			Serializer.Cache(SerializeDouble, DeserializeDouble, typeof(Double));

			Serializer.Cache(SerializeByteArray, DeserializeByteArray, typeof(byte[]));
			Serializer.Cache(SerializeString, DeserializeString, typeof(string));
		}
	}

	public interface ISerializationMethodsBuilder { 

		SerializationMethodsBase GetSerializationMethods(Type type, bool withCache);

	}

	public class SerializationException : Exception { 

		public SerializationException(string message) : base(message) {}

	}

	public class SerializationRule : Attribute { 

		public bool IsSerializable { get; private set; }

		public SerializationRule(bool isSerializable) { 
			this.IsSerializable = isSerializable;
		}

	}
	
	public interface IContextSerializable { 
		void WriteToStream(SerializeStream stream, SerializationContext context);

		void ReadFromStream(SerializeStream stream, DeserializationContext context);
	}

	//--------------------------------------------------------------------------------------------------------
	//--------------------------------Context optimization for reference types--------------------------------
	//--------------------------------------------------------------------------------------------------------

	//nulls, object references duplicates (include references cicles) cases optimization

	public static class ContextOptimizationConsts { 
		public const byte CODE_NORMAL_OBJECT = 0;
		public const byte CODE_NULL = 1;
		public const byte CODE_ALREADY_HANDLED = 2;
	}

	public class SerializationContext { 
		
		Dictionary<object, int> SerializedObjects = new Dictionary<object, int>();
		int currentObjectIndex = 0;

		//Call this before serialization
		//If method returns true, you don't have to serialize the object, false else
		public bool Optimize(SerializeStream stream, object obj) {
			if(obj == null) { 
				stream.Write(ContextOptimizationConsts.CODE_NULL);
				return true;
			} else if(SerializedObjects.TryGetValue(obj, out int index)) { 
				stream.Write(ContextOptimizationConsts.CODE_ALREADY_HANDLED);
				stream.Write(index);
				return true;
			} else { 
				stream.Write(ContextOptimizationConsts.CODE_NORMAL_OBJECT);
				//Don't optimize structures
				if(!obj.GetType().IsValueType) {
					SerializedObjects.Add(obj, currentObjectIndex);
					currentObjectIndex++;
				}
				return false;
			}
		}
	}

	public class DeserializationContext { 
		
		List<object> deserializedObjects = new List<object>();

		//Return's tuple where Item1 = true if object is in Item2
		/*
		 * Example deserialization code:
		 * var optimizeResult = context.optimize(stream); //!!!
		 * if(!optimizeResult.Item1)
		 *     return optimizeResult.Item2;
		 * 
		 * 
		 * //deserialization code
		 * ...
		 * var objectInstance = new object();
		 * context.addObject(result); //!!!
		 * ...
		 * 
		 * return result;
		 * }
		 */
		public (bool, object) Optimize(SerializeStream stream) {
			byte code = stream.ReadByte();
			if(code == ContextOptimizationConsts.CODE_NULL) { 
				return (true, null);
			} else if(code == ContextOptimizationConsts.CODE_ALREADY_HANDLED) { 
				int index = stream.ReadInt32();
				return (true, deserializedObjects[index]);
			} else if(code == ContextOptimizationConsts.CODE_NORMAL_OBJECT) { 
				return (false, null);
			} else { 
				throw new SerializationException($"Invalid context optimization code {code}");
			}
		}

		//Returns this object
		public object AddObject(object obj) {
			if(!obj.GetType().IsValueType)
				deserializedObjects.Add(obj);
			return obj;
		}
	}

}
