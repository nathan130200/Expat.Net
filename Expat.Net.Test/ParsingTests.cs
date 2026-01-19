using System.Diagnostics;
using System.Text;

namespace Expat.Test;

public class ParsingTests
{
	[Test]
	public void TestSimpleXml()
	{
		var sample = "<foo xmlns='bar'/>"u8.ToArray();

		using var parser = new XmlParser();

		var tcs = new TaskCompletionSource();

		parser.OnStartTag += (name, attrs) =>
		{
			Assert.Multiple(() =>
			{
				try
				{
					Assert.That(name, Is.EqualTo("foo"));
					Assert.That(attrs, Has.Count.EqualTo(1));
					Assert.That(attrs["xmlns"], Is.EqualTo("bar"));
				}
				finally
				{
					tcs.TrySetResult();
				}
			});
		};

		var (result, error) = parser.TryParse(sample, sample.Length);

		Console.WriteLine("status: " + result);
		Console.WriteLine("error: " + error + " (" + error.Message + ")");

		Assert.Multiple(() =>
		{
			Assert.That(result, Is.True);
			Assert.That(error, Is.EqualTo(XmlError.None));
		});

		tcs.Task.Wait();
	}

	[Test]
	public void ParseInvalidXml()
	{
		var str = "<foo xmlns='&'/"u8.ToArray();

		using var parser = new XmlParser();

		var exception = Assert.Throws<ExpatException>(() => parser.Parse(str, str.Length, true));

		Assert.Multiple(() =>
		{
			Assert.That(exception, Is.Not.Null);
			Assert.That(exception.Code, Is.EqualTo(XmlError.InvalidToken));
		});
	}

	[Test]
	public void TryParseInvalidXml()
	{
		var str = "<foo xmlns='&'/"u8.ToArray();

		using var parser = new XmlParser();

		var (result, error) = parser.TryParse(str, str.Length, true);

		Console.WriteLine("result: " + result);
		Console.WriteLine("error: " + error);

		Assert.Multiple(() =>
		{
			Assert.That(result, Is.False);
			Assert.That(error, Is.EqualTo(XmlError.InvalidToken));
		});
	}

	[Test]
	public async Task ParseStartTag()
	{
		var tagName = "stream:stream";

		var buf = Encoding.ASCII.GetBytes("<" + tagName + ">");

		using var parser = new XmlParser();

		var tcs = new TaskCompletionSource<string>();

		parser.OnStartTag += (name, _) => tcs.TrySetResult(name);

		parser.Parse(buf, buf.Length);

		var result = await tcs.Task;

		Assert.That(result, Is.EqualTo(tagName));
	}

	[Test]
	public async Task ParseEndTag()
	{
		var name = "stream:stream";

		var buf = Encoding.ASCII.GetBytes($"<{name}></{name}>"); // need at least open tag first.

		using var parser = new XmlParser();

		var tcs = new TaskCompletionSource<string>();

		parser.OnEndTag += (name) => tcs.TrySetResult(name);

		parser.Parse(buf, buf.Length);

		var result = await tcs.Task;

		Assert.That(result, Is.EqualTo(name));
	}

	[Test]
	public async Task ParseText()
	{
		var text = "Hello World";

		// need at least toplevel start tag
		var buf = Encoding.ASCII.GetBytes("<root>" + text + "</root>");

		using var parser = new XmlParser();

		var tcs = new TaskCompletionSource<string>();

		parser.OnText += value =>
		{
			tcs.TrySetResult(value);
		};

		parser.Parse(buf, buf.Length);

		var result = await tcs.Task;

		Assert.That(result, Is.EqualTo(text));

		Console.WriteLine("Text: " + result);
	}

