using System.Security.Cryptography;

namespace Expat;

public sealed class XmlParserOptions
{
	public static XmlParserOptions Default { get; } = new();

	static void DefaultHashSaltFactory(Span<byte> buf)
	{
		RandomNumberGenerator.Fill(buf);
	}

	XmlParserOptions()
	{
		Encoding = XmlEncoding.Utf8;
		ParamEntityParsing = XmlParamEntityParsing.Never;
		HashSaltFactory = DefaultHashSaltFactory;
		ShouldEmitInputContext = false;
	}

	public XmlParserOptions(XmlParserOptions other)
	{
		Encoding = other.Encoding;
		ParamEntityParsing = other.ParamEntityParsing;
		BillionLaughsAttackProtectionMaximumAmplification = other.BillionLaughsAttackProtectionMaximumAmplification;
		BillionLaughsAttackProtectionActivationThreshold = other.BillionLaughsAttackProtectionActivationThreshold;
		AllocTrackerMaximumAmplification = other.AllocTrackerMaximumAmplification;
		AllocTrackerActivationThreshold = other.AllocTrackerActivationThreshold;
		HashSaltFactory = other.HashSaltFactory;
		ShouldEmitInputContext = other.ShouldEmitInputContext;
	}

	public XmlEncoding Encoding
	{
		get;
		init;
	}

	public XmlParamEntityParsing? ParamEntityParsing
	{
		get;
		init;
	}

	public float? BillionLaughsAttackProtectionMaximumAmplification
	{
		get;
		init;
	}

	public long? BillionLaughsAttackProtectionActivationThreshold
	{
		get;
		init;
	}

	public float? AllocTrackerMaximumAmplification
	{
		get;
		init;
	}

	public long? AllocTrackerActivationThreshold
	{
		get;
		init;
	}

	public HashSaltDelegate? HashSaltFactory
	{
		get;
		init;
	}

	public bool ShouldEmitInputContext
	{
		get;
		init;
	}
}

public delegate void HashSaltDelegate(Span<byte> buf);