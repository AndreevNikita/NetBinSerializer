# NetBinSerializer
C# types serialization library

Features:
* minimum result bytes array size (without compressing)
* program and version independence (but it's unsafe (without classes fields coincidence control))
* base for serialization methods auto builders
* opportunity to save serialization/deserialization type methods and use it
* using reflection only for type serialization methods building

Limitations
* serialization classes and structures extension isn't allowed. I.e. if `class Cat : Animal` will be passed with animal type, it will be serialized as an `Animal`
* NetBinSerializer can't normal serialize/deserialize types, which use system pointers/descriptors and other resources
* you should know, which fields are in serialized class/structure

## Classes
NetBinSerialization library has two serialization classes  
**SerializeStream** - low level serialization object to serialize base types  
**Serializer** - high level serialization static class with serialization methods caching, picking and building for difficult types  

Interfaces:  
**ISerializable** - interface with writeToStream and readToStream methods. If a class or structure implements this interface, serialization and deserialization will be complete by these methods call (deserialization works without constructor call!).  
**ISerializationMethodsBuilder** - interface for high level serialize/deserialize custom methods builder class (implement getSerializationMethods for type serialization methods build)  


**SerializationMethodsBase** - object, that provides methods `serialize(SerializeStream stream, object obj)` and `object deserialize(SerializeStream stream)`. Serializer class contains cached Dictionary of <Type, ISerializationMethods>.

## SerializeStream
Stream for objects serialization/deserialization

#### Create
* for serialization create SerializeStream by `new SerializeStream()`
* for deserialization create SerializeStream by `new SerializeStream(byte[] bytes)`

#### Write/read
Supported main types: Int64, Int32, Int16, SByte, UInt64, UInt32, UInt16, Byte, Float, Double, String, bytes array, Serializable

* to write base type in bytes use `void write([var_type])` overloaded method
* to read base type in bytes use `[var_type] read+[var_type]()`
* you can write objects by `void writeUnknown(object)`, but it's no reason to use this method. You should always know serializable/deserializable object's type

So SerializeStream can work with other Arrays and Collections types, but it's better to use Serializer.serialize and Serializer.deserialize methods for difficult types
Write and read other types methods:
* `void writeArray(Array)` / `Array readArray(Type)` with generic analogs
* `void writeCollectionObject(object)` / `object readCollectionObject(Type)`
* `void writeObject(object)` / `object readObject()` for difficultObject's (by using BinaryFormatter)

## Serializer
High level serialization class, with supports caching, serialize/deserialize methods save, custom methods builders
It's better to use this for difficult types, which aren't SerializeStream main types (so it's better for arrays and collections)

#### Cache
* `bool isCached(Type)` returns true if type's serialization methods are in cache
* `bool getCached(Type, out ISerializationMethods methods)` returns **true** and **methods = serialization methods container** if type's serialization methods are in cache else **false**
* `bool cache(SerializationMethods.SerializeMethod, SerializationMethods.DeserializeMethod, Type)` adds serialize and deserialize methods in cache, returns false, if methods for type are already in cache.
* `bool cache(this ISerializationMethods, Type)` cache function for methods in **ISerializationMethods** shell 

#### Prebuild serialization methods
* `bool buildAndCacheIntegrated(Type)` (builds and caches type serialization methods with integrated **SimpleSerializationMethodsBuilder** (only for arrays and collections)) returns true if success
* `bool buildAndCache(Type)` builds and caches type serialization methods with custom serializationMethodsBuilder

#### Serialization/deserialization
* `serializeSafe/deserializeSafe and generics analogs` returns true, if obejct was successfully serialized/deserialized
* `serialize/deserialize` if serialization/deserialization fail occured, thrwos SerializationException
So Serializer methods has this SerializeStream arg, and because you can write `stream.serialize(myObject, typeof(MyObject))`

## Example
[Example code for low and hight levels serialization/deserialization](https://github.com/AndreevNikita/NetBinSerializer/blob/master/NetBinSerializer/Test/Program.cs)