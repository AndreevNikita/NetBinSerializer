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

	public class Serializer {

		private ConcurrentDictionary<Type, SerializationMethodsBase> serializationMethodsMap = new ConcurrentDictionary<Type, SerializationMethodsBase>();
		private ISerializationMethodsBuilder simpleSerializationMethodsBuilder = null;
		private ISerializationMethodsBuilder serializationMethodsBuilder = null;
		public bool CacheDefault { get; set; } = true;
		//public static bool 

		public Serializer(bool cacheDefault = true) {
			simpleSerializationMethodsBuilder = new SimpleSerializationMethodsBuilder();
			this.CacheDefault = cacheDefault;
			this.RegisterStandartSerializationMethods();
		}

		//Cache managment interface

		public bool IsCached<TYPE>() { return IsCached(typeof(TYPE)); }
		public bool IsCached(Type type) {
			return serializationMethodsMap.ContainsKey(type);
		}


		public bool GetCached<TYPE>(out SerializationMethodsBase result) { return GetCached(typeof(TYPE), out result); }
		public bool GetCached(Type type, out SerializationMethodsBase result) {
			return serializationMethodsMap.TryGetValue(type, out result);
		}


		public bool Cache<TYPE>(SerializationMethods.SerializeMethod serializeMethod, SerializationMethods.DeserializeMethod deserializeMethod) { return Cache(serializeMethod, deserializeMethod, typeof(TYPE)); }
		public bool Cache(SerializationMethods.SerializeMethod serializeMethod, SerializationMethods.DeserializeMethod deserializeMethod, Type type) {
			return Cache(new SerializationMethods(serializeMethod, deserializeMethod), type);
		}


		public bool Cache<TYPE>(SerializationMethodsBase methods) { return Cache(methods, typeof(TYPE)); }
		public bool Cache(SerializationMethodsBase methods, Type type) {
			return serializationMethodsMap.TryAdd(type, methods);
		}


		public bool BuildAndCacheIntegrated<TYPE>() { return BuildAndCacheIntegrated(typeof(TYPE)); }
		public bool BuildAndCacheIntegrated(Type type) {
			SerializationMethodsBase serializationMethods = simpleSerializationMethodsBuilder.GetSerializationMethods(this, type, true);
			return serializationMethods != null ? Cache(serializationMethods, type) : false;
		}

		public bool BuildAndCache<TYPE>() { return BuildAndCache(typeof(TYPE)); }
		public bool BuildAndCache(Type type) {
			AsserNoMethodsBuilder();
			SerializationMethodsBase serializationMethods = serializationMethodsBuilder.GetSerializationMethods(this, type, true);
			return serializationMethods != null ? Cache(serializationMethods, type) : false;
		}


		private void AsserNoMethodsBuilder() {
			if(serializationMethodsBuilder == null)
				throw new SerializationException("No serialization methods builder");
		}

		public bool GetSerializationMethods(Type type, out SerializationMethodsBase methods, bool? cacheBuiltMethods = null) {
			if(GetCached(type, out methods)) {
				return true;
				//For unknown types
			} else if(typeof(ISerializable).IsAssignableFrom(type)) {
				methods = new SerializationMethods(SerializeSerializable, (SerializeStream stream, DeserializationContext context) => { return stream.ReadSerializable(type); });
				if(cacheBuiltMethods.HasValue ? cacheBuiltMethods.Value : CacheDefault)
					serializationMethodsMap[type] = methods;
				return true;
			} else if(typeof(IContextSerializable).IsAssignableFrom(type)) {
				methods = new SerializationMethods(SerializeContextSerializable, (SerializeStream stream, DeserializationContext context) => { return DeserializeContextSerializable(stream, type, context); });
				if(cacheBuiltMethods.HasValue ? cacheBuiltMethods.Value : CacheDefault)
					serializationMethodsMap[type] = methods;
				return true;
			} else {
				if(serializationMethodsBuilder == null) {
					if((methods = simpleSerializationMethodsBuilder.GetSerializationMethods(this, type, cacheBuiltMethods.HasValue ? cacheBuiltMethods.Value : CacheDefault)) != null) {
						return true;
					}
				} else {
					if((methods = serializationMethodsBuilder.GetSerializationMethods(this, type, cacheBuiltMethods.HasValue ? cacheBuiltMethods.Value : CacheDefault)) != null) {
						if(cacheBuiltMethods.HasValue ? cacheBuiltMethods.Value : CacheDefault)
							serializationMethodsMap[type] = methods;
						return true;
					}
				}

				return false;

			}
		}

		public SerializationMethodsBase GetSerializationMethods(Type type, bool? cacheBuiltMethods = null) {
			GetSerializationMethods(type, out SerializationMethodsBase result, cacheBuiltMethods);
			return result;
		}

		public void SerializeSerializable(SerializeStream stream, object serializable, SerializationContext context) {
			((ISerializable)serializable).WriteToStream(stream);
		}

		public void UseMethodsBuilder(ISerializationMethodsBuilder methodsBuilder) {
			if(serializationMethodsBuilder != null)
				throw new Exception($"SerializationMethodsBuilder is already in use ({serializationMethodsBuilder.GetType()})");
			serializationMethodsBuilder = methodsBuilder;
		}

		//1.4 Serialize TYPE safe
		public bool SerializeSafe<TYPE>(SerializeStream stream, TYPE obj, bool? cacheBuiltMethods = null, SerializationContext context = null) {
			return SerializeSafe(stream, obj, typeof(TYPE), cacheBuiltMethods, context);
		}

		//1.3 Serialize TYPE unsafe
		public void Serialize<TYPE>(SerializeStream stream, TYPE obj, bool? cacheBuiltMethods = null, SerializationContext context = null) {
			Serialize(stream, obj, typeof(TYPE), cacheBuiltMethods, context);
		}

		//1.2 Serialize object unsafe
		public void Serialize(SerializeStream stream, object obj, Type type, bool? cacheBuiltMethods = null, SerializationContext context = null) {
			if(!SerializeSafe(stream, obj, type, cacheBuiltMethods, context)) {
				throw new SerializationException($"Can't serialize type {type}");
			}
		}

		//1.1 Serialize object safe
		public bool SerializeSafe(SerializeStream stream, object obj, Type type, bool? cacheBuiltMethods = null, SerializationContext context = null) {
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
		public TYPE Deserialize<TYPE>(SerializeStream stream, bool? cacheBuiltMethods = null, DeserializationContext context = null) {
			if(DeserializeSafe<TYPE>(stream, out TYPE result, cacheBuiltMethods, context)) {
				return result;
			}

			throw new SerializationException($"Can't deserialize type {typeof(TYPE)}");
		}

		//2.3 Deserialize TYPE safe 
		public bool DeserializeSafe<TYPE>(SerializeStream stream, out TYPE obj, bool? cacheBuiltMethods = null, DeserializationContext context = null) {
			bool boolResult = DeserializeSafe(stream, out object result, typeof(TYPE), cacheBuiltMethods, context);
			obj = (TYPE)result;
			return boolResult;
		}

		//2.2 Deserialize object unsafe
		public object Deserialize(SerializeStream stream, Type type, bool? cacheBuiltMethods = null, DeserializationContext context = null) {
			if(DeserializeSafe(stream, out object result, type, cacheBuiltMethods, context)) {
				return result;
			}

			throw new SerializationException($"Can't deserialize type {type}");
		}

		//2.1 Deserialize object safe with return
		public bool DeserializeSafe(SerializeStream stream, out object result, Type type, bool? cacheBuiltMethods = null, DeserializationContext context = null) {
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

		public void SerializeContextSerializable(SerializeStream stream, object serializable, SerializationContext context) {
			((IContextSerializable)serializable).WriteToStream(stream, context);
		}

		public IContextSerializable DeserializeContextSerializable(SerializeStream stream, Type type, DeserializationContext context) {
			IContextSerializable result = (IContextSerializable)FormatterServices.GetUninitializedObject(type);
			result.ReadFromStream(stream, context);
			return result;
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



		internal static void RegisterStandartSerializationMethods(this Serializer serializer) {
			serializer.Cache(SerializeInt64, DeserializeInt64, typeof(Int64));
			serializer.Cache(SerializeInt32, DeserializeInt32, typeof(Int32));
			serializer.Cache(SerializeInt16, DeserializeInt16, typeof(Int16));
			serializer.Cache(SerializeInt8, DeserializeInt8, typeof(sbyte));

			serializer.Cache(SerializeUInt64, DeserializeUInt64, typeof(UInt64));
			serializer.Cache(SerializeUInt32, DeserializeUInt32, typeof(UInt32));
			serializer.Cache(SerializeUInt16, DeserializeUInt16, typeof(UInt16));
			serializer.Cache(SerializeUInt8, DeserializeUInt8, typeof(byte));

			serializer.Cache(SerializeFloat, DeserializeFloat, typeof(Single));
			serializer.Cache(SerializeDouble, DeserializeDouble, typeof(Double));

			serializer.Cache(SerializeByteArray, DeserializeByteArray, typeof(byte[]));
			serializer.Cache(SerializeString, DeserializeString, typeof(string));
		}
	}

	public interface ISerializationMethodsBuilder {

		SerializationMethodsBase GetSerializationMethods(Serializer serializer, Type type, bool withCache);

	}

	public class SerializationException : Exception {

		public SerializationException(string message) : base(message) { }

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
