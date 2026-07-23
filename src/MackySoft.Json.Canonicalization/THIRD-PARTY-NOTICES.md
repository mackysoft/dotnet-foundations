# Third-party notices

## Embedded ECMAScript number serialization

This package contains a source-level copy of the C# number serializer from
[`cyberphone/json-canonicalization`](https://github.com/cyberphone/json-canonicalization)
at commit `19d51d7fe467d4706a3ff08adf8a748f29fc21e0`, under
`dotnet/es6numberserializer`.

The copied files are kept under
`Internal/Es6NumberSerialization`. `NumberToJson` was made internal so the
embedded implementation does not become part of this package's public API.
The nullable return annotation and descriptive comments were adapted to the
containing project. The repository formatter and analyzers were applied,
including accessibility, type spelling, brace and parentheses normalization,
and unused import cleanup. The upstream algorithmic assignments are retained.
The corresponding source form remains available in the
[`MackySoft.Json.Canonicalization` source directory](https://github.com/mackysoft/dotnet-foundations/tree/master/src/MackySoft.Json.Canonicalization).

The upstream repository declares Anders Rundgren's contributions under the
Apache License 2.0:

Copyright 2018 Anders Rundgren

See [`licenses/Apache-2.0.txt`](licenses/Apache-2.0.txt).

Individual embedded files retain additional upstream notices and license terms
in their source headers:

### V8-derived files

Applies to `NumberCachedPowers.cs`, `NumberDiyFp.cs`,
`NumberDoubleHelper.cs`, and `NumberFastDToA.cs`.

Copyright 2010 the V8 project authors. All rights reserved.

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are met:

- Redistributions of source code must retain the above copyright notice,
  this list of conditions and the following disclaimer.
- Redistributions in binary form must reproduce the above copyright notice,
  this list of conditions and the following disclaimer in the documentation
  and/or other materials provided with the distribution.
- Neither the name of Google Inc. nor the names of its contributors may be
  used to endorse or promote products derived from this software without
  specific prior written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE
LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN
CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE
POSSIBILITY OF SUCH DAMAGE.

### Mozilla Public License files

`NumberDToA.cs` and `NumberFastDToABuilder.cs` are subject to the Mozilla
Public License 2.0. See [`licenses/MPL-2.0.txt`](licenses/MPL-2.0.txt).

### David M. Gay decimal conversion notice

`NumberDToA.cs` also retains this notice:

The author of this software is David M. Gay.

Copyright (c) 1991, 2000, 2001 by Lucent Technologies.

Permission to use, copy, modify, and distribute this software for any purpose
without fee is hereby granted, provided that this entire notice is included in
all copies of any software which is or includes a copy or modification of this
software and in all copies of the supporting documentation for such software.

THIS SOFTWARE IS BEING PROVIDED "AS IS", WITHOUT ANY EXPRESS OR IMPLIED
WARRANTY. IN PARTICULAR, NEITHER THE AUTHOR NOR LUCENT MAKES ANY
REPRESENTATION OR WARRANTY OF ANY KIND CONCERNING THE MERCHANTABILITY OF THIS
SOFTWARE OR ITS FITNESS FOR ANY PARTICULAR PURPOSE.

### WebPKI number-to-JSON entry point

`NumberToJson.cs` is:

Copyright 2006-2018 WebPKI.org (http://webpki.org).

It is licensed under the Apache License 2.0. See
[`licenses/Apache-2.0.txt`](licenses/Apache-2.0.txt).