	[Test]
	public async Task ParseComment()
	{
		var text = "Hello World";
		var buf = Encoding.ASCII.GetBytes("<!--" + text + "-->");

		using var parser = new XmlParser();

		var tcs = new TaskCompletionSource<string>();

		parser.OnComment += value =>
		{
			tcs.TrySetResult(value);
		};

		parser.Parse(buf, buf.Length);

		var result = await tcs.Task;

		Assert.That(result, Is.EqualTo(text));

		Console.WriteLine("Comment: " + result);
	}

	[Test]
	public async Task ParseCdataSection()
	{
		var text = "Hello World";

		// CDATA need top level start tag
		var buf = Encoding.ASCII.GetBytes("<root><![CDATA[" + text + "]]></root>");

		using var parser = new XmlParser();

		var tcs = new TaskCompletionSource<string>();

		parser.OnCdata += value =>
		{
			tcs.TrySetResult(value);
		};

		parser.Parse(buf, buf.Length);

		var result = await tcs.Task;

		Assert.That(result, Is.EqualTo(text));

		Console.WriteLine("CDATA: " + result);
	}

	const string c_EmptyString = "";

	[Test]
	[TestCase("mso-application")]
	[TestCase("strict", "value")]
	[TestCase("not-strict", "value with spaces")]
	public async Task ParsePI(string target, string? data = "")
	{
		var sb = new StringBuilder($"<?{target}");

		if (!string.IsNullOrWhiteSpace(data))
			sb.Append(' ').Append(data);

		var xml = sb.Append("?>").ToString();

		var buf = Encoding.ASCII.GetBytes(xml);

		Console.WriteLine("Trying to parse PI: " + xml);

		using var parser = new XmlParser();

		var tcs = new TaskCompletionSource<(string target, string data)>();

		parser.OnProcessingInstruction += (target, data) =>
		{
			tcs.TrySetResult((target, data));
		};

		parser.Parse(buf, buf.Length);

		var result = await tcs.Task;

		Assert.Multiple(() =>
		{
			Assert.That(result.target, Is.EqualTo(target));
			Assert.That(result.data, Is.EqualTo(data));
		});

		Console.WriteLine("PI: target=" + result.target + ", data=" + result.data);
	}

	[Test]
	[TestCase(null, XmlStandalone.NotSet)]
	[TestCase(null, XmlStandalone.Yes)]
	[TestCase(null, XmlStandalone.No)]
	[TestCase("utf-8", XmlStandalone.NotSet)]
	[TestCase("utf-8", XmlStandalone.Yes)]
	[TestCase("utf-8", XmlStandalone.No)]
	public async Task ParseProlog(string? encoding, XmlStandalone standalone)
	{
		using var parser = new XmlParser();

		var tcs = new TaskCompletionSource<(string version, string? encoding, XmlStandalone standalone)>();

		parser.OnProlog += (version, encoding, standalone) =>
		{
			tcs.TrySetResult((version, encoding, standalone));
		};

		var sb = new StringBuilder("<?xml version='1.0'");

		if (encoding != null)
			sb.AppendFormat(" encoding='{0}'", encoding);

		if (standalone != XmlStandalone.NotSet)
			sb.AppendFormat(" standalone='{0}'", standalone == XmlStandalone.Yes ? "yes" : "no");

		var xml = sb.Append("?>\n<root/>").ToString();

		var buf = Encoding.UTF8.GetBytes(xml);

		Console.WriteLine("Trying to parse XML:\n" + xml + "\n");

		parser.Parse(buf, buf.Length, true);

		var result = await tcs.Task;

		Console.WriteLine("-- result --\nversion: {0}\nencoding:{1}\nstandalone: {2}",
			result.version, result.encoding, result.standalone);

		Assert.Multiple(() =>
		{
			Assert.That(result.version, Is.EqualTo("1.0"));
			Assert.That(result.encoding, Is.EqualTo(encoding));
			Assert.That(result.standalone, Is.EqualTo(standalone));
		});
	}

