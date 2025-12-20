# Expat.Net

This unofficial bindings provides a managed XML parser wrapper built on top of expat library.

The goal is to combine Expat’s performance and streaming model with .NET API, suitable for high-throughput and long-running parsers.
## Design Overview

* Consumers interact with the managed `XmlParser` class
* Native functions can be used within the `PInvoke` class (eg: extend custom features).
* Native parser pointer exposed in `XmlParser.Handle` property.

## XmlParser API Model

`XmlParser` exposes a SAX-like callback model:

* `OnProlog`
* `OnProcessingInstruction`
* `OnStartTag`
* `OnEndTag`
* `OnText`
* `OnCdata`
* `OnComment`


### Native Library Resolution Priority

1. Look for `EXPAT_LIBRARY_PATH` env var.
1. Find common expat name and extensions for each platform.
1. Fallbak to default .NET library loader.

## Notes

There is an explicit intention to keep this project aligned with the latest official libexpat releases.

The native library must be available on the system (.dll, .so, or .dylib depending on the platform).

This library implement [native library resolver](https://learn.microsoft.com/en-us/dotnet/standard/native-interop/native-library-loading) to attempt load almost all possible combinations of names (libexpat, expat) and file extensions (.dll, .so, .so.1, .dylib).

Point absolute path to expat library using `EXPAT_LIBRARY_PATH` environment variable.