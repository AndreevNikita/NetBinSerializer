using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace NetBinSerializer {

	public struct RWMethodsInfo { 
		public readonly MethodInfo readMethodInfo;
		public readonly MethodInfo writeMethodInfo;

		public RWMethodsInfo(MethodInfo readMethodInfo, MethodInfo writeMethodInfo) { 
			this.readMethodInfo = readMethodInfo;
			this.writeMethodInfo = writeMethodInfo;
		}
	}

	public class SerializeStream
	{
		public static bool ENABLE_NOT_SERIALIZABLE_WARNINGS { get; set; } = false;
		public static bool USE_SERIALIZER_FOR_DIFFICULT_TYPES { get; set; } = true;

		public long Position { get => memoryStream.Position; set => memoryStream.Position = value;}
		public long Length { get => memoryStream.Length; }
		MemoryStream memoryStream;
		public BinaryReader Reader { get; private set; }
		public BinaryWriter Writer { get; private set; }

		/*
		 *                 Work
		 */
		
		public SerializeStream() {
			memoryStream = new MemoryStream();
			Writer = new BinaryWriter(memoryStream);
			Reader = new BinaryReader(memoryStream);
		}

		public SerializeStream(byte[] bytes) {
			memoryStream = new MemoryStream(bytes);
			Writer = new BinaryWriter(memoryStream);
			Reader = new BinaryReader(memoryStream);
			
		}

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

		public static readonly RWMethodsInfo RWCollectionObjectMethodsInfo;
		public static readonly RWMethodsInfo RWCollectionMethodsInfo;
		public static readonly RWMethodsInfo RWKeyValuePairsCollectionMethodsInfo;

		private static Dictionary<Type, RWMethodsInfo> baseTypesRWMethodInfosDictionary;

		public static RWMethodsInfo GetBaseTypeRWMethods(Type type) {
			if(GetBaseTypeRWMethodsIfExists(type, out RWMethodsInfo result)) { 
				return result;
			}
			throw new ArgumentException($"Can't get RW methods for {type}");
		}

		public static bool GetBaseTypeRWMethodsIfExists(Type type, out RWMethodsInfo result) {
			return baseTypesRWMethodInfosDictionary.TryGetValue(type, out result);
		}

		static SerializeStream() {
			RWInt64MethodsInfo = new RWMethodsInfo(
				typeof(SerializeStream).GetMethod("ReadInt64", BindingFlags.Public | BindingFlags.Instance, null, new Type[0], null),
				typeof(SerializeStream).GetMethod("WriteInt64", BindingFlags.Public | BindingFlags.Instance, null, new [] { typeof(Int64) }, null)
			);

			RWInt32MethodsInfo = new RWMethodsInfo(
				typeof(SerializeStream).GetMethod("ReadInt32", BindingFlags.Public | BindingFlags.Instance, null, new Type[0], null),
				typeof(SerializeStream).GetMethod("WriteInt32", BindingFlags.Public | BindingFlags.Instance, null, new [] { typeof(Int32) }, null)
			);

			RWInt16MethodsInfo = new RWMethodsInfo(
				typeof(SerializeStream).GetMethod("ReadInt16", BindingFlags.Public | BindingFlags.Instance, null, new Type[0], null),
				typeof(SerializeStream).GetMethod("WriteInt16", BindingFlags.Public | BindingFlags.Instance, null, new [] { typeof(Int16) }, null)
			);

			RWSByteMethodsInfo = new RWMethodsInfo(
				typeof(SerializeStream).GetMethod("ReadSByte", BindingFlags.Public | BindingFlags.Instance, null, new Type[0], null),
				typeof(SerializeStream).GetMethod("WriteSByte", BindingFlags.Public | BindingFlags.Instance, null, new [] { typeof(sbyte) }, null)
			);



			RWUInt64MethodsInfo = new RWMethodsInfo(
				typeof(SerializeStream).GetMethod("ReadUInt64", BindingFlags.Public | BindingFlags.Instance, null, new Type[0], null),
				typeof(SerializeStream).GetMethod("WriteUInt64", BindingFlags.Public | BindingFlags.Instance, null, new [] { typeof(UInt64) }, null)
			);

			RWUInt32MethodsInfo = new RWMethodsInfo(
				typeof(SerializeStream).GetMethod("ReadUInt32", BindingFlags.Public | BindingFlags.Instance, null, new Type[0], null),
				typeof(SerializeStream).GetMethod("WriteUInt32", BindingFlags.Public | BindingFlags.Instance, null, new [] { typeof(UInt32) }, null)
			);

			RWUInt16MethodsInfo = new RWMethodsInfo(
				typeof(SerializeStream).GetMethod("ReadUInt16", BindingFlags.Public | BindingFlags.Instance, null, new Type[0], null),
				typeof(SerializeStream).GetMethod("WriteUInt16", BindingFlags.Public | BindingFlags.Instance, null, new [] { typeof(UInt16) }, null)
			);

			RWByteMethodsInfo = new RWMethodsInfo(
				typeof(SerializeStream).GetMethod("ReadByte", BindingFlags.Public | BindingFlags.Instance, null, new Type[0], null),
				typeof(SerializeStream).GetMethod("WriteByte", BindingFlags.Public | BindingFlags.Instance, null, new [] { typeof(byte) }, null)
			);

			RWFloatMethodsInfo = new RWMethodsInfo(
				typeof(SerializeStream).GetMethod("ReadFloat", BindingFlags.Public | BindingFlags.Instance, null, new Type[0], null),
				typeof(SerializeStream).GetMethod("WriteFloat", BindingFlags.Public | BindingFlags.Instance, null, new [] { typeof(float) }, null)
			);

			RWDoubleMethodsInfo = new RWMethodsInfo(
				typeof(SerializeStream).GetMethod("ReadDouble", BindingFlags.Public | BindingFlags.Instance, null, new Type[0], null),
				typeof(SerializeStream).GetMethod("WriteDouble", BindingFlags.Public | BindingFlags.Instance, null, new [] { typeof(double) }, null)
			);

			RWStringMethodsInfo = new RWMethodsInfo(
				typeof(SerializeStream).GetMethod("ReadString", BindingFlags.Public | BindingFlags.Instance, null, new Type[0], null),
				typeof(SerializeStream).GetMethod("WriteString", BindingFlags.Public | BindingFlags.Instance, null, new [] { typeof(string) }, null)
			);

			RWBytesMethodsInfo = new RWMethodsInfo(
				typeof(SerializeStream).GetMethod("ReadBytes", BindingFlags.Public | BindingFlags.Instance, null, new Type[0], null),
				typeof(SerializeStream).GetMethod("WriteBytes", BindingFlags.Public | BindingFlags.Instance, null, new [] { typeof(byte[]) }, null)
			);

			RWArrayMethodsInfo = new RWMethodsInfo(
				typeof(SerializeStream).GetMethod("ReadArray", BindingFlags.Public | BindingFlags.Instance, null, new [] { typeof(Type) }, null),
				typeof(SerializeStream).GetMethod("WriteArray", BindingFlags.Public | BindingFlags.Instance, null, new [] { typeof(Array) }, null)
			);


			RWCollectionObjectMethodsInfo = new RWMethodsInfo(
				typeof(SerializeStream).GetMethod("ReadCollectionObject", BindingFlags.Public | BindingFlags.Instance, null, new [] { typeof(Type) }, null),
				typeof(SerializeStream).GetMethod("WriteCollectionObject", BindingFlags.Public | BindingFlags.Instance, null, new [] { typeof(object) }, null)
			);


			RWCollectionMethodsInfo = new RWMethodsInfo(
				typeof(SerializeStream).GetMethod("ReadCollection"),
				typeof(SerializeStream).GetMethod("WriteCollection")
			);

			RWKeyValuePairsCollectionMethodsInfo = new RWMethodsInfo(
				typeof(SerializeStream).GetMethod("ReadKeyValuePairsCollection"),
				typeof(SerializeStream).GetMethod("WriteKeyValuePairsCollection")
			);

			RWSerializableMethodsInfo = new RWMethodsInfo(
				typeof(SerializeStream).GetMethod("ReadSerializable", BindingFlags.Public | BindingFlags.Instance, null, new [] { typeof(Type) }, null),
				typeof(SerializeStream).GetMethod("WriteSerializable", BindingFlags.Public | BindingFlags.Instance, null, new [] { typeof(ISerializable) }, null)
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
			};
		}


		/*--------------------------------------------------------------------------------------------------------------------------------*/
		/*--------------------------------------------------------------------------------------------------------------------------------*/
		/*--------------------------------------------------------------------------------------------------------------------------------*/

		public static bool IsCollectionType(Type type) { 
			return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ICollection<>);
		}

		public byte[] GetBytes() {
			memoryStream.Flush();
			return memoryStream.ToArray();
		}

		public long Available {
			get {
				return memoryStream.Length - memoryStream.Position;
			}
		}
		

		/*
		 *                 Write
		 */

		public void WriteInt128(UInt64 hiPart, UInt64 loPart) { 
			Write(hiPart);
			Write(loPart);
		}

		public void Write(Int64 value) { WriteInt64(value); }
		public void WriteInt64(Int64 value) { Writer.Write(value); }


		public void Write(Int32 value) { WriteInt32(value); }
		public void WriteInt32(Int32 value) { Writer.Write(value); }


		public void Write(Int16 value) { WriteInt16(value); }
		public void WriteInt16(Int16 value) { Writer.Write(value); }

		
		public void Write(sbyte value) { WriteSByte(value); }
		public void WriteSByte(sbyte value) { Writer.Write(value); }
		

		public void Write(UInt64 value) { WriteUInt64(value); }
		public void WriteUInt64(UInt64 value) { Writer.Write(value); }


		public void Write(UInt32 value) { WriteUInt32(value); }
		public void WriteUInt32(UInt32 value) { Writer.Write(value); }


		public void Write(UInt16 value) { WriteUInt16(value); }
		public void WriteUInt16(UInt16 value) { Writer.Write(value); }


		public void Write(byte value) { WriteByte(value); }
		public void WriteByte(byte value) { Writer.Write(value); }


		public void Write(Single value) { WriteFloat(value); }
		public void WriteFloat(Single value) { Writer.Write(value); }


		public void Write(Double value) { WriteDouble(value); }
		public void WriteDouble(Double value) { Writer.Write(value); }


		public void Write(byte[] bytes) { WriteBytes(bytes); }
		public void WriteBytes(byte[] bytes) {
			Write(bytes.Length);
			memoryStream.Write(bytes, 0, bytes.Length);
		}


		public void Write(string str) { WriteString(str); }
		public void WriteString(string str) {
			Writer.Write(str);
		}

		public static IEnumerable<int[]> NDArrayWalker(int[] dimensions) {
			int[] currentPos = new int[dimensions.Length];
			while(true) { 
				yield return currentPos;
					
				int changeIndex;
				for(changeIndex = currentPos.Length - 1; changeIndex > -1 && currentPos[changeIndex] == dimensions[changeIndex] - 1; changeIndex--)
					currentPos[changeIndex] = 0;

				if(changeIndex == -1)
					yield break;
				else
					currentPos[changeIndex]++;
			}
		}

		public void WriteArray(Array arr) {

			int rank = arr.Rank;
			Type elementType = arr.GetType().GetElementType();
			Write(rank);
			if(rank == 1) { 
				Write(arr.Length);
				if(elementType.IsArray) {
					foreach(Array element in arr)
						WriteArray(element);
				} else { 
					foreach(object element in arr)
						Write(element, elementType);
				}
			} else {
				int[] dimensions = new int[rank];
				
				for(int dimensionIndex = 0; dimensionIndex < rank; dimensionIndex++) {
					Write(dimensions[dimensionIndex] = arr.GetLength(dimensionIndex));
				}

				
				if(elementType.IsArray) {
					foreach(int[] currentPos in NDArrayWalker(dimensions))
						WriteArray((Array)arr.GetValue(currentPos));
				} else {
					foreach(int[] currentPos in NDArrayWalker(dimensions))
						Write(arr.GetValue(currentPos), elementType);
				}
				
			}
		}

		public void Write(ISerializable serializable) { WriteSerializable(serializable); }
		public void WriteSerializable(ISerializable serializable) {
			serializable.WriteToStream(this);
		}


		public void WriteKeyValuePairsCollection<KEY_TYPE, VALUE_TYPE>(ICollection<KeyValuePair<KEY_TYPE, VALUE_TYPE>> collection) { 
			Write(collection.Count());
			foreach(KeyValuePair<KEY_TYPE, VALUE_TYPE> pair in collection) { 
				Write(pair.Key, typeof(KEY_TYPE));
				Write(pair.Value, typeof(VALUE_TYPE));
			}
		}

		public void WriteCollection<ELEMENT_TYPE>(ICollection<ELEMENT_TYPE> collection) { 
			Write(collection.Count());
			foreach(ELEMENT_TYPE element in collection) { 
				Write(element, typeof(ELEMENT_TYPE));
			}
		}

		public void WriteCollectionObject(object collection) {
			Type elementType = collection.GetType().GetInterfaces().First((Type interfaceType) => interfaceType.IsGenericType && interfaceType.GetGenericTypeDefinition() == typeof(ICollection<>)).GetGenericArguments()[0];
			if(elementType.IsGenericType && elementType.GetGenericTypeDefinition() == typeof(KeyValuePair<,>)) {
				Type[] genericArgs = elementType.GetGenericArguments();
				RWKeyValuePairsCollectionMethodsInfo.writeMethodInfo.MakeGenericMethod(genericArgs[0], genericArgs[1]).Invoke(this, new object[] { collection });
			} else {
				RWCollectionMethodsInfo.writeMethodInfo.MakeGenericMethod(elementType).Invoke(this, new object[] { collection });
			}
		}


		public void WriteObject(object obj) {
			if(ENABLE_NOT_SERIALIZABLE_WARNINGS) {
				Console.WriteLine("Warning! {0} is not serializable", obj.GetType().Name);
			}
			BinaryFormatter bf = new BinaryFormatter();
			bf.Serialize(memoryStream, obj);
		}

		public void WriteUnknown(object value) { 
			Write(value, value.GetType());
		}

		public void Write(object value, Type type) {
			if(type == typeof(Int64)) {
				Write((Int64)value);
			} 
			else if(type == typeof(Int32)) {
				Write((Int32)value);
			} 
			else if(type == typeof(Int16)) {
				Write((Int16)value);
			} 
			else if(type == typeof(sbyte)) { 
				Write((sbyte)value);
			}
			else if(type == typeof(UInt64)) {
				Write((UInt64)value);
			} 
			else if(type == typeof(UInt32)) {
				Write((UInt32)value);
			} 
			else if(type == typeof(UInt16)) {
				Write((UInt16)value);
			} 
			else if(type == typeof(byte)) {
				Write((byte)value);
			} 
			else if(type == typeof(float)) {
				Write((float)value);
			} 
			else if(type == typeof(double)) {
				Write((double)value);
			} 
			else if(type == typeof(string)) {
				Write((string)value);
			}
			else if(type == typeof(byte[])) {
				Write((byte[])value);
			} 

			//Special types
			else if(typeof(SerializeStream).IsAssignableFrom(type)) {
				Write(((SerializeStream)value).GetBytes());
			} else if(typeof(ISerializable).IsAssignableFrom(type)) {
				Write((ISerializable)value);
			} 
			//Difficult types
			else if(typeof(Array).IsAssignableFrom(type)) {
				if(USE_SERIALIZER_FOR_DIFFICULT_TYPES)
					Serializer.Serialize(this, value, type);
				else
					WriteArray((Array)value);
			} else if(type.GetInterfaces().Any((Type t) => IsCollectionType(t))) {
				if(USE_SERIALIZER_FOR_DIFFICULT_TYPES)
					Serializer.Serialize(this, value, type);
				else
					WriteCollectionObject(value);
			} else if(Serializer.SerializeSafe(this, value, type)) {

			} else {
				throw new SerializeStreamException($"Can't write type {type}");
			}
		} 
		

		/*
		 *                 Read
		 */

		public void ReadInt128(out UInt64 hiPart, out UInt64 loPart) { 
			hiPart = Read<UInt64>();
			loPart = Read<UInt64>();
		}

		public Int16 ReadInt16() { return Reader.ReadInt16(); }

		public Int32 ReadInt32() { return Reader.ReadInt32(); }

		public Int64 ReadInt64() { return Reader.ReadInt64(); }

		public sbyte ReadSByte() { return Reader.ReadSByte(); }

		public UInt64 ReadUInt64() { return Reader.ReadUInt64(); }

		public UInt32 ReadUInt32() { return Reader.ReadUInt32(); }

		public UInt16 ReadUInt16() { return Reader.ReadUInt16(); }

		public byte ReadByte() { return Reader.ReadByte(); }

		public Single ReadFloat() { return Reader.ReadSingle(); }

		public Double ReadDouble() { return Reader.ReadDouble(); }

		public byte[] ReadBytes() {
			int length = ReadInt32();
			byte[] buffer = new byte[length];
			memoryStream.Read(buffer, 0, length);
			return buffer;
		}

		public string ReadString() {
			return Reader.ReadString();
		}

		public ARRAY_TYPE ReadArray<ARRAY_TYPE>() {
			return (ARRAY_TYPE)(object)ReadArray(typeof(ARRAY_TYPE));
		}

		
		
		public COLLECTION_TYPE ReadCollection<COLLECTION_TYPE, ELEMENT_TYPE>() where COLLECTION_TYPE : ICollection<ELEMENT_TYPE>, new() { 
			COLLECTION_TYPE result = new COLLECTION_TYPE();
			int length = ReadInt32();
			for(int index = 0; index < length; index++)
				result.Add(Read<ELEMENT_TYPE>());
			return result;
		}

		public COLLECTION_TYPE ReadKeyValuePairsCollection<COLLECTION_TYPE, KEY_TYPE, VALUE_TYPE>() where COLLECTION_TYPE : ICollection<KeyValuePair<KEY_TYPE, VALUE_TYPE>>, new() { 
			COLLECTION_TYPE result = new COLLECTION_TYPE();
			int length = ReadInt32();
			for(int index = 0; index < length; index++) {
				KEY_TYPE key = Read<KEY_TYPE>();
				VALUE_TYPE value = Read<VALUE_TYPE>();
				result.Add(new KeyValuePair<KEY_TYPE, VALUE_TYPE>(key, value));
			}
			return result;
		}

		public object ReadCollectionObject(Type collectionType) { 
			Type elementType = collectionType.GetInterfaces().First((Type interfaceType) => interfaceType.IsGenericType && interfaceType.GetGenericTypeDefinition() == typeof(ICollection<>)).GetGenericArguments()[0];
			if(elementType.IsGenericType && elementType.GetGenericTypeDefinition() == typeof(KeyValuePair<,>)) {
				Type[] genericArgs = elementType.GetGenericArguments();
				return RWKeyValuePairsCollectionMethodsInfo.readMethodInfo.MakeGenericMethod(collectionType, genericArgs[0], genericArgs[1]).Invoke(this, new object[0]);
			} else { 
				return RWCollectionMethodsInfo.readMethodInfo.MakeGenericMethod(collectionType, elementType).Invoke(this, new object[0]);
			}
		}

		public Array ReadArray(Type arrayType) { 
			int rank = ReadInt32();
			Type elementType = arrayType.GetElementType();
			if(rank == 1) { 
				int length = ReadInt32();
				Array result = Array.CreateInstance(elementType, length);

				if(elementType.IsArray) { 
					for(int index = 0; index < length; index++)
						result.SetValue(ReadArray(elementType), index);
				} else { 
					for(int index = 0; index < length; index++)
						result.SetValue(Read(elementType), index);
				}
				return result;
			} else { 
				int[] dimensions = new int[rank];
				for(int dimensionIndex = 0; dimensionIndex < rank; dimensionIndex++) { 
					dimensions[dimensionIndex] = ReadInt32();
				}

				Array result = Array.CreateInstance(elementType, dimensions);
				if(elementType.IsArray) { 
					foreach(int[] currentPos in NDArrayWalker(dimensions))
						result.SetValue(ReadArray(elementType), currentPos);
				} else { 
					foreach(int[] currentPos in NDArrayWalker(dimensions))
						result.SetValue(Read(elementType), currentPos);
				}

				return result;
			}
		}

		public object ReadObject() {
			BinaryFormatter bf = new BinaryFormatter();
			return bf.Deserialize(memoryStream);
		}

		public SERIALIZABLE_TYPE ReadSerializable<SERIALIZABLE_TYPE>() where SERIALIZABLE_TYPE : ISerializable { 
			return (SERIALIZABLE_TYPE)ReadSerializable(typeof(SERIALIZABLE_TYPE));
		}

		public ISerializable ReadSerializable(Type t) { 
			ISerializable result = (ISerializable)FormatterServices.GetUninitializedObject(t);
			result.ReadFromStream(this);
			return result;
		}

		public void Read<T>(out T result) {
			result = (T)Read(typeof(T));
		}

		public T Read<T>() {
			return (T)Read(typeof(T));
		}

		public object Read(Type type) {
			if(type == typeof(Int64)) {
				return ReadInt64();
			} 
			else if(type == typeof(Int32)) {
				return ReadInt32();
			} 
			else if(type == typeof(Int16)) {
				return ReadInt16();
			} 
			else if(type == typeof(sbyte)) {
				return ReadSByte();
			} 
			else if(type == typeof(UInt64)) {
				return ReadUInt64();
			} 
			else if(type == typeof(UInt32)) {
				return ReadUInt32();
			} 
			else if(type == typeof(UInt16)) {
				return ReadUInt16();
			} 
			else if(type == typeof(byte)) {
				return ReadByte();
			} 
			else if(type == typeof(float)) {
				return ReadFloat();
			} 
			else if(type == typeof(double)) {
				return ReadDouble();
			} 
			else if(type == typeof(string)) {
				return ReadString();
			}
			else if(type == typeof(byte[])) {
				return ReadBytes();
			}

			//Special types
			else if(type == typeof(SerializeStream)) {
				return new SerializeStream(ReadBytes());
			}
			else if(typeof(ISerializable).IsAssignableFrom(type)) { 
				return ReadSerializable(type);
			}

			//Difficult types
			else if(type.IsArray) {
				if(USE_SERIALIZER_FOR_DIFFICULT_TYPES)
					return Serializer.Deserialize(this, type);
				else
					return ReadArray(type);
			} else if(type.GetInterfaces().Any((Type t) => IsCollectionType(t))) {
				if(USE_SERIALIZER_FOR_DIFFICULT_TYPES)
					return Serializer.Deserialize(this, type);
				else
					return ReadCollectionObject(type);
			} else if(Serializer.DeserializeSafe(this, out object result, type)) {
				return result;
			} else {
				throw new SerializeStreamException($"Can't read type {type}");
			}
		}



		//Backwrite sugar
		/*
		 * Write next length (exclude 4 bytes per int) in bytes to position
		 */
		public void WriteNextLength(long pos) {
			Position = pos;
			Write((UInt32)(Length - pos - sizeof(int)));
			Position = Length;
		}

		public long RememberAndSeek(int bytes) {
			long nowPos = Position;
			Position += bytes;
			return nowPos;
		}

	}

	

	public interface ISerializable {
		void WriteToStream(SerializeStream stream);

		void ReadFromStream(SerializeStream stream);
	}

	public class SerializeStreamException : Exception { 

		public SerializeStreamException(string str, params object[] args) : base(string.Format(str, args)) { }

	}
}
