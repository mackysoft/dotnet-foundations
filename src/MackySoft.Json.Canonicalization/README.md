# MackySoft.Json.Canonicalization

`MackySoft.Json.Canonicalization` provides product-independent RFC 8785 JSON
canonicalization for .NET. It targets `netstandard2.1`.

The package accepts UTF-8 JSON or `System.Text.Json.JsonElement` values through
`Rfc8785JsonCanonicalizer` and returns an owned canonical UTF-8 byte array.
Consumers pass those bytes to the digest algorithm or comparison mechanism
required by their own contracts. General hashing, JSON Schema generation,
product-specific semantic validation, and artifact storage are outside this
package.

## Input and output contract

The raw UTF-8 entry point accepts one JSON value without a UTF-8 byte-order
mark. It rejects invalid UTF-8, comments, trailing commas, additional top-level
values, duplicate decoded property names, unpaired UTF-16 surrogates, and
numbers outside the finite IEEE 754 binary64 domain. It also rejects negative
zero in accordance with verified RFC 8785 erratum 7920.

JSON number tokens are interpreted as binary64 values before ECMAScript number
serialization. Distinct decimal tokens that round to the same binary64 value
therefore produce the same canonical bytes.

Raw input has a maximum nesting depth of 64. The package does not impose a
byte-size limit; transports and file readers must apply their own limit before
canonicalization. The `JsonElement` entry point operates on the already parsed
JSON value, so syntax and parser-depth policy remain the responsibility of the
parser that created the element.

Every successful call returns a newly allocated `byte[]`. The caller owns that
array, and its lifetime is independent of the input buffer or source
`JsonDocument`.

Contract violations throw `JsonCanonicalizationException`. Its `FailureKind`
is one of these CLR enum members:

- `InvalidJson`
- `DuplicateProperty`
- `InvalidUnicode`
- `NumberNotRepresentable`
- `NegativeZero`
- `MaximumDepthExceeded`

These enum member names classify failures inside .NET. This package does not
define a serialized text token or JSON converter for them.

## Embedded number serialization

ECMAScript-compatible number serialization is built from the C# source in the
RFC 8785 authors' official
[`cyberphone/json-canonicalization`](https://github.com/cyberphone/json-canonicalization)
repository at pinned commit
`19d51d7fe467d4706a3ff08adf8a748f29fc21e0`. The implementation is embedded
as an internal detail and does not add another public API or NuGet dependency.

The copied source retains its original headers. The package includes
[`THIRD-PARTY-NOTICES.md`](THIRD-PARTY-NOTICES.md), the Apache License 2.0,
and the Mozilla Public License 2.0 that apply to the upstream contributions.
