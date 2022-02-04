using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NetBinSerializer {

	public class SimpleSerializationMethodsBuilder : ISerializationMethodsBuilder {
		public SerializationMethodsBase GetSerializationMethods(Serializer serializer, Type type, bool withCache) {
			if(type.IsArray) {
				SerializationMethodsBase arrayElementSerializationMethods = serializer.GetSerializationMethods(type.GetElementType(), withCache);
				return new ArraySerializationMethodsChain(type, arrayElementSerializationMethods);

			} else if(type.GetInterfaces().Any((Type t) => SerializeStream.IsCollectionType(t))) {
				Type elementType = type.GetInterfaces().First((Type interfaceType) => interfaceType.IsGenericType && interfaceType.GetGenericTypeDefinition() == typeof(ICollection<>)).GetGenericArguments()[0];
				SerializationMethodsBase collectionElementSerializationMethods = serializer.GetSerializationMethods(elementType, withCache);
				return (SerializationMethodsBase)typeof(CollectionSerializationMethodsChain<,>).MakeGenericType(type, elementType)
					.GetConstructor(new Type[] { typeof(SerializationMethodsBase) })
					.Invoke(new object[] { collectionElementSerializationMethods });

			} else if(type.IsGenericType && type.GetGenericTypeDefinition() == typeof(KeyValuePair<,>)) {
				Type[] genericArgs = type.GetGenericArguments();
				SerializationMethodsBase keySerializationMethods = serializer.GetSerializationMethods(genericArgs[0], withCache);
				SerializationMethodsBase valueSerializationMethods = serializer.GetSerializationMethods(genericArgs[1], withCache);
				return (SerializationMethodsBase)typeof(KeyValuePairSerializationMethodsChain<,,>).MakeGenericType(type, genericArgs[0], genericArgs[1])
					.GetConstructor(new Type[] { typeof(SerializationMethodsBase), typeof(SerializationMethodsBase) })
					.Invoke(new object[] { keySerializationMethods, valueSerializationMethods });

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

		public ArraySerializationMethodsChain(Type arrayType, SerializationMethodsBase serializationMethods) : base(arrayType, serializationMethods) {
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

	public class CollectionSerializationMethodsChain<TCollection, TElement> : SerializationMethodsChain where TCollection : ICollection<TElement>, new() {

		public CollectionSerializationMethodsChain(SerializationMethodsBase serializationMethods) : base(typeof(TCollection), serializationMethods) { }

		public override void Serialize(SerializeStream stream, object obj, SerializationContext context) {
			if(context.Optimize(stream, obj))
				return;
			TCollection collectionObj = (TCollection)obj;

			long countPosition = stream.RememberAndSeek(sizeof(Int32));
			Int32 elementsCounter = 0;
			foreach(TElement element in collectionObj) {
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

			TCollection result = new TCollection();
			context.AddObject(result);
			int length = stream.ReadInt32();
			for(int index = 0; index < length; index++)
				result.Add((TElement)ContainTypeSerializationMethods.Deserialize(stream, context));

			return result;
		}
	}

	public class KeyValuePairSerializationMethodsChain<KEY_VALUE_PAIR_TYPE, KEY_TYPE, VALUE_TYPE> : SerializationMethodsChain {

		SerializationMethodsBase keySerializationMethods;
		SerializationMethodsBase valueSerializationMethods;

		public KeyValuePairSerializationMethodsChain(SerializationMethodsBase keySerializationMethods, SerializationMethodsBase valueSerializationMethods) : base(typeof(KEY_VALUE_PAIR_TYPE), null) {
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

}
