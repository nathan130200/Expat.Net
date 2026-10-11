namespace Expat;

public delegate void PrologEventHandler(ReadOnlySpan<byte> version, ReadOnlySpan<byte> encoding, bool? standalone);

public delegate void StartElementEventHandler(ReadOnlySpan<byte> name, XmlAttributesReader attributes);

public delegate void EndElementEventHandler(ReadOnlySpan<byte> name);

public delegate void StartNamespaceDeclarationEventHandler(ReadOnlySpan<byte> prefix, ReadOnlySpan<byte> uri);

public delegate void EndNamespaceDeclarationEventHandler(ReadOnlySpan<byte> prefix);

public delegate void CharacterDataEventHandler(ReadOnlySpan<byte> content);

public delegate void CommentEventHandler(ReadOnlySpan<byte> comment);

public delegate void ProcessingInstructionEventHandler(ReadOnlySpan<byte> target, ReadOnlySpan<byte> data);