﻿using NetBinSerializer;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Test {

	//Struct was checked too
	class SerializableExample : ISerializable {
		double x, y;


		public SerializableExample(double x, double y) { 
			this.x = x;
			this.y = y;
		}

		public void WriteToStream(SerializeStream stream) {
			stream.Write(x);
			stream.Write(y);
		}

		public void ReadFromStream(SerializeStream stream) {
			x = stream.ReadDouble();
			y = stream.ReadDouble();
		}

		public override string ToString() {
			return $"({x}; {y})";
		}

	}

	class Program {

		static void foo<T1>(ICollection<T1> t) { 
			Console.WriteLine(typeof(ICollection<T1>));
		}

		static T1 bar<T1, T2>() where T1 : ICollection<T2>, new() { 
			T1 result = new T1();
			return result;
		}

		static void Main(string[] args) {
			Console.WriteLine("--------------------------------Low level serialization--------------------------------");
			TestLowLevelSerialization();
			Console.WriteLine("--------------------------------High level serialization--------------------------------");
			TestHighLevelSerialization();
			Console.ReadKey();
		}

		static void TestLowLevelSerialization() { 
			//--------------------------------Serialize--------------------------------
			SerializeStream sstream = new SerializeStream();
			SerializeStream.USE_SERIALIZER_FOR_DIFFICULT_TYPES = false;
			//Write simple values
			int writeInt = 12;
			string writeString = "Hello!";
			double writeDouble = 100.5;
			
			sstream.Write(writeInt);
			sstream.Write(writeString);
			sstream.Write(writeDouble);

			//Write arrays
			int[] writeInt1DArray = { 5, 5, 6, 3};
			sstream.WriteArray(writeInt1DArray);
			int[,] writeInt2DArray = { { 1, 3 }, { 4, 1 } };
			sstream.WriteArray(writeInt2DArray);
			int[][] writeIntNestedArrays = { new int[]{ 1, 3 }, new int[]{ 4, 1, 1, 1, 2, 2 } };
			sstream.WriteArray(writeIntNestedArrays);
			int[,][] write2DIntNestedArraysArray = { {new int[]{ 1, 3 }, new int[]{ 4, 1, 1, 1, 2, 2 } }, { new int[] { 5, 6, 6, 3 }, new int[] { 3, 4, 5, 6} } };
			sstream.WriteArray(write2DIntNestedArraysArray);

			//Write serializable
			SerializableExample writeSerializableExample = new SerializableExample(3.0, 4.0);
			sstream.Write(writeSerializableExample);

			//Write list
			List<int> writeList = new List<int>() { 5, 3, 2, 1 };
			sstream.WriteCollection(writeList);

			//Write dictionary
			Dictionary<string, int> writeDict = new Dictionary<string, int>() { 
				{ "azrael", 5 },
				{ "luthor", 100},
				{ "tom", 50},
			};
			sstream.WriteCollectionObject(writeDict);

			sstream.WriteUInt32(0xA116000D);

			//--------------------------------Deserialize--------------------------------

			SerializeStream dstream = new SerializeStream(sstream.GetBytes());
			//Read simple values
			int readInt = dstream.ReadInt32();
			string readString = dstream.ReadString();
			double readDouble = dstream.ReadDouble();
			Console.WriteLine("Output: {0}, {1}, {2}", readInt, readString, readDouble);

			//Read arrays
			int[] readInt1DArray = dstream.ReadArray<int[]>();
			Console.WriteLine("Output 1D Array: ");
			for(int index = 0; index < readInt1DArray.Length; index++)
				Console.Write($"{readInt1DArray[index]} ");
			Console.WriteLine();


			Console.WriteLine();
			Console.WriteLine("Output 2D Array: ");
			int[,] readInt2DArray = dstream.ReadArray<int[,]>();
			for(int i = 0; i < readInt2DArray.GetLength(0); i++) { 
				for(int j = 0; j < readInt2DArray.GetLength(1); j++) { 
					Console.Write($"{readInt2DArray[i, j]} ");
				}
				Console.WriteLine();
			}


			Console.WriteLine();
			Console.WriteLine("Output nested arrays: ");
			int[][] readIntNestedArrays = dstream.ReadArray<int[][]>();
			for(int i = 0; i < readIntNestedArrays.Length; i++) { 
				for(int j = 0; j < readIntNestedArrays[i].Length; j++) { 
					Console.Write($"{readIntNestedArrays[i][j]} ");
				}
				Console.WriteLine();
			}

			Console.WriteLine();
			Console.WriteLine("Output nested arrays in 2D array: ");
			int[,][] read2DIntNestedArraysArray = dstream.ReadArray<int[,][]>();
			for(int i = 0; i < read2DIntNestedArraysArray.GetLength(0); i++) { 
				for(int j = 0; j < read2DIntNestedArraysArray.GetLength(1); j++) { 
					for(int index = 0; index < read2DIntNestedArraysArray[i, j].Length; index++)
						Console.Write($"{read2DIntNestedArraysArray[i, j][index]} ");
					Console.Write(" | ");
				}
				Console.WriteLine();
			}

			Console.WriteLine();

			//Read serializable
			SerializableExample readSerializableExample = dstream.ReadSerializable<SerializableExample>();
			Console.WriteLine($"Output: {readSerializableExample}");

			Console.WriteLine();
			Console.WriteLine("Output list:");
			foreach(int element in dstream.ReadCollection<List<int>, int>())
				Console.Write($"{element} ");
			Console.WriteLine();

			Console.WriteLine();
			Console.WriteLine("Output Dictionary:");
			foreach(var element in (Dictionary<string, int>)dstream.ReadCollectionObject(typeof(Dictionary<string, int>)))
				Console.WriteLine($"{element.Key}: {element.Value} ");
			Console.WriteLine();
			
			Console.WriteLine($"Control code: {dstream.ReadUInt32():X}");
			Console.WriteLine();
			
		}

		static void printArray(string header, int[] arr) { 
			if(arr == null) {
				Console.WriteLine($"{header}: null");
				return;
			}

			Console.Write($"{header}: ");
			for(int index = 0; index < arr.Length; index++)
				Console.Write($"{arr[index]} ");
			Console.WriteLine();
		}

		static void TestHighLevelSerialization() { 
			//--------------------------------Serialize--------------------------------
			Serializer.CACHE_DEFAULT = true;
			SerializeStream.USE_SERIALIZER_FOR_DIFFICULT_TYPES = true;

			SerializeStream sstream = new SerializeStream();
			//Write simple values
			sstream.Serialize<int>(12);
			sstream.Serialize<string>("Hello!");
			sstream.Serialize<double>(100.5);

			//Write arrays
			sstream.Serialize<int[]>(new int[] { 5, 5, 6, 3});
			sstream.Serialize<int[,]>(new int [,] { { 1, 3 }, { 4, 1 } });
			sstream.Serialize<int[][]>(new int [][] { new int[]{ 1, 3 }, new int[]{ 4, 1, 1, 1, 2, 2 } });
			sstream.Serialize<int[,][]>(new int[,][] { {new int[]{ 1, 3 }, new int[]{ 4, 1, 1, 1, 2, 2 } }, { new int[] { 5, 6, 6, 3 }, new int[] { 3, 4, 5, 6} } });

			//Write serializable
			sstream.Serialize<SerializableExample>(new SerializableExample(3.0, 4.0));

			//Write list
			sstream.Serialize<List<int>>(new List<int>() { 5, 3, 2, 1 });

			//Write dictionary
			sstream.Serialize<Dictionary<string, int>>(new Dictionary<string, int>() { 
				{ "azrael", 5 },
				{ "luthor", 100},
				{ "tom", 50},
			});

			//Context test
			int[] arr = { 1, 2, 3, 4, 5 };
			SerializationContext scontext = new SerializationContext();
			sstream.Serialize<int[]>(arr, null, scontext);
			sstream.Serialize<int[]>(arr, null, scontext);
			sstream.Serialize<int[]>(null, null, scontext);
			sstream.Serialize<int[]>(arr, null, scontext);

			sstream.WriteUInt32(0xA116000D);

			//--------------------------------Deserialize--------------------------------

			SerializeStream dstream = new SerializeStream(sstream.GetBytes());
			//Read simple values
			int readInt = dstream.Deserialize<int>();
			string readString = dstream.Deserialize<string>();
			double readDouble = dstream.Deserialize<double>();
			Console.WriteLine("Output: {0}, {1}, {2}", readInt, readString, readDouble);

			//Read arrays
			int[] readInt1DArray = dstream.Deserialize<int[]>();
			Console.WriteLine("Output 1D Array: ");
			for(int index = 0; index < readInt1DArray.Length; index++)
				Console.Write($"{readInt1DArray[index]} ");
			Console.WriteLine();


			Console.WriteLine();
			Console.WriteLine("Output 2D Array: ");
			int[,] readInt2DArray = dstream.Deserialize<int[,]>();
			for(int i = 0; i < readInt2DArray.GetLength(0); i++) { 
				for(int j = 0; j < readInt2DArray.GetLength(1); j++) { 
					Console.Write($"{readInt2DArray[i, j]} ");
				}
				Console.WriteLine();
			}


			Console.WriteLine();
			Console.WriteLine("Output nested arrays: ");
			int[][] readIntNestedArrays = dstream.Deserialize<int[][]>();
			for(int i = 0; i < readIntNestedArrays.Length; i++) { 
				for(int j = 0; j < readIntNestedArrays[i].Length; j++) { 
					Console.Write($"{readIntNestedArrays[i][j]} ");
				}
				Console.WriteLine();
			}

			Console.WriteLine();
			Console.WriteLine("Output nested arrays in 2D array: ");
			int[,][] read2DIntNestedArraysArray = dstream.Deserialize<int[,][]>();
			for(int i = 0; i < read2DIntNestedArraysArray.GetLength(0); i++) { 
				for(int j = 0; j < read2DIntNestedArraysArray.GetLength(1); j++) { 
					for(int index = 0; index < read2DIntNestedArraysArray[i, j].Length; index++)
						Console.Write($"{read2DIntNestedArraysArray[i, j][index]} ");
					Console.Write(" | ");
				}
				Console.WriteLine();
			}

			Console.WriteLine();

			//Read serializable
			SerializableExample readSerializableExample = dstream.ReadSerializable<SerializableExample>();
			Console.WriteLine($"Output: {readSerializableExample}");

			Console.WriteLine();
			Console.WriteLine("Output list:");
			foreach(int element in dstream.Deserialize<List<int>>())
				Console.Write($"{element} ");
			Console.WriteLine();

			Console.WriteLine();
			Console.WriteLine("Output Dictionary:");
			foreach(var element in dstream.Deserialize<Dictionary<string, int>>())
				Console.WriteLine($"{element.Key}: {element.Value} ");
			Console.WriteLine();

			//Context test
			Console.WriteLine();
			Console.WriteLine();
			Console.WriteLine("Context test...");
			DeserializationContext dcontext = new DeserializationContext();
			int[] arr1 = dstream.Deserialize<int[]>(null, dcontext);
			int[] arr2 = dstream.Deserialize<int[]>(null, dcontext);
			int[] arr3 = dstream.Deserialize<int[]>(null, dcontext);
			int[] arr4 = dstream.Deserialize<int[]>(null, dcontext);
			printArray("arr1", arr1);
			printArray("arr2", arr2);
			printArray("arr3", arr3);
			printArray("arr4", arr4);
			Console.WriteLine($"arr1 == arr2: {arr1 == arr2}");
			Console.WriteLine($"arr2 == arr4: {arr2 == arr4}");
			Console.WriteLine();

			Console.WriteLine($"Control code: {dstream.Deserialize<UInt32>():X}");
		}
	}
}
