using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace NetBinSerializer {
	
	public struct RWMethodsInfo { 
		public readonly MethodInfo readMethodInfo;
		public readonly MethodInfo writeMethodInfo;

		public RWMethodsInfo(MethodInfo readMethodInfo, MethodInfo writeMethodInfo) { 
			this.readMethodInfo = readMethodInfo;
			this.writeMethodInfo = writeMethodInfo;
		}
	}

	public interface IRWMethods {
		ReadMethod ReadMethod { get; }
		WriteMethod WriteMethod { get; }
	}

	public struct RWMethods : IRWMethods { 
		public ReadMethod ReadMethod { get; }
		public WriteMethod WriteMethod { get; }

		public RWMethods(ReadMethod readMethod, WriteMethod writeMethod) { 
			this.ReadMethod = readMethod;
			this.WriteMethod = writeMethod;
		}
	
	}

	public struct RWMethodsWithTypeOnR : IRWMethods { 
		public ReadMethod ReadMethod => ReadMethodShell;
		private ReadMethodWithType ReadMethodSource;
		public WriteMethod WriteMethod { get; }

		public readonly Type type;

		public RWMethodsWithTypeOnR(ReadMethodWithType readMethod, WriteMethod writeMethod, Type type) { 
			this.ReadMethodSource = readMethod;
			this.WriteMethod = writeMethod;
			this.type = type;
		}

		private object ReadMethodShell() => ReadMethodSource(type);
	
	}

	public struct RWMethodsWithTypeOnRW : IRWMethods { 
		public ReadMethod ReadMethod => ReadMethodShell;
		private ReadMethodWithType ReadMethodSource;
		public WriteMethod WriteMethod => WriteMethodShell;

		private WriteMethodWithType WriteMethodSource;

		public readonly Type type;

		public RWMethodsWithTypeOnRW(ReadMethodWithType readMethod, WriteMethodWithType writeMethod, Type type) { 
			this.ReadMethodSource = readMethod;
			this.WriteMethodSource = writeMethod;
			this.type = type;
		}

		private object ReadMethodShell() => ReadMethodSource(type);

		private void WriteMethodShell(object obj) => WriteMethodSource(obj, type);
	
	}

	public class KeyValuePairRWMethods : IRWMethods { 
		public ReadMethod ReadMethod { get; }
		public WriteMethod WriteMethod { get; }

		private readonly IRWMethods keyRWMethods;
		private readonly IRWMethods valueRWMethods;

		private readonly Delegate readKeyValuePairDelegate;
		private readonly Delegate writeKeyValuePairDelegate;

		public KeyValuePairRWMethods(Type keyType, Type valueType, IRWMethods keyRWMethods, IRWMethods valueRWMethods) { 
			this.keyRWMethods = keyRWMethods;
			this.valueRWMethods = valueRWMethods;
			var methodInfo = typeof(KeyValuePairRWMethods).GetMethod(nameof(ReadKeyValuePairObject), BindingFlags.NonPublic | BindingFlags.Instance);
			var genericMethod = methodInfo.MakeGenericMethod(keyType, valueType);
			readKeyValuePairDelegate = typeof(KeyValuePairRWMethods).GetMethod(nameof(ReadKeyValuePairObject), BindingFlags.NonPublic | BindingFlags.Instance).MakeGenericMethod(keyType, valueType).CreateDelegate(typeof(Func<object>), this);
			writeKeyValuePairDelegate = typeof(KeyValuePairRWMethods).GetMethod(nameof(WriteKeyValuePairObject), BindingFlags.NonPublic | BindingFlags.Instance).MakeGenericMethod(keyType, valueType).CreateDelegate(typeof(Action<object>), this);
			this.ReadMethod = ReadKeyValueObjectDelegateCall;
			this.WriteMethod = WriteKeyValueObjectDelegateCall;
			

		}
		
		private object ReadKeyValueObjectDelegateCall() => readKeyValuePairDelegate.DynamicInvoke();

		private object ReadKeyValuePairObject<TKey, TValue>() {
			TKey key = (TKey)keyRWMethods.ReadMethod();
			TValue value = (TValue)valueRWMethods.ReadMethod();
			return new KeyValuePair<TKey, TValue>(key, value);
		}

		private void WriteKeyValueObjectDelegateCall(object obj) => writeKeyValuePairDelegate.DynamicInvoke(obj);

		private void WriteKeyValuePairObject<TKey, TValue>(object keyValuePairObject) {
			KeyValuePair<TKey, TValue> keyValuePair = (KeyValuePair<TKey, TValue>)keyValuePairObject;
			keyRWMethods.WriteMethod(keyValuePair.Key);
			valueRWMethods.WriteMethod(keyValuePair.Value);
		}
		
	}

	public delegate object ReadMethod();
	public delegate object ReadMethodWithType(Type type);
	public delegate void WriteMethodWithType(object obj, Type type);
	public delegate void WriteMethod(object obj);

	public delegate IRWMethods RWMethodsGetter(SerializeStream stream);
	public delegate IRWMethods RWMethodsWithType<in TStream>(TStream stream, Type type) where TStream : SerializeStream;

	public partial class SerializeStream {

		/*
		 * --------------------------------------------MethodInfos for reflection usage------------------------------------------------
		 */

		public static readonly RWMethodsInfo RWInt64MethodsInfo;
		public static readonly RWMethodsInfo RWInt32MethodsInfo;
		public static readonly RWMethodsInfo RWInt16MethodsInfo;
		public static readonly RWMethodsInfo RWSByteMethodsInfo;
		public static readonly RWMethodsInfo RWUInt64MethodsInfo;
		public static readonly RWMethodsInfo RWUInt32MethodsInfo;
		public static readonly RWMethodsInfo RWUInt16MethodsInfo;
		public static readonly RWMethodsInfo RWByteMethodsInfo;

		public static readonly RWMethodsInfo RWFloatMethodsInfo;
		public static readonly RWMethodsInfo RWDoubleMethodsInfo;

		public static readonly RWMethodsInfo RWStringMethodsInfo;
		public static readonly RWMethodsInfo RWBytesMethodsInfo;
		public static readonly RWMethodsInfo RWArrayMethodsInfo;

		public static readonly RWMethodsInfo RWSerializableMethodsInfo;

		public static readonly RWMethodsInfo RWCollectionMethodsInfo;

		private static Dictionary<Type, RWMethodsInfo> baseTypesRWMethodInfosDictionary;

		public static RWMethodsInfo GetBaseTypeRWMethodsInfos(Type type) {
			if(TryGetBaseTypeRWMethodsInfos(type, out RWMethodsInfo result)) { 
				return result;
			}
			throw new ArgumentException($"Can't get RW methods for {type}");
		}

		public static bool TryGetBaseTypeRWMethodsInfos(Type type, out RWMethodsInfo result) {
			return baseTypesRWMethodInfosDictionary.TryGetValue(type, out result);
		}

		/*
		 * --------------------------------------------Delegates------------------------------------------------
		 */

		public static readonly RWMethodsGetter RWInt64MethodsGetter =	(s) => new RWMethods(s.ReadInt64Object, s.WriteInt64Object);
		public static readonly RWMethodsGetter RWInt32MethodsGetter =	(s) => new RWMethods(s.ReadInt32Object, s.WriteInt32Object);
		public static readonly RWMethodsGetter RWInt16MethodsGetter =	(s) => new RWMethods(s.ReadInt16Object, s.WriteInt16Object);
		public static readonly RWMethodsGetter RWSByteMethodsGetter =	(s) => new RWMethods(s.ReadSByteObject, s.WriteSByteObject);
		public static readonly RWMethodsGetter RWUInt64MethodsGetter =	(s) => new RWMethods(s.ReadUInt64Object, s.WriteUInt64Object);
		public static readonly RWMethodsGetter RWUInt32MethodsGetter =	(s) => new RWMethods(s.ReadUInt32Object, s.WriteUInt32Object);
		public static readonly RWMethodsGetter RWUInt16MethodsGetter =	(s) => new RWMethods(s.ReadUInt16Object, s.WriteUInt16Object);
		public static readonly RWMethodsGetter RWByteMethodsGetter =	(s) => new RWMethods(s.ReadByteObject, s.WriteByteObject);

		public static readonly RWMethodsGetter RWFloatMethodsGetter =	(s) => new RWMethods(s.ReadFloatObject, s.WriteFloatObject);
		public static readonly RWMethodsGetter RWDoubleMethodsGetter =	(s) => new RWMethods(s.ReadDoubleObject, s.WriteDoubleObject);

		public static readonly RWMethodsGetter RWStringMethodsGetter =	(s) => new RWMethods(s.ReadStringObject, s.WriteStringObject);
		public static readonly RWMethodsGetter RWBytesMethodsGetter =	(s) => new RWMethods(s.ReadBytesObject, s.WriteBytesObject);

		private static Dictionary<Type, RWMethodsGetter> baseTypesRWMethodsGettersDictionary = new Dictionary<Type, RWMethodsGetter> { 
			{ typeof(Int64),	RWInt64MethodsGetter },
			{ typeof(Int32),	RWInt32MethodsGetter },
			{ typeof(Int16),	RWInt16MethodsGetter },
			{ typeof(SByte),	RWSByteMethodsGetter },

			{ typeof(UInt64),	RWUInt64MethodsGetter },
			{ typeof(UInt32),	RWUInt32MethodsGetter },
			{ typeof(UInt16),	RWUInt16MethodsGetter },
			{ typeof(Byte),		RWByteMethodsGetter },

			{ typeof(float),	RWFloatMethodsGetter },
			{ typeof(double),	RWDoubleMethodsGetter },

			{ typeof(string),	RWStringMethodsGetter },
			{ typeof(byte[]),	RWBytesMethodsGetter  },
		};

		public static RWMethodsGetter GetBaseTypeRWMethodsGetter(Type type) {
			if(TryGetBaseTypeRWMethodsGetter(type, out RWMethodsGetter result)) { 
				return result;
			}
			throw new ArgumentException($"Can't get RW methods for {type}");
		}

		public static bool TryGetBaseTypeRWMethodsGetter(Type type, out RWMethodsGetter result) {
			return baseTypesRWMethodsGettersDictionary.TryGetValue(type, out result);
		} 

		public IRWMethods GetBaseTypeRWMethods(Type type) => GetBaseTypeRWMethodsGetter(type)(this);

		public bool TryGetBaseTypeRWMethods(Type type, out IRWMethods result) {
			if(baseTypesRWMethodsGettersDictionary.TryGetValue(type, out RWMethodsGetter methodsGetter)) {
				result = methodsGetter(this);
				return true;
			}
			result = default;
			return false;
		} 


		protected bool TryGetRWMethodsForKeyValuePair(Type type, out IRWMethods result) {
			Type[] genericArgs = type.GetGenericArguments();
			Type keyType = genericArgs[0];
			IRWMethods keyRWMethods = GetRWMethods(keyType);
			if(keyRWMethods == null) { 
				result = default;
				return false;
			}

			Type valueType = genericArgs[1];
			IRWMethods valueRWMethods = GetRWMethods(valueType);
			if(valueRWMethods == null) { 
				result = default;
				return false;
			}

			result = new KeyValuePairRWMethods(keyType, valueType, keyRWMethods, valueRWMethods);
			return true;
		}

		public virtual IRWMethods GetRWMethods(Type type) {
			IRWMethods rwMethodsBuffer;
			if(TryGetBaseTypeRWMethods(type, out rwMethodsBuffer)) {
				return rwMethodsBuffer;
			} else if(typeof(Array).IsAssignableFrom(type)) {
				return new RWMethodsWithTypeOnR(this.ReadArrayObject, this.WriteArrayObject, type);
			} else if(typeof(ISerializable).IsAssignableFrom(type)) {
				return new RWMethodsWithTypeOnR(this.ReadSerializableObject, this.WriteSerializableObject, type);
			} else if(IsKeyValuePairType(type)) {
				return TryGetRWMethodsForKeyValuePair(type, out var result) ? result : null;
			} else if(type.GetInterfaces().Any((Type t) => IsCollectionType(t))) {
				return new RWMethodsWithTypeOnR(this.ReadCollectionObject, this.WriteCollectionObject, type);
			} else {
				return null;
			}
		}

		static SerializeStream() {
			RWInt64MethodsInfo = new RWMethodsInfo(
				typeof(SerializeStream).GetMethod(nameof(ReadInt64), BindingFlags.Public | BindingFlags.Instance, null, new Type[0], null),
				typeof(SerializeStream).GetMethod(nameof(WriteInt64), BindingFlags.Public | BindingFlags.Instance, null, new [] { typeof(Int64) }, null)
			);

			RWInt32MethodsInfo = new RWMethodsInfo(
				typeof(SerializeStream).GetMethod(nameof(ReadInt32), BindingFlags.Public | BindingFlags.Instance, null, new Type[0], null),
				typeof(SerializeStream).GetMethod(nameof(WriteInt32), BindingFlags.Public | BindingFlags.Instance, null, new [] { typeof(Int32) }, null)
			);

			RWInt16MethodsInfo = new RWMethodsInfo(
				typeof(SerializeStream).GetMethod(nameof(ReadInt16), BindingFlags.Public | BindingFlags.Instance, null, new Type[0], null),
				typeof(SerializeStream).GetMethod(nameof(WriteInt16), BindingFlags.Public | BindingFlags.Instance, null, new [] { typeof(Int16) }, null)
			);

			RWSByteMethodsInfo = new RWMethodsInfo(
				typeof(SerializeStream).GetMethod(nameof(ReadSByte), BindingFlags.Public | BindingFlags.Instance, null, new Type[0], null),
				typeof(SerializeStream).GetMethod(nameof(WriteSByte), BindingFlags.Public | BindingFlags.Instance, null, new [] { typeof(sbyte) }, null)
			);



			RWUInt64MethodsInfo = new RWMethodsInfo(
				typeof(SerializeStream).GetMethod(nameof(ReadUInt64), BindingFlags.Public | BindingFlags.Instance, null, new Type[0], null),
				typeof(SerializeStream).GetMethod(nameof(WriteUInt64), BindingFlags.Public | BindingFlags.Instance, null, new [] { typeof(UInt64) }, null)
			);

			RWUInt32MethodsInfo = new RWMethodsInfo(
				typeof(SerializeStream).GetMethod(nameof(ReadUInt32), BindingFlags.Public | BindingFlags.Instance, null, new Type[0], null),
				typeof(SerializeStream).GetMethod(nameof(WriteUInt32), BindingFlags.Public | BindingFlags.Instance, null, new [] { typeof(UInt32) }, null)
			);

			RWUInt16MethodsInfo = new RWMethodsInfo(
				typeof(SerializeStream).GetMethod(nameof(ReadUInt16), BindingFlags.Public | BindingFlags.Instance, null, new Type[0], null),
				typeof(SerializeStream).GetMethod(nameof(WriteUInt16), BindingFlags.Public | BindingFlags.Instance, null, new [] { typeof(UInt16) }, null)
			);

			RWByteMethodsInfo = new RWMethodsInfo(
				typeof(SerializeStream).GetMethod(nameof(ReadByte), BindingFlags.Public | BindingFlags.Instance, null, new Type[0], null),
				typeof(SerializeStream).GetMethod(nameof(WriteByte), BindingFlags.Public | BindingFlags.Instance, null, new [] { typeof(byte) }, null)
			);

			RWFloatMethodsInfo = new RWMethodsInfo(
				typeof(SerializeStream).GetMethod(nameof(ReadFloat), BindingFlags.Public | BindingFlags.Instance, null, new Type[0], null),
				typeof(SerializeStream).GetMethod(nameof(WriteFloat), BindingFlags.Public | BindingFlags.Instance, null, new [] { typeof(float) }, null)
			);

			RWDoubleMethodsInfo = new RWMethodsInfo(
				typeof(SerializeStream).GetMethod(nameof(ReadDouble), BindingFlags.Public | BindingFlags.Instance, null, new Type[0], null),
				typeof(SerializeStream).GetMethod(nameof(WriteDouble), BindingFlags.Public | BindingFlags.Instance, null, new [] { typeof(double) }, null)
			);

			RWStringMethodsInfo = new RWMethodsInfo(
				typeof(SerializeStream).GetMethod(nameof(ReadString), BindingFlags.Public | BindingFlags.Instance, null, new Type[0], null),
				typeof(SerializeStream).GetMethod(nameof(WriteString), BindingFlags.Public | BindingFlags.Instance, null, new [] { typeof(string) }, null)
			);

			RWBytesMethodsInfo = new RWMethodsInfo(
				typeof(SerializeStream).GetMethod(nameof(ReadBytes), BindingFlags.Public | BindingFlags.Instance, null, new Type[0], null),
				typeof(SerializeStream).GetMethod(nameof(WriteBytes), BindingFlags.Public | BindingFlags.Instance, null, new [] { typeof(byte[]) }, null)
			);

			RWArrayMethodsInfo = new RWMethodsInfo(
				typeof(SerializeStream).GetMethod(nameof(ReadArray), BindingFlags.Public | BindingFlags.Instance, null, new [] { typeof(Type) }, null),
				typeof(SerializeStream).GetMethod(nameof(WriteArray), BindingFlags.Public | BindingFlags.Instance, null, new [] { typeof(Array) }, null)
			);


			RWCollectionMethodsInfo = new RWMethodsInfo(
				typeof(SerializeStream).GetMethod(nameof(ReadCollection), BindingFlags.Public | BindingFlags.Instance),
				typeof(SerializeStream).GetMethod(nameof(WriteCollection), BindingFlags.Public | BindingFlags.Instance)
			);


			RWSerializableMethodsInfo = new RWMethodsInfo(
				typeof(SerializeStream).GetMethod(nameof(ReadSerializable), BindingFlags.Public | BindingFlags.Instance, null, new [] { typeof(Type) }, null),
				typeof(SerializeStream).GetMethod(nameof(WriteSerializable), BindingFlags.Public | BindingFlags.Instance, null, new [] { typeof(ISerializable) }, null)
			);

			baseTypesRWMethodInfosDictionary = new Dictionary<Type, RWMethodsInfo>() { 
				{ typeof(Int64),	RWInt64MethodsInfo },
				{ typeof(Int32),	RWInt32MethodsInfo },
				{ typeof(Int16),	RWInt16MethodsInfo },
				{ typeof(SByte),	RWSByteMethodsInfo },

				{ typeof(UInt64),	RWUInt64MethodsInfo },
				{ typeof(UInt32),	RWUInt32MethodsInfo },
				{ typeof(UInt16),	RWUInt16MethodsInfo },
				{ typeof(Byte),		RWByteMethodsInfo },

				{ typeof(float),	RWFloatMethodsInfo },
				{ typeof(double),	RWDoubleMethodsInfo },

				{ typeof(string),	RWStringMethodsInfo },
				{ typeof(byte[]),	RWBytesMethodsInfo },
			};
		}
	}
}
