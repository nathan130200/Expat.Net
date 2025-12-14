# Expat.Net

This unofficial bindings provides a **managed wrapper built on top of libexpat**, using P/Invoke internally but **not exposing the native API directly**.

Instead, it offers a focused, idiomatic C# surface centered around a single class, `XmlParser`, which owns and manages the native Expat parser instance, its lifetime, and its callbacks.

The goal is to combine Expat’s performance and streaming model with a controlled, predictable .NET API suitable for high-throughput and long-running parsers.

## Design Overview

* Native Expat functions are accessed internally via P/Invoke
* Consumers interact only with the managed `XmlParser` class
* The native parser instance is created, configured, and destroyed by `XmlParser`
* Callbacks are marshaled into managed code in a controlled manner

## XmlParser API Model

`XmlParser` exposes a SAX-like callback model:

* `OnStartElement`
* `OnEndElement`
* `OnText`
* `OnCdata`
* `OnComment`

## Memory Management

By default expat uses system memory management functions (malloc, realloc and free), but you can override these functions implementing custom classe from base class of `MemoryHandlingSuite` class and re-implement your own memory handling 

The library has a default implementation `MemoryHandlingSuite.Default` (its just wrapper to `System.Runtime.InteropServices.Marshal` functions: `AllocHGlobal`, `ReallocHGlobal`, `FreeHGlobal`).

## Dependencies

* Native libexpat (build with vcpkg or grab in [libexpat github releases page](https://github.com/libexpat/libexpat/releases))
* .NET (C#)

## Version Alignment

There is an explicit intention to keep this project **aligned with the latest official libexpat releases**.

## Notes

The native library must be available on the system (.dll, .so, or .dylib depending on the platform).

This library implement [native library resolver](https://learn.microsoft.com/en-us/dotnet/standard/native-interop/native-library-loading) to attempt load almost all possible combinations of native library name (libexpat, expat, expat.so.1, etc...).