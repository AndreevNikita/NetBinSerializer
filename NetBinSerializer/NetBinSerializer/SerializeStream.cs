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
	public class SerializeStream
	{
		public static bool ENABLE_NOT_SERIALIZABLE_WARNINGS = false;

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

		private static readonly MethodInfo writeCollectionMethodInfo;
		private static readonly MethodInfo writeKeyValuePairsCollectionMethodInfo;

		private static readonly MethodInfo readCollectionMethodInfo;
		private static readonly MethodInfo readKeyValuePairsCollectionMethodInfo;

		static SerializeStream() {
			writeCollectionMethodInfo = typeof(SerializeStream).GetMethod("writeCollection");
			writeKeyValuePairsCollectionMethodInfo = typeof(SerializeStream).GetMethod("writeKeyValuePairsCollection");
			readCollectionMethodInfo = typeof(SerializeStream).GetMethod("readCollection");
			readKeyValuePairsCollectionMethodInfo = typeof(SerializeStream).GetMethod("readKeyValuePairsCollection");
		}

		private static bool isCollectionType(Type type) { 
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

		private IEnumerable<int[]> ndArrayWalker(int[] dimensions) {
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
						writeUnknown(element);
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
						writeUnknown(arr.GetValue(currentPos));
				}
				
			}
		}

		public void write(Serializable serializable) {
			serializable.writeToStream(this);
		}

		public void writeKeyValuePairsCollection<KEY_TYPE, VALUE_TYPE>(ICollection<KeyValuePair<KEY_TYPE, VALUE_TYPE>> collection) { 
			write(collection.Count());
			foreach(KeyValuePair<KEY_TYPE, VALUE_TYPE> pair in collection) { 
				writeUnknown(pair.Key);
				writeUnknown(pair.Value);
			}
		}

		public void writeCollection<ELEMENT_TYPE>(ICollection<ELEMENT_TYPE> collection) { 
			write(collection.Count());
			foreach(ELEMENT_TYPE element in collection) { 
				writeUnknown(element);
			}
		}

		public void writeCollectionObject(object collection) {
			Type elementType = collection.GetType().GetInterfaces().First((Type interfaceType) => interfaceType.IsGenericType && interfaceType.GetGenericTypeDefinition() == typeof(ICollection<>)).GetGenericArguments()[0];
			if(elementType.GetGenericTypeDefinition() == typeof(KeyValuePair<,>)) {
				Type[] genericArgs = elementType.GetGenericArguments();
				writeKeyValuePairsCollectionMethodInfo.MakeGenericMethod(genericArgs[0], genericArgs[1]).Invoke(this, new object[] { collection });
			} else {
				writeCollectionMethodInfo.MakeGenericMethod(elementType).Invoke(this, new object[] { collection });
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
			if(value is Int64) {
				write((Int64)value);
			} 
			else if(value is Int32) {
				write((Int32)value);
			} 
			else if(value is Int16) {
				write((Int16)value);
			} 
			else if(value is sbyte) { 
				write((sbyte)value);
			}
			else if(value is UInt64) {
				write((UInt64)value);
			} 
			else if(value is UInt32) {
				write((UInt32)value);
			} 
			else if(value is UInt16) {
				write((UInt16)value);
			} 
			else if(value is byte) {
				write((byte)value);
			} 
			else if(value is float) {
				write((float)value);
			} 
			else if(value is double) {
				write((double)value);
			} 
			else if(value is string) {
				write((string)value);
			}
			else if(value is byte[]) {
				write((byte[])value);
			} else if(value is SerializeStream) {
				write(((SerializeStream)value).getBytes());
			} else if(value is Array) {
				writeArray((Array)value);
			} else if(value.GetType().GetInterfaces().Any((Type t) => isCollectionType(t))) {
				writeCollectionObject(value);
			} else {
				if(!Serializer.serialize(value, this))
					writeObject(value);
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
			if(elementType.GetGenericTypeDefinition() == typeof(KeyValuePair<,>)) {
				Type[] genericArgs = elementType.GetGenericArguments();
				return readKeyValuePairsCollectionMethodInfo.MakeGenericMethod(collectionType, genericArgs[0], genericArgs[1]).Invoke(this, new object[0]);
			} else { 
				return readCollectionMethodInfo.MakeGenericMethod(collectionType, elementType).Invoke(this, new object[0]);
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
						result.SetValue(readUnknown(elementType), index);
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
						result.SetValue(readUnknown(elementType), currentPos);
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
			result = (T)readUnknown(typeof(T));
		}

		public T read<T>() {
			return (T)readUnknown(typeof(T));
		}

		public object readUnknown(Type type) {
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
			else if(type == typeof(SerializeStream)) {
				return new SerializeStream(readBytes());
			}
			else if(type.IsArray) {
				return readArray(type);
			} else if(typeof(Serializable).IsAssignableFrom(type)) { 
				return readSerializable(type);
			} else if(type.GetInterfaces().Any((Type t) => isCollectionType(t))) {
				return readCollectionObject(type);
			} else {
				object result;
				if(Serializer.deserialize(this, type, out result))
					result = readObject();
				return result;
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
}
