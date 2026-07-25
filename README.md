# Expat.Net

Unofficial .NET wrapper for [expat](https://github.com/libexpat/libexpat) library, a fast stream-oriented XML parser library written in C.

## XmlParser API Model

`XmlParser` exposes a SAX-like callback model:

* `OnProlog`
* `OnProcessingInstruction`
* `OnStartElement`
* `OnText`
* `OnCdata`
* `OnComment`
* `OnEndElement`

> `XmlParser` class is not thread-safe concurrent interactions with parser may corrupt unmanaged state and cause memory access violations!

### Native Library Resolution Priority

1. Look for `EXPAT_LIBRARY_PATH` environment var (absolute path to expat library with file name and its extension, eg: `C:\vcpkg\installed\x64-windows\bin\libexpat.dll`).

1. Find common expat name and extensions for each platform.

1. Fallbak to default .NET library loader.

## Notes

- There is an explicit intention to keep this project aligned with the latest official libexpat releases..