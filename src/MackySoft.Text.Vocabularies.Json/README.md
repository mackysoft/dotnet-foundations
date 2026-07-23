# MackySoft.Text.Vocabularies.Json

[![NuGet](https://img.shields.io/nuget/v/MackySoft.Text.Vocabularies.Json?label=MackySoft.Text.Vocabularies.Json)](https://www.nuget.org/packages/MackySoft.Text.Vocabularies.Json) [![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg)](https://github.com/mackysoft/ucli/blob/master/LICENSE)

`MackySoft.Text.Vocabularies.Json` reads and writes declared vocabulary values as JSON strings and dictionary property names.

The package targets `netstandard2.1` and depends on `MackySoft.Text.Vocabularies` and `System.Text.Json`. Vocabulary discovery, validation, resolution, and enumeration remain owned by the core package.

## Installation

Install a pinned version from nuget.org:

```bash
dotnet add package MackySoft.Text.Vocabularies.Json --version <version>
```

## Register the Converter

```csharp
using System.Text.Json;
using MackySoft.Text.Vocabularies.Json;

var options = new JsonSerializerOptions();
options.Converters.Add(new VocabularyJsonConverterFactory());
```

With a vocabulary such as `DeploymentMode`, the converter writes values as strings:

```csharp
string json = JsonSerializer.Serialize(DeploymentMode.Safe, options);
// "safe"
```

It also supports vocabulary values as dictionary keys:

```csharp
var values = new Dictionary<DeploymentMode, int>
{
    [DeploymentMode.Advanced] = 1,
};

string json = JsonSerializer.Serialize(values, options);
// {"advanced":1}
```

## Failure Contract

JSON values must use the string token type. Property keys must be JSON property names. Unknown text and undeclared typed values fail with `JsonException`.

An invalid vocabulary declaration is a core configuration error and remains an `InvalidOperationException` while the converter is resolved.

The adapter does not trim, ignore case, resolve aliases, generate JSON Schema, or canonicalize JSON documents.

## Repository and Support

Source, issues, and support are available in the [uCLI repository](https://github.com/mackysoft/ucli).

## License

This package is under the [MIT License](https://github.com/mackysoft/ucli/blob/master/LICENSE).
