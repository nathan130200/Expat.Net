using System.Diagnostics;

using static Expat.PInvoke;

namespace Expat.Test;

public class LibraryTests
{
	readonly struct NativeXmlParser : IDisposable
	{
		static volatile int g_Counter = 1;

		readonly int _counter;
		readonly nint _value;

		public NativeXmlParser() : this("UTF-8")
		{

		}

		public NativeXmlParser(string? encoding)
		{
			_value = XML_ParserCreate(encoding);
			Assert.That(_value, Is.Not.EqualTo(0));
			_counter = g_Counter++;

			Console.WriteLine("NativeXmlParser::NativeXmlParser(): Create #" + _counter + " parser");
		}

		public nint Handle => _value;

		public void Dispose()
		{
			XML_ParserFree(_value);
			Console.WriteLine("NativeXmlParser::~NativeXmlParser(): Dispose #" + _counter + " parser");
		}

		public static implicit operator nint(NativeXmlParser self) => self._value;
	}

	[Test]
	public void PrintExpatVersion()
	{
		var version = XML_ExpatVersion();
		Console.WriteLine("Expat version: " + version);
	}

	[Test]
	[Repeat(10)]
	public void PrintErrors()
	{
		var watch = Stopwatch.StartNew();

		for (int i = 0; i <= 64; i++)
			_ = XML_ErrorString((XmlError)i);

		Console.WriteLine($"[PrintErrors] Run test! [Iteration {(TestContext.CurrentContext.CurrentRepeatCount + 1),2}] " +
			$"Elapsed time: {watch.Elapsed.TotalMilliseconds:F2} ms");
	}

	[Test]
	[TestCase("UTF-8")]
	[TestCase("US-ASCII")]
	[TestCase("ISO-8859-1")]
	[TestCase("UTF-16")]
	[TestCase("UTF-16BE")]
	[TestCase("UTF-16LE")]
	public void CreateParserWithKnownEncoding(string encodingName)
	{
		using var parser = new NativeXmlParser(encodingName);
		Assert.That(parser.Handle, Is.Not.EqualTo(0));
		Console.WriteLine($"Parser instance: 0x{parser.Handle:x8}");
	}

	static readonly byte[] SampleXml = "<foo bar='baz' xmlns='urn:xml:test' />"u8.ToArray();

	[Test]
	public void TryParseWithInvalidEncoding()
	{
		using var parser = new NativeXmlParser("BOOH!");

		Assert.That(XML_Parse(parser, SampleXml, SampleXml.Length, false), Is.EqualTo(XmlStatus.Error));

		var code = XML_GetErrorCode(parser);

		Assert.That(code,
			Is.EqualTo(XmlError.UnknownEncoding) |
			Is.EqualTo(XmlError.IncorrectEncoding));

		Console.WriteLine("ERROR: " + code);
	}

	[Test]
	public void BasicXmlParsing()
	{
		using var parser = new NativeXmlParser("UTF-8");

		var result = XML_Parse(parser, SampleXml, SampleXml.Length, true);
		var error = XML_GetErrorCode(parser);

		Assert.That(result, Is.EqualTo(XmlStatus.Success));

		Console.WriteLine("result: " + result);

		Assert.That(error, Is.EqualTo(XmlError.None));

		Console.WriteLine("error: " + error);

		var attr = XML_GetSpecifiedAttributeCount(parser);

		Assert.That(attr / 2, Is.EqualTo(2));
	}

	[Test]
	[TestCase(true)]
	[TestCase(false)]
	public void CantParseWhileSuspended(bool resumable)
	{
		using var parser = new NativeXmlParser("UTF-8");

		var sample = "<foo>"u8.ToArray();

		var status = XML_Parse(parser, sample, sample.Length, false);

		if (status == XmlStatus.Error)
			Assert.Fail(XML_GetErrorCode(parser).Message);

		Assert.That(status, Is.EqualTo(XmlStatus.Success));

		if (XML_StopParser(parser, resumable) == XmlStatus.Error)
			Assert.Fail("Cannot stop parser");

		Console.WriteLine("Stopped parser.");

		sample = "<bar/>"u8.ToArray();

		status = XML_Parse(parser, sample, sample.Length, false);
		Assert.That(status, Is.EqualTo(XmlStatus.Error));

		if (!resumable)
		{
			var code = XML_GetErrorCode(parser);
			Assert.That(code, Is.EqualTo(XmlError.Finished));
			Console.WriteLine("Cant resume parser (resumable = false)");
		}
		else
		{
			status = XML_ResumeParser(parser);
			Assert.That(status, Is.EqualTo(XmlStatus.Success));
			Console.WriteLine("Can resume parser (resumable = true)");
		}
	}
}
