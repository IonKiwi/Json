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