	[Test]
	[TestCase(10)]
	[TestCase(100)]
	[TestCase(1000)]
	[TestCase(10000)]
	//[TestCase(100000)] DO NOT - This took ~40 seconds to test. Collected ~270MB of text nodes parsed.
	public void BillionLaughsAttackTest(int factorScale)
	{
		var buf =
			"""
			<?xml version="1.0"?>
			<!DOCTYPE lolz [
			 <!ENTITY lol "lol">
			 <!ELEMENT lolz (#PCDATA)>
			 <!ENTITY lol1 "&lol;&lol;&lol;&lol;&lol;&lol;&lol;&lol;&lol;&lol;">
			 <!ENTITY lol2 "&lol1;&lol1;&lol1;&lol1;&lol1;&lol1;&lol1;&lol1;&lol1;&lol1;">
			 <!ENTITY lol3 "&lol2;&lol2;&lol2;&lol2;&lol2;&lol2;&lol2;&lol2;&lol2;&lol2;">
			 <!ENTITY lol4 "&lol3;&lol3;&lol3;&lol3;&lol3;&lol3;&lol3;&lol3;&lol3;&lol3;">
			 <!ENTITY lol5 "&lol4;&lol4;&lol4;&lol4;&lol4;&lol4;&lol4;&lol4;&lol4;&lol4;">
			 <!ENTITY lol6 "&lol5;&lol5;&lol5;&lol5;&lol5;&lol5;&lol5;&lol5;&lol5;&lol5;">
			 <!ENTITY lol7 "&lol6;&lol6;&lol6;&lol6;&lol6;&lol6;&lol6;&lol6;&lol6;&lol6;">
			 <!ENTITY lol8 "&lol7;&lol7;&lol7;&lol7;&lol7;&lol7;&lol7;&lol7;&lol7;&lol7;">
			 <!ENTITY lol9 "&lol8;&lol8;&lol8;&lol8;&lol8;&lol8;&lol8;&lol8;&lol8;&lol8;">
			]>
			<lolz>&lol9;</lolz>
			"""u8.ToArray();

		var numBytesParsed = 0L;

		var options = new XmlParserOptions
		{
			BillionLaughsAttackProtectionActivationThreshold = 1 << 16,
			BillionLaughsAttackProtectionMaximumAmplification = 10 * factorScale
		};

		using var parser = new XmlParser(options);

		parser.OnText += value =>
		{
			numBytesParsed += (long)Encoding.UTF8.GetByteCount(value);
		};

		var proc = Process.GetCurrentProcess();

		Console.WriteLine("Starting working set: {0:F2}", FormatByteSize(proc.WorkingSet64));

		var (result, error) = parser.TryParse(buf, buf.Length, true);

		proc.Refresh();

		Console.WriteLine("Num bytes (of text nodes) parsed: {0}", FormatByteSize(numBytesParsed));

		Assert.Multiple(() =>
		{
			Assert.That(result, Is.False);
			Assert.That(error, Is.EqualTo(XmlError.AmplificationLimitBreach));
			Console.WriteLine("[{0}] = {1}", result, error);
		});

		Console.WriteLine("[1] Post-parsing working set: {0:F2}", FormatByteSize(proc.WorkingSet64));

		proc.Refresh();

		Console.WriteLine("[2] End-parsing working set: {0:F2}", FormatByteSize(proc.WorkingSet64));

		GC.Collect();
		GC.WaitForPendingFinalizers();

		Console.WriteLine("[3] Post GC working set: {0:F2}", FormatByteSize(proc.WorkingSet64));

		static string FormatByteSize(long size)
		{
			string[] formats = ["B", "KB", "MB", "GB", "TB"];

			double mSize = (double)size;
			int index = 0;

			while (mSize > 1024d)
			{
				if (index > formats.Length - 1)
					break;

				mSize /= 1024d;
				index++;
			}

			return string.Format("{0:F2} {1}", mSize, formats[index]);
		}
	}
}
