# Expat.Net

An unofficial, high-performance .NET wrapper for [libexpat](https://github.com/libexpat/libexpat), a fast, stream-oriented XML parser written in C.

Expat.Net provides direct access to Expat's event-driven XML parsing capabilities through a lightweight, .NET-friendly API, with an emphasis on performance, minimal allocations, and fine-grained control over the parsing process.

## Overview

Starting with version **2.9.0**, Expat.Net adopts a near-zero-allocation approach to XML parsing.

Instead of converting native callback data into managed strings, the wrapper receives raw pointers from Expat and exposes their contents to .NET event handlers as `ReadOnlySpan<byte>`, avoiding unnecessary allocations and string conversions.

The goal is **not to outperform or replace `System.Xml.XmlReader`**, which remains an excellent choice for general-purpose XML processing.

Expat.Net is designed for scenarios where developers need greater control over how XML is consumed, particularly when working with:

- Custom network protocols and binary-framed XML messages.
- Incremental parsing of arbitrarily sized input chunks.
- XML streams embedded within other data formats.
- Event-driven XML processing without building a DOM.
- Performance-sensitive applications where minimizing managed allocations is important.

## API Model

The API is centered around two primary classes: `XmlParserOptions` and `XmlParser`.

### XmlParserOptions

The `XmlParserOptions` class configures the behavior of the underlying Expat parser.

| Property / Method | Description |
|---|---|
| `Encoding` | Specifies one of Expat's built-in supported encodings through the `XmlEncoding` enumeration, or uses `XmlEncoding.Default`. |
| `NamespaceSeparatorChar` | Optional namespace separator character. When specified, enables Expat's namespace processing functionality. |
| `IgnoreWhitespace` | When enabled, suppresses text callbacks containing only whitespace. |
| `HashSaltFactory` | Optional factory used to generate the hash salt for Expat's internal hash calculations, helping protect against hash collision DoS attacks. |
| `DefaultHashSaltFactory` | Provides a cryptographically secure hash salt generator using `RandomNumberGenerator.Fill`, producing 16 bytes of random entropy. |

### XmlParser

The `XmlParser` class manages the native parser instance and exposes an event-driven API for processing XML content.

#### Available Events

| Event | Description |
|---|---|
| `OnProlog` | Raised when an XML declaration is parsed. |
| `OnProcessingInstruction` | Raised when a processing instruction is encountered. |
| `OnStartElement` | Raised when an opening element tag is parsed. |
| `OnEndElement` | Raised when a closing element tag is parsed. |
| `OnStartNamespaceDeclaration` | Raised when a namespace declaration enters scope. Requires namespace processing to be enabled. |
| `OnEndNamespaceDeclaration` | Raised when a namespace declaration leaves scope. Requires namespace processing to be enabled. |
| `OnText` | Raised when character data is parsed. Whitespace-only content may be suppressed depending on parser options. |
| `OnCdata` | Raised when CDATA content is parsed. |
| `OnComment` | Raised when an XML comment is encountered. |

### Memory and Callback Lifetime

Expat.Net minimizes managed allocations by exposing native callback data through `ReadOnlySpan<byte>` rather than allocating managed strings for each event.

This design provides several advantages:

- Avoids unnecessary UTF-8-to-UTF-16 conversions.
- Reduces garbage collector pressure during parsing.
- Allows callers to process XML data directly as UTF-8 bytes.
- Gives applications control over when managed copies or string allocations are necessary.

**Important:** Callback spans reference memory whose lifetime is controlled by the native parser. They must be consumed during the callback and must not be retained for later use.

Applications that need to preserve callback data should explicitly copy it into managed storage.

### Thread Safety

> [!WARNING]
> `XmlParser` is **not thread-safe**.
>
> Concurrent operations on the same parser instance are unsupported and may corrupt unmanaged state or result in native memory access violations.
>
> Ensure that parsing operations and parser lifecycle management are properly synchronized when using the parser across multiple threads.

### Exception Handling

> [!NOTE]
> Exceptions thrown by managed event handlers are captured during native callback execution and propagated after the active `Parse(...)` invocation terminates.
>
> This prevents managed exceptions from unwinding directly through unmanaged Expat callback frames.

## Design Philosophy

Expat.Net intentionally maintains a relatively thin abstraction over the native Expat library.

Its primary design goals are:

**Minimal overhead:** Avoid unnecessary allocations, conversions, and intermediate representations between native callbacks and managed event handlers.

**Explicit control:** Allow applications to determine exactly which input buffers are submitted to the parser and when parsing occurs.

**Native performance:** Preserve the characteristics of Expat's event-driven architecture without introducing an additional XML processing layer.

**Predictable behavior:** Keep parsing operations explicit and avoid hidden buffering or automatic document materialization.

**Compatibility:** Maintain close alignment with the upstream libexpat library and its supported functionality.

## Versioning

Expat.Net intentionally keeps its package version aligned with the corresponding upstream **libexpat** release.

For example, Expat.Net `2.9.0` targets the corresponding libexpat `2.9.0` release.

Pre-release builds use the `-preview` suffix to indicate that the wrapper version is still under development and may receive additional fixes or API adjustments before its stable release.

## Benchmarks

The following benchmarks compare Expat.Net against .NET's built-in `XmlReader`, measuring parsing performance and managed memory allocations across different parsing strategies.

```cs
BenchmarkDotNet v0.15.8, Windows 10 (10.0.19045.6937/22H2/2022Update)
AMD Ryzen 5 5600GT with Radeon Graphics 3.60GHz, 1 CPU, 12 logical and 6 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1
WarmupCount=3

| Type                        | Method                    | StringAllocation | Mean      | Error      | StdDev   | Gen0    | Gen1   | Allocated |
|---------------------------- |-------------------------- |----------------- |----------:|-----------:|---------:|--------:|-------:|----------:|
| ExpatSimpleParserBenchmark  | Parse_Buffered            | ?                | 485.80 us | 144.864 us | 7.940 us |       - |      - |     688 B |
| ExpatSimpleParserBenchmark  | Parse_InMemory            | ?                | 278.75 us |  67.465 us | 3.698 us |       - |      - |     144 B |
| ExpatInlineParserBenchmark  | Parsing_Bytes             | False            | 248.98 us |  18.702 us | 1.025 us |       - |      - |     144 B |
| ExpatRecycleParserBenchmark | Parsing_Bytes             | False            | 284.16 us | 111.644 us | 6.120 us |       - |      - |         - |
| XmlReaderBenchmark          | Parsing_Buffered_InMemory | False            |  44.22 us |   5.336 us | 0.292 us |  2.2583 | 0.1221 |   18968 B |
| ExpatInlineParserBenchmark  | Parsing_Buffered          | False            | 277.32 us |  39.506 us | 2.165 us |       - |      - |     208 B |
| ExpatRecycleParserBenchmark | Parsing_Buffered          | False            | 271.92 us |  18.689 us | 1.024 us |       - |      - |      64 B |
| XmlReaderBenchmark          | Parsing_Buffered_FromFile | False            | 222.98 us |  32.592 us | 1.786 us |  1.9531 |      - |   19377 B |
| ExpatInlineParserBenchmark  | Parsing_Bytes             | True             | 326.90 us |  68.534 us | 3.757 us | 10.7422 |      - |   90056 B |
| ExpatRecycleParserBenchmark | Parsing_Bytes             | True             | 354.70 us |  40.439 us | 2.217 us | 10.7422 |      - |   89912 B |
| XmlReaderBenchmark          | Parsing_Buffered_InMemory | True             |  57.59 us |  11.646 us | 0.638 us |  6.5308 | 0.4272 |   54840 B |
| ExpatInlineParserBenchmark  | Parsing_Buffered          | True             | 354.17 us |  78.851 us | 4.322 us | 10.7422 |      - |   90120 B |
| ExpatRecycleParserBenchmark | Parsing_Buffered          | True             | 350.43 us | 182.100 us | 9.982 us | 10.7422 |      - |   89976 B |
| XmlReaderBenchmark          | Parsing_Buffered_FromFile | True             | 241.69 us | 111.376 us | 6.105 us |  6.3477 |      - |   55250 B |
```

**Key observations:**

- **Memory efficiency:** Expat.Net achieves near-zero managed allocations when string allocation is disabled, with the recycled parser requiring no managed allocations in the in-memory scenario.

- **"String allocations":** Enabling string allocations increases memory usage and execution time, highlighting the benefits of processing UTF-8 data directly through `ReadOnlySpan<byte>`.

- **Parser reuse:** Reusing an existing parser instance reduces allocation overhead, making it suitable for repeated parsing operations.