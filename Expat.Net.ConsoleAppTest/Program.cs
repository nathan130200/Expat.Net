using Expat;

int depth = 0;

using var parser = new XmlParser(XmlParserOptions.Default);

parser.OnProlog += (version, encoding, standalone) =>
{
	Console.ForegroundColor = ConsoleColor.DarkGray;

	Console.Write(new string('.', depth));

	Console.ForegroundColor = ConsoleColor.DarkBlue;

	Console.WriteLine("prolog: version={0},encoding={1},standalone={2}", version, encoding, standalone);
};

parser.OnProcessingInstruction += (target, data) =>
{
	Console.ForegroundColor = ConsoleColor.DarkGray;

	Console.Write(new string('.', depth));

	Console.ForegroundColor = ConsoleColor.Blue;

	Console.WriteLine("pi: target={0}, data={1}", target, data);
};

parser.OnStartElement += (name, atts) =>
{
	Console.ForegroundColor = ConsoleColor.DarkGray;

	Console.Write(new string('.', depth));

	Console.ForegroundColor = ConsoleColor.Magenta;

	Console.WriteLine("start element: " + name);

	depth++;

	foreach (var (key, value) in atts)
	{
		Console.ForegroundColor = ConsoleColor.DarkGray;

		Console.Write(new string('.', depth));

		Console.ForegroundColor = ConsoleColor.Cyan;

		Console.WriteLine("attribute: {0} -> {1}", key, value);
	}

	depth++;

	Console.ForegroundColor = ConsoleColor.Gray;
};

parser.OnEndElement += (name) =>
{
	depth -= 2;

	Console.ForegroundColor = ConsoleColor.DarkGray;

	Console.Write(new string('.', depth));

	Console.ForegroundColor = ConsoleColor.DarkMagenta;

	Console.WriteLine("end element: " + name);

	Console.ForegroundColor = ConsoleColor.Gray;
};

parser.OnCdata += value =>
{
	Console.ForegroundColor = ConsoleColor.DarkGray;

	Console.Write(new string('.', depth));

	Console.ForegroundColor = ConsoleColor.Red;

	Console.WriteLine("cdata: {0}", value.Trim());

	Console.ForegroundColor = ConsoleColor.Gray;
};

parser.OnComment += value =>
{
	Console.ForegroundColor = ConsoleColor.DarkGray;

	Console.Write(new string('.', depth));

	Console.ForegroundColor = ConsoleColor.Green;

	Console.WriteLine("comment: {0}", value.Trim());

	Console.ForegroundColor = ConsoleColor.Gray;
};

parser.OnText += value =>
{
	//if (string.IsNullOrWhiteSpace(value)) return;

	Console.ForegroundColor = ConsoleColor.DarkGray;

	Console.Write(new string('.', depth));

	Console.ForegroundColor = ConsoleColor.Yellow;

	Console.WriteLine("text: {0}", value.Trim());

	Console.ForegroundColor = ConsoleColor.Gray;
};

using var stream = File.OpenRead("sample.xml");

var buf = new byte[1024];

int len;

while (true)
{
	len = await stream.ReadAsync(buf);

	if (len == 0) break;

	parser.Parse(buf.AsSpan(0, len));
}

Console.ForegroundColor = ConsoleColor.Gray;