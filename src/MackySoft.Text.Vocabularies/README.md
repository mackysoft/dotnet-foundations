# MackySoft.Text.Vocabularies

[![NuGet](https://img.shields.io/nuget/v/MackySoft.Text.Vocabularies?label=MackySoft.Text.Vocabularies)](https://www.nuget.org/packages/MackySoft.Text.Vocabularies) [![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg)](https://github.com/mackysoft/dotnet-foundations/blob/master/LICENSE)

`MackySoft.Text.Vocabularies` defines finite, typed vocabularies whose values map one-to-one to canonical text.

The package targets `netstandard2.1` and does not depend on `System.Text.Json`. It is suitable for protocol literals, configuration values, and other machine-readable closed sets that need one exact text for each declared value.

## Installation

Install a pinned version from nuget.org:

```bash
dotnet add package MackySoft.Text.Vocabularies --version <version>
```

## Define and Resolve a Vocabulary

```csharp
using MackySoft.Text.Vocabularies;

[VocabularyDefinition]
public enum DeploymentMode
{
    [VocabularyText("safe")]
    Safe,

    [VocabularyText("advanced")]
    Advanced,
}

string text = Vocabulary.GetText(DeploymentMode.Safe);

if (Vocabulary.TryGetValue("advanced", out DeploymentMode mode))
{
    Console.WriteLine(mode);
}

foreach (VocabularyEntry<DeploymentMode> entry in Vocabulary.GetEntries<DeploymentMode>())
{
    Console.WriteLine($"{entry.Text}: {entry.Value}");
}
```

Resolution uses ordinal comparison. The package does not trim input, ignore case, resolve aliases, or normalize Unicode.

## Definition Guarantees

A vocabulary is validated on first use. Validation rejects definitions that:

- have no declared members;
- omit `VocabularyTextAttribute` from any member;
- repeat a typed value or canonical text;
- use empty, whitespace-only, leading-whitespace, or trailing-whitespace text.

`Vocabulary.Validate(Type)` validates a definition known only at runtime. `Vocabulary.IsVocabulary(Type)` discovers declared definitions and validates them without requiring a JSON adapter or another consumer to inspect member metadata.

## Non-Goals

This package does not provide descriptions, examples, JSON Schema, general text codecs, path rules, JSON canonicalization, or product-specific input policy.

For `System.Text.Json` string values and property names, use `MackySoft.Text.Vocabularies.Json`.

## Repository and Support

Source, issues, and support are available in the [MackySoft .NET Foundations repository](https://github.com/mackysoft/dotnet-foundations).

## License

This package is under the [MIT License](https://github.com/mackysoft/dotnet-foundations/blob/master/LICENSE).
