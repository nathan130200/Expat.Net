using System.Diagnostics;

using static Expat.PInvoke;

namespace Expat.Net.Test;

public class LibraryTests
{

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
		var parser = XML_ParserCreate(encodingName);
		Assert.That(parser, Is.Not.EqualTo(0));
		Console.WriteLine($"Parser instance: 0x{parser:x8}");
		XML_ParserFree(parser);
	}

	static nint CreateParser(string enc)
	{
		var parser = XML_ParserCreate(enc);
		Assert.That(parser, Is.Not.EqualTo(0));
		return parser;
	}

	static readonly byte[] SampleXml = "<foo bar='baz' xmlns='urn:xml:test' />"u8.ToArray();

	[Test]
	public void TryParseWithInvalidEncoding()
	{
		var parser = CreateParser("BOOH!");

		Assert.That(XML_Parse(parser, SampleXml, SampleXml.Length, false), Is.EqualTo(XmlStatus.Error));

		var code = XML_GetErrorCode(parser);

		Assert.That(code,
			Is.EqualTo(XmlError.UnknownEncoding) |
			Is.EqualTo(XmlError.IncorrectEncoding));

		Console.WriteLine("ERROR: " + code);

		XML_ParserFree(parser);
	}

	[Test]
	public void BasicXmlParsing()
	{
		var parser = CreateParser("UTF-8");

		var result = XML_Parse(parser, SampleXml, SampleXml.Length, true);
		var error = XML_GetErrorCode(parser);

		Assert.That(result, Is.EqualTo(XmlStatus.Success));

		Console.WriteLine("result: " + result);

		Assert.That(error, Is.EqualTo(XmlError.None));

		Console.WriteLine("error: " + error);

		var attr = XML_GetSpecifiedAttributeCount(parser);

		Assert.That(attr / 2, Is.EqualTo(2));

		XML_ParserFree(parser);
	}

	[Test]
	public void TestParsingStatus()
	{
		var parser = CreateParser("UTF-8");

		try
		{
			Report(out var result, "Before parse");

			Assert.Multiple(() =>
			{
				Assert.That(result.status, Is.EqualTo(ParsingState.Type.Initialized));
				Assert.That(result.finalBuffer, Is.True);
			});

			XML_Parse(parser, SampleXml, SampleXml.Length, false);

			Report(out result, "After Parse Sample");

			Assert.Multiple(() =>
			{
				Assert.That(result.status, Is.EqualTo(ParsingState.Type.Parsing));
				Assert.That(result.finalBuffer, Is.False);
			});

			XML_Parse(parser, [], 0, true);

			Report(out result, "End Parsing");

			Assert.Multiple(() =>
			{
				Assert.That(result.status, Is.EqualTo(ParsingState.Type.Finished));
				Assert.That(result.finalBuffer, Is.True);
			});
		}
		finally
		{
			XML_ParserFree(parser);
		}

		void Report(out ParsingState result, string? prefix = "Start")
		{
			XML_GetParsingStatus(parser, out result);

			Console.WriteLine(prefix + " | status: " + result.status);
			Console.WriteLine(prefix + " | is final buffer: " + result.finalBuffer);
			Console.WriteLine(prefix + " | -----------\n");
		}
	}
}
