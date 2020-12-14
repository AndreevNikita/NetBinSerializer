using NetBinSerializer;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Test {

	//Struct was checked too
	class SerializableExample : Serializable {
		double x, y;


		public SerializableExample(double x, double y) { 
			this.x = x;
			this.y = y;
		}

		public SerializableExample(SerializeStream stream) { 
			x = stream.readDouble();
			y = stream.readDouble();
		}

		public void writeToStream(SerializeStream stream) {
			stream.write(x);
			stream.write(y);
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
		
			//--------------------------------Serialize--------------------------------
			SerializeStream sstream = new SerializeStream();
			//Write simple values
			int writeInt = 12;
			string writeString = "Hello!";
			double writeDouble = 100.5;
			
			sstream.write(writeInt);
			sstream.write(writeString);
			sstream.write(writeDouble);

			//Write arrays
			int[] writeInt1DArray = { 5, 5, 6, 3};
			sstream.writeArray(writeInt1DArray);
			int[,] writeInt2DArray = { { 1, 3 }, { 4, 1 } };
			sstream.writeArray(writeInt2DArray);
			int[][] writeIntNestedArrays = { new int[]{ 1, 3 }, new int[]{ 4, 1, 1, 1, 2, 2 } };
			sstream.writeArray(writeIntNestedArrays);
			int[,][] write2DIntNestedArraysArray = { {new int[]{ 1, 3 }, new int[]{ 4, 1, 1, 1, 2, 2 } }, { new int[] { 5, 6, 6, 3 }, new int[] { 3, 4, 5, 6} } };
			sstream.writeArray(write2DIntNestedArraysArray);

			//Write serializable
			SerializableExample writeSerializableExample = new SerializableExample(3.0, 4.0);
			sstream.write(writeSerializableExample);

			//Write list
			List<int> writeList = new List<int>() { 5, 3, 2, 1 };
			sstream.writeCollection(writeList);

			//Write dictionary
			Dictionary<string, int> writeDict = new Dictionary<string, int>() { 
				{ "azrael", 5 },
				{ "luthor", 100},
				{ "tom", 50},
			};
			sstream.writeCollectionObject(writeDict);

			//--------------------------------Deserialize--------------------------------

			SerializeStream dstream = new SerializeStream(sstream.getBytes());
			//Read simple values
			int readInt = dstream.readInt32();
			string readString = dstream.readString();
			double readDouble = dstream.readDouble();
			Console.WriteLine("Output: {0}, {1}, {2}", readInt, readString, readDouble);

			//Read arrays
			int[] readInt1DArray = dstream.readArray<int[]>();
			Console.WriteLine("Output 1D Array: ");
			for(int index = 0; index < readInt1DArray.Length; index++)
				Console.Write($"{readInt1DArray[index]} ");
			Console.WriteLine();


			Console.WriteLine();
			Console.WriteLine("Output 2D Array: ");
			int[,] readInt2DArray = dstream.readArray<int[,]>();
			for(int i = 0; i < readInt2DArray.GetLength(0); i++) { 
				for(int j = 0; j < readInt2DArray.GetLength(1); j++) { 
					Console.Write($"{readInt2DArray[i, j]} ");
				}
				Console.WriteLine();
			}


			Console.WriteLine();
			Console.WriteLine("Output nested arrays: ");
			int[][] readIntNestedArrays = dstream.readArray<int[][]>();
			for(int i = 0; i < readIntNestedArrays.Length; i++) { 
				for(int j = 0; j < readIntNestedArrays[i].Length; j++) { 
					Console.Write($"{readIntNestedArrays[i][j]} ");
				}
				Console.WriteLine();
			}

			Console.WriteLine();
			Console.WriteLine("Output nested arrays in 2D array: ");
			int[,][] read2DIntNestedArraysArray = dstream.readArray<int[,][]>();
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
			SerializableExample readSerializableExample = dstream.readSerializable<SerializableExample>();
			Console.WriteLine($"Output: {readSerializableExample}");

			Console.WriteLine();
			Console.WriteLine("Output list:");
			foreach(int element in dstream.readCollection<List<int>, int>())
				Console.Write($"{element} ");
			Console.WriteLine();

			Console.WriteLine();
			Console.WriteLine("Output Dictionary:");
			foreach(var element in (Dictionary<string, int>)dstream.readCollectionObject(typeof(Dictionary<string, int>)))
				Console.WriteLine($"{element.Key}: {element.Value} ");
			Console.WriteLine();

			Console.ReadKey();
		}
	}
}
