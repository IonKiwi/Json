# Yet Another JSON parser

## Features

* parse/write JSON
* parse/write ECMAScript like JSON
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
	* Automatic for tuples included in a object

```
[JsonObject]
private sealed class TupleHolder {
	[JsonProperty]
	public (bool a, int b) Value1 { get; set; }
}

json => { Value1: { a: true, b: 42 } }
```

	* Manual for top level tuples or when using generics

```
JsonWriter.SerializeSync(
	output,
    (true, 42),
    JsonWriter.DefaultSettings.With(s => s.JsonWriteMode = JsonWriteMode.ECMAScript),
    tupleNames: new string[] { "a", "b" });
=> { a: true, b: 42 }
```
