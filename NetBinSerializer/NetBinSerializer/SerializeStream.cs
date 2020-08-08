using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

		public void write(Serializable serializable) {
			serializable.writeToStream(this);
		}

		public void writeObject(object obj) {
			if(ENABLE_NOT_SERIALIZABLE_WARNINGS) {
				Console.WriteLine("Warning! {0} is not serializable", obj.GetType().Name);
			}
			BinaryFormatter bf = new BinaryFormatter();
			bf.Serialize(memoryStream, obj);
		}

		public void write(object value) {
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
				write(((Array)value).Length);
				IEnumerable<object> arr = (IEnumerable<object>)value;
				foreach(object obj in arr)
					write(obj);
			} else if(value is Serializable) {
				write((Serializable)value);
			}
			else {
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

		public object readObject() {
			BinaryFormatter bf = new BinaryFormatter();
			return bf.Deserialize(memoryStream);
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
			else if(type == typeof(SerializeStream)) {
				return new SerializeStream(readBytes());
			}
			else if(type.IsArray) {
				Type elementType = type.GetElementType();
				int length = readInt32();
				object[] array = new object[length];
				
				for(int index = 0; index < length; index++) { 
					array[index] = read(elementType);
				}
				Array result = Array.CreateInstance(type, length);
				Array.Copy(array, result, length);
				return result;
			} else if(typeof(Serializable).IsAssignableFrom(type)) { 
				return readSerializable(type);
			} else {
				object result;
				if(!Serializer.deserialize(this, type, out result))
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
