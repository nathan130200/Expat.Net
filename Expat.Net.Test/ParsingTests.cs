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

		var result = parser.TryParse(sample, sample.Length, out var error);

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

		var exception = Assert.Throws<ExpatException>(() => parser.Parse(str, str.Length));

		var code = (XmlError)exception.Data["Code"]!;

		Assert.That(code, Is.EqualTo(XmlError.InvalidToken));
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

		// CDATA also need toplevel start tag
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

	[Test]
	public async Task ParsePI()
	{
		string target = "foo", data = "bar";

		// CDATA also need toplevel start tag
		var buf = Encoding.ASCII.GetBytes($"<?{target} {data}?>");

		using var parser = new XmlParser();

		var tcs = new TaskCompletionSource<(string target, string data)>();

		parser.OnProcessingInstruction += (target, data) =>
		{
			tcs.TrySetResult((target, data));
		};

		parser.Parse(buf, buf.Length);

		var result = await tcs.Task;

		Assert.That(result.target, Is.EqualTo(target));
		Assert.That(result.data, Is.EqualTo(data));

		Console.WriteLine("PI: target=" + result.target + ", data=" + result.data);
	}
}
