using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace NetBinSerializer {

	public partial class SerializeStream : ISerializable {
		public virtual bool EnableStandartSerializer { get; set; } = false;
		public long Position { get => memoryStream.Position; set => memoryStream.Position = value; }
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

		/*--------------------------------------------------------------------------------------------------------------------------------*/
		/*--------------------------------------------------------------------------------------------------------------------------------*/
		/*--------------------------------------------------------------------------------------------------------------------------------*/

		public static bool IsCollectionType(Type type) {
			return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ICollection<>);
		}

		public static bool IsKeyValuePairType(Type type) {
			return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(KeyValuePair<,>);
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

		public void Write(Int64 value) => WriteInt64(value);
		public void WriteInt64Object(object value) => WriteInt64((Int64)value);
		public void WriteInt64(Int64 value) => Writer.Write(value);


		public void Write(Int32 value) => WriteInt32(value);
		public void WriteInt32Object(object value) => WriteInt32((Int32)value);
		public void WriteInt32(Int32 value) => Writer.Write(value);


		public void Write(Int16 value) => WriteInt16(value);
		public void WriteInt16Object(object value) => WriteInt16((Int16)value);
		public void WriteInt16(Int16 value) => Writer.Write(value);


		public void Write(sbyte value) => WriteSByte(value);
		public void WriteSByteObject(object value) => WriteSByte((sbyte)value);
		public void WriteSByte(sbyte value) => Writer.Write(value);


		public void Write(UInt64 value) => WriteUInt64(value);
		public void WriteUInt64Object(object value) => WriteUInt64((UInt64)value);
		public void WriteUInt64(UInt64 value) => Writer.Write(value);


		public void Write(UInt32 value) => WriteUInt32(value);
		public void WriteUInt32Object(object value) => WriteUInt32((UInt32)value);
		public void WriteUInt32(UInt32 value) => Writer.Write(value);


		public void Write(UInt16 value) => WriteUInt16(value);
		public void WriteUInt16Object(object value) => WriteUInt16((UInt16)value);
		public void WriteUInt16(UInt16 value) => Writer.Write(value);


		public void Write(byte value) => WriteByte(value);
		public void WriteByteObject(object value) => WriteByte((byte)value);
		public void WriteByte(byte value) => Writer.Write(value);


		public void Write(Single value) => WriteFloat(value);
		public void WriteFloatObject(object value) => WriteFloat((Single)value);
		public void WriteFloat(Single value) => Writer.Write(value);


		public void Write(Double value) => WriteDouble(value);
		public void WriteDoubleObject(object value) => WriteDouble((Double)value);
		public void WriteDouble(Double value) => Writer.Write(value);


		public void Write(byte[] bytes) => WriteBytes(bytes);
		public void WriteBytesObject(object value) => WriteBytes((byte[])value);
		public void WriteBytes(byte[] bytes) {
			Write(bytes.Length);
			memoryStream.Write(bytes, 0, bytes.Length);
		}


		public void Write(string str) => WriteString(str);
		public void WriteStringObject(object value) => WriteString((string)value);
		public void WriteString(string str) => Writer.Write(str);

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

		public void WriteArray<TElement>(TElement[] arr) => WriteArray((Array)arr);

		public void WriteArrayObject(object obj) => WriteArray((Array)obj);
		public virtual void WriteArray(Array arr) {
			int rank = arr.Rank;
			Type elementType = arr.GetType().GetElementType();
			IRWMethods rwMethods = GetRWMethods(elementType);
			Write(rank);
			if(rank == 1) {
				Write(arr.Length);
				for(int index = 0; index < arr.Length; index++)
					rwMethods.WriteMethod(arr.GetValue(index));
			} else {
				int[] dimensions = new int[rank];

				for(int dimensionIndex = 0; dimensionIndex < rank; dimensionIndex++) {
					Write(dimensions[dimensionIndex] = arr.GetLength(dimensionIndex));
				}

				foreach(int[] currentPos in NDArrayWalker(dimensions))
					rwMethods.WriteMethod(arr.GetValue(currentPos));
			}
		}

		public void Write(ISerializable serializable) { WriteSerializable(serializable); }
		public void WriteSerializableObject(object obj) => WriteSerializable((ISerializable)obj);
		public void WriteSerializable(ISerializable serializable) {
			serializable.WriteToStream(this);
		}

		public virtual void WriteCollection<TCollection, TElement>(TCollection collection) where TCollection : ICollection<TElement>, new() {
			long countPosition = RememberAndSeek(sizeof(Int32));
			IRWMethods rwMethods = GetRWMethods(typeof(TElement));
			Int32 elementsCounter = 0;
			foreach(TElement element in collection) {
				rwMethods.WriteMethod(element);
				elementsCounter++;
			}
			long posBuffer = Position;
			Position = countPosition;
			WriteInt32(elementsCounter);
			Position = posBuffer;
		}

		public void WriteCollectionObject(object collection) {
			Type collectionType = collection.GetType();
			Type elementType = collectionType.GetInterfaces().First((Type interfaceType) => interfaceType.IsGenericType && interfaceType.GetGenericTypeDefinition() == typeof(ICollection<>)).GetGenericArguments()[0];
			RWCollectionMethodsInfo.writeMethodInfo.MakeGenericMethod(collectionType, elementType).Invoke(this, new object[] { collection });
		}


		public virtual void WriteObject(object obj) {
			if(EnableStandartSerializer) {
				Console.WriteLine("Warning! {0} is not serializable", obj.GetType().Name);
			}
			BinaryFormatter bf = new BinaryFormatter();
			bf.Serialize(memoryStream, obj);
		}

		public void WriteUnknown(object value) {
			Write(value, value.GetType());
		}

		public void Write<T>(object value) => Write(value, typeof(T));
		public void Write(object value, Type type) => GetRWMethods(type).WriteMethod(value);

		/*
		 *                 Read
		 */

		public void ReadInt128(out UInt64 hiPart, out UInt64 loPart) {
			hiPart = Read<UInt64>();
			loPart = Read<UInt64>();
		}

		public object ReadInt64Object() => ReadInt64();
		public Int64 ReadInt64() => Reader.ReadInt64();

		public object ReadInt32Object() => ReadInt32();
		public Int32 ReadInt32() => Reader.ReadInt32();

		public object ReadInt16Object() => ReadInt16();
		public Int16 ReadInt16() => Reader.ReadInt16();

		public object ReadSByteObject() => ReadSByte();
		public sbyte ReadSByte() => Reader.ReadSByte();


		public object ReadUInt64Object() => ReadUInt64();
		public UInt64 ReadUInt64() => Reader.ReadUInt64();

		public object ReadUInt32Object() => ReadUInt32();
		public UInt32 ReadUInt32() => Reader.ReadUInt32();

		public object ReadUInt16Object() => ReadUInt16();
		public UInt16 ReadUInt16() => Reader.ReadUInt16();

		public object ReadByteObject() => ReadByte();
		public byte ReadByte() => Reader.ReadByte();

		public object ReadFloatObject() => ReadFloat();
		public Single ReadFloat() => Reader.ReadSingle();

		public object ReadDoubleObject() => ReadDouble();
		public Double ReadDouble() => Reader.ReadDouble();

		public object ReadBytesObject() => ReadBytes();
		public byte[] ReadBytes() {
			int length = ReadInt32();
			byte[] buffer = new byte[length];
			memoryStream.Read(buffer, 0, length);
			return buffer;
		}

		public object ReadStringObject() => ReadString();
		public string ReadString() => Reader.ReadString();

		public ARRAY_TYPE ReadArray<ARRAY_TYPE>() {
			return (ARRAY_TYPE)(object)ReadArray(typeof(ARRAY_TYPE));
		}



		public virtual TCollection ReadCollection<TCollection, TElement>() where TCollection : ICollection<TElement>, new() {
			TCollection result = new TCollection();
			IRWMethods rwMethods = GetRWMethods(typeof(TElement));
			int length = ReadInt32();
			for(int index = 0; index < length; index++)
				result.Add((TElement)rwMethods.ReadMethod());
			return result;
		}

		public object ReadCollectionObject(Type collectionType) {
			Type elementType = collectionType.GetInterfaces().First((Type interfaceType) => interfaceType.IsGenericType && interfaceType.GetGenericTypeDefinition() == typeof(ICollection<>)).GetGenericArguments()[0];
			return RWCollectionMethodsInfo.readMethodInfo.MakeGenericMethod(collectionType, elementType).Invoke(this, new object[0]);
		}

		public object ReadArrayObject(Type type) => ReadArray(type);
		public virtual Array ReadArray(Type arrayType) {
			int rank = ReadInt32();
			Type elementType = arrayType.GetElementType();
			IRWMethods rwMethods = GetRWMethods(elementType);
			if(rank == 1) {
				int length = ReadInt32();
				Array result = Array.CreateInstance(elementType, length);

				for(int index = 0; index < length; index++)
					result.SetValue(rwMethods.ReadMethod(), index);
				return result;
			} else {
				int[] dimensions = new int[rank];
				for(int dimensionIndex = 0; dimensionIndex < rank; dimensionIndex++) {
					dimensions[dimensionIndex] = ReadInt32();
				}

				Array result = Array.CreateInstance(elementType, dimensions);
				foreach(int[] currentPos in NDArrayWalker(dimensions))
					result.SetValue(rwMethods.ReadMethod(), currentPos);

				return result;
			}
		}

		public virtual object ReadObject() {
			BinaryFormatter bf = new BinaryFormatter();
			return bf.Deserialize(memoryStream);
		}

		public TSerializable ReadSerializable<TSerializable>() where TSerializable : ISerializable {
			return (TSerializable)ReadSerializable(typeof(TSerializable));
		}

		public object ReadSerializableObject(Type t) => ReadSerializable(t);
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

		public object Read(Type type) => GetRWMethods(type).ReadMethod();

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

		public void WriteToStream(SerializeStream stream) => stream.WriteBytes(this.GetBytes());

		public void ReadFromStream(SerializeStream stream) => new SerializeStream(stream.ReadBytes());
	}

	public class SerializeStreamException : Exception {

		public SerializeStreamException(string str, params object[] args) : base(string.Format(str, args)) { }

	}
}
