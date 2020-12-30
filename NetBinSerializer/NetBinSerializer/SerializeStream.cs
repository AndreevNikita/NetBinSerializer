using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace NetBinSerializer
{

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

		public static readonly RWMethodsInfo rwInt64MethodsInfo;
		public static readonly RWMethodsInfo rwInt32MethodsInfo;
		public static readonly RWMethodsInfo rwInt16MethodsInfo;
		public static readonly RWMethodsInfo rwSByteMethodsInfo;
		public static readonly RWMethodsInfo rwUInt64MethodsInfo;
		public static readonly RWMethodsInfo rwUInt32MethodsInfo;
		public static readonly RWMethodsInfo rwUInt16MethodsInfo;
		public static readonly RWMethodsInfo rwByteMethodsInfo;

		public static readonly RWMethodsInfo rwFloatMethodsInfo;
		public static readonly RWMethodsInfo rwDoubleMethodsInfo;

		public static readonly RWMethodsInfo rwStringMethodsInfo;
		public static readonly RWMethodsInfo rwBytesMethodsInfo;
		public static readonly RWMethodsInfo rwArrayMethodsInfo;

		public static readonly RWMethodsInfo rwSerializableMethodsInfo;

		public static readonly RWMethodsInfo rwCollectionObjectMethodsInfo;
		public static readonly RWMethodsInfo rwCollectionMethodsInfo;
		public static readonly RWMethodsInfo rwKeyValuePairsCollectionMethodsInfo;

		private static Dictionary<Type, RWMethodsInfo> baseTypesRWMethodInfosDictionary;

		public static RWMethodsInfo getBaseTypeRWMethods(Type type) {
			if(getBaseTypeRWMethodsIfExists(type, out RWMethodsInfo result)) { 
				return result;
			}
			throw new ArgumentException($"Can't get RW methods for {type}");
		}

		public static bool getBaseTypeRWMethodsIfExists(Type type, out RWMethodsInfo result) {
			return baseTypesRWMethodInfosDictionary.TryGetValue(type, out result);
		}

		static SerializeStream() {
			rwInt64MethodsInfo = new RWMethodsInfo(
				typeof(SerializeStream).GetMethod("readInt64", BindingFlags.Public | BindingFlags.Instance, null, new Type[0], null),
				typeof(SerializeStream).GetMethod("write", BindingFlags.Public | BindingFlags.Instance, null, new [] { typeof(Int64) }, null)
			);

			rwInt32MethodsInfo = new RWMethodsInfo(
				typeof(SerializeStream).GetMethod("readInt32", BindingFlags.Public | BindingFlags.Instance, null, new Type[0], null),
				typeof(SerializeStream).GetMethod("write", BindingFlags.Public | BindingFlags.Instance, null, new [] { typeof(Int32) }, null)
			);

			rwInt16MethodsInfo = new RWMethodsInfo(
				typeof(SerializeStream).GetMethod("readInt16", BindingFlags.Public | BindingFlags.Instance, null, new Type[0], null),
				typeof(SerializeStream).GetMethod("write", BindingFlags.Public | BindingFlags.Instance, null, new [] { typeof(Int16) }, null)
			);

			rwSByteMethodsInfo = new RWMethodsInfo(
				typeof(SerializeStream).GetMethod("readSByte", BindingFlags.Public | BindingFlags.Instance, null, new Type[0], null),
				typeof(SerializeStream).GetMethod("write", BindingFlags.Public | BindingFlags.Instance, null, new [] { typeof(sbyte) }, null)
			);



			rwUInt64MethodsInfo = new RWMethodsInfo(
				typeof(SerializeStream).GetMethod("readUInt64", BindingFlags.Public | BindingFlags.Instance, null, new Type[0], null),
				typeof(SerializeStream).GetMethod("write", BindingFlags.Public | BindingFlags.Instance, null, new [] { typeof(UInt64) }, null)
			);

			rwUInt32MethodsInfo = new RWMethodsInfo(
				typeof(SerializeStream).GetMethod("readUInt32", BindingFlags.Public | BindingFlags.Instance, null, new Type[0], null),
				typeof(SerializeStream).GetMethod("write", BindingFlags.Public | BindingFlags.Instance, null, new [] { typeof(UInt32) }, null)
			);

			rwUInt16MethodsInfo = new RWMethodsInfo(
				typeof(SerializeStream).GetMethod("readUInt16", BindingFlags.Public | BindingFlags.Instance, null, new Type[0], null),
				typeof(SerializeStream).GetMethod("write", BindingFlags.Public | BindingFlags.Instance, null, new [] { typeof(UInt16) }, null)
			);

			rwByteMethodsInfo = new RWMethodsInfo(
				typeof(SerializeStream).GetMethod("readByte", BindingFlags.Public | BindingFlags.Instance, null, new Type[0], null),
				typeof(SerializeStream).GetMethod("write", BindingFlags.Public | BindingFlags.Instance, null, new [] { typeof(byte) }, null)
			);

			rwFloatMethodsInfo = new RWMethodsInfo(
				typeof(SerializeStream).GetMethod("readFloat", BindingFlags.Public | BindingFlags.Instance, null, new Type[0], null),
				typeof(SerializeStream).GetMethod("write", BindingFlags.Public | BindingFlags.Instance, null, new [] { typeof(float) }, null)
			);

			rwDoubleMethodsInfo = new RWMethodsInfo(
				typeof(SerializeStream).GetMethod("readDouble", BindingFlags.Public | BindingFlags.Instance, null, new Type[0], null),
				typeof(SerializeStream).GetMethod("write", BindingFlags.Public | BindingFlags.Instance, null, new [] { typeof(double) }, null)
			);

			rwStringMethodsInfo = new RWMethodsInfo(
				typeof(SerializeStream).GetMethod("readString", BindingFlags.Public | BindingFlags.Instance, null, new Type[0], null),
				typeof(SerializeStream).GetMethod("write", BindingFlags.Public | BindingFlags.Instance, null, new [] { typeof(string) }, null)
			);

			rwBytesMethodsInfo = new RWMethodsInfo(
				typeof(SerializeStream).GetMethod("readBytes", BindingFlags.Public | BindingFlags.Instance, null, new Type[0], null),
				typeof(SerializeStream).GetMethod("write", BindingFlags.Public | BindingFlags.Instance, null, new [] { typeof(byte[]) }, null)
			);

			rwArrayMethodsInfo = new RWMethodsInfo(
				typeof(SerializeStream).GetMethod("readArray", BindingFlags.Public | BindingFlags.Instance, null, new [] { typeof(Type) }, null),
				typeof(SerializeStream).GetMethod("writeArray", BindingFlags.Public | BindingFlags.Instance, null, new [] { typeof(Array) }, null)
			);


			rwCollectionObjectMethodsInfo = new RWMethodsInfo(
				typeof(SerializeStream).GetMethod("readCollectionObject", BindingFlags.Public | BindingFlags.Instance, null, new [] { typeof(Type) }, null),
				typeof(SerializeStream).GetMethod("writeCollectionObject", BindingFlags.Public | BindingFlags.Instance, null, new [] { typeof(object) }, null)
			);


			rwCollectionMethodsInfo = new RWMethodsInfo(
				typeof(SerializeStream).GetMethod("readCollection"),
				typeof(SerializeStream).GetMethod("writeCollection")
			);

			rwKeyValuePairsCollectionMethodsInfo = new RWMethodsInfo(
				typeof(SerializeStream).GetMethod("readKeyValuePairsCollection"),
				typeof(SerializeStream).GetMethod("writeKeyValuePairsCollection")
			);

			rwSerializableMethodsInfo = new RWMethodsInfo(
				typeof(SerializeStream).GetMethod("write", BindingFlags.Public | BindingFlags.Instance, null, new [] { typeof(Serializable) }, null), 
				typeof(SerializeStream).GetMethod("readSerializable", BindingFlags.Public | BindingFlags.Instance, null, new [] { typeof(Type) }, null)
			);

			baseTypesRWMethodInfosDictionary = new Dictionary<Type, RWMethodsInfo>() { 
				{ typeof(Int64),	rwInt64MethodsInfo },
				{ typeof(Int32),	rwInt32MethodsInfo },
				{ typeof(Int16),	rwInt16MethodsInfo },
				{ typeof(SByte),	rwSByteMethodsInfo },

				{ typeof(UInt64),	rwUInt64MethodsInfo },
				{ typeof(UInt32),	rwUInt32MethodsInfo },
				{ typeof(UInt16),	rwUInt16MethodsInfo },
				{ typeof(Byte),		rwByteMethodsInfo },

				{ typeof(float),	rwFloatMethodsInfo },
				{ typeof(double),	rwDoubleMethodsInfo },

				{ typeof(string),	rwStringMethodsInfo },
			};
		}


		/*--------------------------------------------------------------------------------------------------------------------------------*/
		/*--------------------------------------------------------------------------------------------------------------------------------*/
		/*--------------------------------------------------------------------------------------------------------------------------------*/

		public static bool isCollectionType(Type type) { 
			return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ICollection<>);
		}

		public byte[] getBytes() {
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

		public void writeInt128(UInt64 hiPart, UInt64 loPart) { 
			write(hiPart);
			write(loPart);
		}

		public void write(Int64 value) { Writer.Write(value); }

		public void write(Int32 value) { Writer.Write(value); }

		public void write(Int16 value) { Writer.Write(value); }
		
		public void write(sbyte value) { Writer.Write(value); }


		public void write(UInt64 value) { Writer.Write(value); }

		public void write(UInt32 value) { Writer.Write(value); }

		public void write(UInt16 value) { Writer.Write(value); }

		public void write(byte value) { Writer.Write(value); }



		public void write(Single value) { Writer.Write(value); }

		public void write(Double value) { Writer.Write(value); }

		public void write(byte[] bytes) {
			write(bytes.Length);
			memoryStream.Write(bytes, 0, bytes.Length);
		}

		public void write(string str) {
			Writer.Write(str);
		}

		public static IEnumerable<int[]> ndArrayWalker(int[] dimensions) {
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

		public void writeArray(Array arr) {

			int rank = arr.Rank;
			Type elementType = arr.GetType().GetElementType();
			write(rank);
			if(rank == 1) { 
				write(arr.Length);
				if(elementType.IsArray) {
					foreach(Array element in arr)
						writeArray(element);
				} else { 
					foreach(object element in arr)
						write(element, elementType);
				}
			} else {
				int[] dimensions = new int[rank];
				
				for(int dimensionIndex = 0; dimensionIndex < rank; dimensionIndex++) {
					write(dimensions[dimensionIndex] = arr.GetLength(dimensionIndex));
				}

				
				if(elementType.IsArray) {
					foreach(int[] currentPos in ndArrayWalker(dimensions))
						writeArray((Array)arr.GetValue(currentPos));
				} else {
					foreach(int[] currentPos in ndArrayWalker(dimensions))
						write(arr.GetValue(currentPos), elementType);
				}
				
			}
		}

		public void write(Serializable serializable) {
			serializable.writeToStream(this);
		}

		public void writeKeyValuePairsCollection<KEY_TYPE, VALUE_TYPE>(ICollection<KeyValuePair<KEY_TYPE, VALUE_TYPE>> collection) { 
			write(collection.Count());
			foreach(KeyValuePair<KEY_TYPE, VALUE_TYPE> pair in collection) { 
				write(pair.Key, typeof(KEY_TYPE));
				write(pair.Value, typeof(VALUE_TYPE));
			}
		}

		public void writeCollection<ELEMENT_TYPE>(ICollection<ELEMENT_TYPE> collection) { 
			write(collection.Count());
			foreach(ELEMENT_TYPE element in collection) { 
				write(element, typeof(ELEMENT_TYPE));
			}
		}

		public void writeCollectionObject(object collection) {
			Type elementType = collection.GetType().GetInterfaces().First((Type interfaceType) => interfaceType.IsGenericType && interfaceType.GetGenericTypeDefinition() == typeof(ICollection<>)).GetGenericArguments()[0];
			if(elementType.IsGenericType && elementType.GetGenericTypeDefinition() == typeof(KeyValuePair<,>)) {
				Type[] genericArgs = elementType.GetGenericArguments();
				rwKeyValuePairsCollectionMethodsInfo.writeMethodInfo.MakeGenericMethod(genericArgs[0], genericArgs[1]).Invoke(this, new object[] { collection });
			} else {
				rwCollectionMethodsInfo.writeMethodInfo.MakeGenericMethod(elementType).Invoke(this, new object[] { collection });
			}
		}


		public void writeObject(object obj) {
			if(ENABLE_NOT_SERIALIZABLE_WARNINGS) {
				Console.WriteLine("Warning! {0} is not serializable", obj.GetType().Name);
			}
			BinaryFormatter bf = new BinaryFormatter();
			bf.Serialize(memoryStream, obj);
		}

		public void writeUnknown(object value) { 
			write(value, value.GetType());
		}

		public void write(object value, Type type) {
			if(type == typeof(Int64)) {
				write((Int64)value);
			} 
			else if(type == typeof(Int32)) {
				write((Int32)value);
			} 
			else if(type == typeof(Int16)) {
				write((Int16)value);
			} 
			else if(type == typeof(sbyte)) { 
				write((sbyte)value);
			}
			else if(type == typeof(UInt64)) {
				write((UInt64)value);
			} 
			else if(type == typeof(UInt32)) {
				write((UInt32)value);
			} 
			else if(type == typeof(UInt16)) {
				write((UInt16)value);
			} 
			else if(type == typeof(byte)) {
				write((byte)value);
			} 
			else if(type == typeof(float)) {
				write((float)value);
			} 
			else if(type == typeof(double)) {
				write((double)value);
			} 
			else if(type == typeof(string)) {
				write((string)value);
			}
			else if(type == typeof(byte[])) {
				write((byte[])value);
			} 

			//Special types
			else if(typeof(SerializeStream).IsAssignableFrom(type)) {
				write(((SerializeStream)value).getBytes());
			} else if(typeof(Serializable).IsAssignableFrom(type)) {
				write((Serializable)value);
			} 
			//Difficult types
			else if(typeof(Array).IsAssignableFrom(type)) {
				if(USE_SERIALIZER_FOR_DIFFICULT_TYPES)
					Serializer.serialize(this, value, type);
				else
					writeArray((Array)value);
			} else if(type.GetInterfaces().Any((Type t) => isCollectionType(t))) {
				if(USE_SERIALIZER_FOR_DIFFICULT_TYPES)
					Serializer.serialize(this, value, type);
				else
					writeCollectionObject(value);
			} else if(Serializer.serializeSafe(this, value, type)) {

			} else {
				throw new SerializeStreamException($"Can't write type {type}");
			}
		} 
		

		/*
		 *                 Read
		 */

		public void readInt128(out UInt64 hiPart, out UInt64 loPart) { 
			hiPart = read<UInt64>();
			loPart = read<UInt64>();
		}

		public Int16 readInt16() { return Reader.ReadInt16(); }

		public Int32 readInt32() { return Reader.ReadInt32(); }

		public Int64 readInt64() { return Reader.ReadInt64(); }

		public sbyte readSByte() { return Reader.ReadSByte(); }

		public UInt64 readUInt64() { return Reader.ReadUInt64(); }

		public UInt32 readUInt32() { return Reader.ReadUInt32(); }

		public UInt16 readUInt16() { return Reader.ReadUInt16(); }

		public byte readByte() { return Reader.ReadByte(); }

		public Single readFloat() { return Reader.ReadSingle(); }

		public Double readDouble() { return Reader.ReadDouble(); }

		public byte[] readBytes() {
			int length = readInt32();
			byte[] buffer = new byte[length];
			memoryStream.Read(buffer, 0, length);
			return buffer;
		}

		public string readString() {
			return Reader.ReadString();
		}

		public ARRAY_TYPE readArray<ARRAY_TYPE>() {
			return (ARRAY_TYPE)(object)readArray(typeof(ARRAY_TYPE));
		}

		
		
		public COLLECTION_TYPE readCollection<COLLECTION_TYPE, ELEMENT_TYPE>() where COLLECTION_TYPE : ICollection<ELEMENT_TYPE>, new() { 
			COLLECTION_TYPE result = new COLLECTION_TYPE();
			int length = readInt32();
			for(int index = 0; index < length; index++)
				result.Add(read<ELEMENT_TYPE>());
			return result;
		}

		public COLLECTION_TYPE readKeyValuePairsCollection<COLLECTION_TYPE, KEY_TYPE, VALUE_TYPE>() where COLLECTION_TYPE : ICollection<KeyValuePair<KEY_TYPE, VALUE_TYPE>>, new() { 
			COLLECTION_TYPE result = new COLLECTION_TYPE();
			int length = readInt32();
			for(int index = 0; index < length; index++) {
				KEY_TYPE key = read<KEY_TYPE>();
				VALUE_TYPE value = read<VALUE_TYPE>();
				result.Add(new KeyValuePair<KEY_TYPE, VALUE_TYPE>(key, value));
			}
			return result;
		}

		public object readCollectionObject(Type collectionType) { 
			Type elementType = collectionType.GetInterfaces().First((Type interfaceType) => interfaceType.IsGenericType && interfaceType.GetGenericTypeDefinition() == typeof(ICollection<>)).GetGenericArguments()[0];
			if(elementType.IsGenericType && elementType.GetGenericTypeDefinition() == typeof(KeyValuePair<,>)) {
				Type[] genericArgs = elementType.GetGenericArguments();
				return rwKeyValuePairsCollectionMethodsInfo.readMethodInfo.MakeGenericMethod(collectionType, genericArgs[0], genericArgs[1]).Invoke(this, new object[0]);
			} else { 
				return rwCollectionMethodsInfo.readMethodInfo.MakeGenericMethod(collectionType, elementType).Invoke(this, new object[0]);
			}
		}

		public Array readArray(Type arrayType) { 
			int rank = readInt32();
			Type elementType = arrayType.GetElementType();
			if(rank == 1) { 
				int length = readInt32();
				Array result = Array.CreateInstance(elementType, length);

				if(elementType.IsArray) { 
					for(int index = 0; index < length; index++)
						result.SetValue(readArray(elementType), index);
				} else { 
					for(int index = 0; index < length; index++)
						result.SetValue(read(elementType), index);
				}
				return result;
			} else { 
				int[] dimensions = new int[rank];
				for(int dimensionIndex = 0; dimensionIndex < rank; dimensionIndex++) { 
					dimensions[dimensionIndex] = readInt32();
				}

				Array result = Array.CreateInstance(elementType, dimensions);
				if(elementType.IsArray) { 
					foreach(int[] currentPos in ndArrayWalker(dimensions))
						result.SetValue(readArray(elementType), currentPos);
				} else { 
					foreach(int[] currentPos in ndArrayWalker(dimensions))
						result.SetValue(read(elementType), currentPos);
				}

				return result;
			}
		}

		public object readObject() {
			BinaryFormatter bf = new BinaryFormatter();
			return bf.Deserialize(memoryStream);
		}

		public SERIALIZABLE_TYPE readSerializable<SERIALIZABLE_TYPE>() where SERIALIZABLE_TYPE : Serializable { 
			return (SERIALIZABLE_TYPE)readSerializable(typeof(SERIALIZABLE_TYPE));
		}

		public Serializable readSerializable(Type t) { 
			return (Serializable)t.GetConstructor(new Type[] { typeof(SerializeStream) }).Invoke(new object[] { this });
		}

		public void read<T>(out T result) {
			result = (T)read(typeof(T));
		}

		public T read<T>() {
			return (T)read(typeof(T));
		}

		public object read(Type type) {
			if(type == typeof(Int64)) {
				return readInt64();
			} 
			else if(type == typeof(Int32)) {
				return readInt32();
			} 
			else if(type == typeof(Int16)) {
				return readInt16();
			} 
			else if(type == typeof(sbyte)) {
				return readSByte();
			} 
			else if(type == typeof(UInt64)) {
				return readUInt64();
			} 
			else if(type == typeof(UInt32)) {
				return readUInt32();
			} 
			else if(type == typeof(UInt16)) {
				return readUInt16();
			} 
			else if(type == typeof(byte)) {
				return readByte();
			} 
			else if(type == typeof(float)) {
				return readFloat();
			} 
			else if(type == typeof(double)) {
				return readDouble();
			} 
			else if(type == typeof(string)) {
				return readString();
			}
			else if(type == typeof(byte[])) {
				return readBytes();
			}

			//Special types
			else if(type == typeof(SerializeStream)) {
				return new SerializeStream(readBytes());
			}
			else if(typeof(Serializable).IsAssignableFrom(type)) { 
				return readSerializable(type);
			}

			//Difficult types
			else if(type.IsArray) {
				if(USE_SERIALIZER_FOR_DIFFICULT_TYPES)
					return Serializer.deserialize(this, type);
				else
					return readArray(type);
			} else if(type.GetInterfaces().Any((Type t) => isCollectionType(t))) {
				if(USE_SERIALIZER_FOR_DIFFICULT_TYPES)
					return Serializer.deserialize(this, type);
				else
					return readCollectionObject(type);
			} else if(Serializer.deserializeSafe(this, out object result, type)) {
				return result;
			} else {
				throw new SerializeStreamException($"Can't read type {type}");
			}
		}



		//Backwrite sugar
		/*
		 * Write next length (exclude 4 bytes per int) in bytes to position
		 */
		public void writeNextLength(long pos) {
			Position = pos;
			write((UInt32)(Length - pos - sizeof(int)));
			Position = Length;
		}

		public long rememberAndSeek(int bytes) {
			long nowPos = Position;
			Position += bytes;
			return nowPos;
		}

	}

	

	public interface Serializable {
		void writeToStream(SerializeStream stream);
	}

	public class SerializeStreamException : Exception { 

		public SerializeStreamException(string str, params object[] args) : base(string.Format(str, args)) { }

	}
}
