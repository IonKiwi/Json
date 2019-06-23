# Yet Another JSON parser

## Features

* parse/write JSON
* parse/write ECMAScript like content
	* Unquoted property names
	* Single / multi-line comments
	* Trailing comma allowed for objects and arrays
	* Single quoted strings
	* Multi-line strings (by escaping new line characters)
	* Unicode CodePoint escape
	* Hexadecimal/octal/binary numbers
	* Numbers with leading or trailing decimal point
	* Positive infinity, negative infinity, NaN
	* Explicit plus sign for numbers

* Support for C#/.NET Tuples (using Tuple Element Names)

_Automatic for tuples included in a object_

```csharp

[JsonObject]
private sealed class TupleHolder {
	[JsonProperty]
	public (bool a, int b) Value1 { get; set; }
}

json => { Value1: { a: true, b: 42 } }

```

_Manual for top level tuples or when using generics_

```csharp

JsonWriter.Serialize(
    output,
    (true, 42),
    JsonWriter.DefaultSettings.With(s => s.JsonWriteMode = JsonWriteMode.ECMAScript),
    tupleNames: new string[] { "a", "b" });
=> { a: true, b: 42 }

```

_Manual using TupleElementNamesAttribute_

```csharp

public class ClassContaingTupleMethod {
  public (string value1, bool value2) MethodReturningTuple() {
    return ("test", true);
  }
}

string json = JsonUtility.Serialize(
  new ClassContaingTupleMethod().MethodReturningTuple(),
  tupleNames: 
    typeof(ClassContaingTupleMethod).GetMethod("MethodReturningTuple", BindingFlags.Instance | BindingFlags.Public)
    .ReturnParameter.GetCustomAttribute<TupleElementNamesAttribute>().TransformNames.ToArray());
// json => {"value1":"test","value2":true}

```

## Usage

**Parsing json**

_parsing a json string synchronously_
```csharp

using (var reader = new StringReader(json)) {
  var value = JsonParser.Parse<ObjectType>(new JsonReader(reader));
}

or

var value = JsonUtility.Parse<ObjectType>(json);

```

_parsing a json string asynchronously_

```csharp

using (var reader = new StringReader(json)) {
  var value = await JsonParser.ParseAsync<ObjectType>(new JsonReader(reader));
}

or

var value = await JsonUtility.ParseAsync<ObjectType>(json);

```

_parsing a json stream synchronously_

```csharp

using (var reader = new StreamReader(stream)) {
  var value = JsonParser.Parse<ObjectType>(new JsonReader(reader));
}

or

var value = JsonUtility.Parse<ObjectType>(stream);

```

_parsing a json stream asynchronously_

```csharp

using (var reader = new StreamReader(stream)) {
  var value = await JsonParser.ParseAsync<ObjectType>(new JsonReader(reader));
}

or

var value = await JsonUtility.ParseAsync<ObjectType>(stream);

```

**Writing json**

_serializing a value as json string synchronously_
```csharp

var sb = new StringBuilder();
using (var writer = new StringWriter(sb)) {
  var value = JsonWriter.Serialize(writer, value);
}
var json = sb.ToString();

or

var json = JsonUtility.Serialize(value);

```

_serializing a value as json string asynchronously_

```csharp

var sb = new StringBuilder();
using (var writer = new StringWriter(sb)) {
  var value = await JsonWriter.SerializeAsync(writer, value);
}
var json = sb.ToString();

or

var json = await JsonUtility.SerializeAsync(value);

```

_serializing a value to a stream synchronously_

```csharp

using (var writer = new StreamWriter(stream)) {
  JsonWriter.Serialize(writer, value);
}

or

JsonUtility.Serialize(stream, value);

```

_serializing a value to a stream asynchronously_

```csharp

using (var writer = new StreamWriter(stream)) {
  await JsonWriter.SerializeAsync(writer, value);
}

or

await JsonUtility.SerializeAsync(stream, value);

```


## Annotation

**Objects**

_use IonKiwi.Json.MetaData.JsonObjectAttribute & IonKiwi.Json.MetaData.JsonPropertyAttribute_

```csharp

[JsonObject]
public class Object1 {

	[JsonProperty]
	public string Property1 { get; set; }
}

```

**Collections**

_use IonKiwi.Json.MetaData.JsonCollectionAttribute and implement IEnumerable<>_

```csharp

[JsonCollection]
public class Collection1<T> : IEnumerable<T> {
}

```

**Dictionaries**

_use IonKiwi.Json.MetaData.JsonDictionaryAttribute and implement IDictionary<,>_

```csharp

[JsonDictionary]
public class Dictionary1<TKey, TValue> : IDictionary<TKey, TValue> {
}

```

**External/existing annotation**

_use existing DataContract/DataMember attributes (System.Runtime.Serialization)_

```csharp

IonKiwi.Json.Utilities.DataContractSupport.Register();

```

_use existing Newtonsoft attributes_

```csharp

IonKiwi.Json.Newtonsoft.NewtonsoftSupport.Register();

```

**Custom constructors**

_use IonKiwi.Json.MetaData.JsonConstructorAttribute & IonKiwi.Json.MetaData.JsonParameterAttribute_

```csharp

[JsonObject]
private class Object2 {

	[JsonConstructor]
	public Object2(bool property1, int property2, [JsonParameter("Property3")]int property3) {
		Property1 = property1;
		Property2 = property2;
	}

	[JsonProperty(Name = "property1")]
	public bool Property1 { get; }

	[JsonProperty(Name = "property2", Required = false)]
	public int Property2 { get; }

	[JsonProperty]
	public int Property3 { get; }
}

```

For non required properties, the default value will be used.
You can declare multiple [JsonConstructor] constructors, the one with the most available parameters will be called.
