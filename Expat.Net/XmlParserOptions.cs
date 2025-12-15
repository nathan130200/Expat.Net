using System.Text;

namespace Expat;

public class XmlParserOptions
{
	public static readonly XmlParserOptions Default = new()
	{
		Encoding = Encoding.UTF8,
		EntityParsing = XmlEntityParsing.Never,
		HashSalt = 0
	};

	public Encoding? Encoding { get; init; }
	public long? BillionLaughsAttackProtectionActivationThreshold { get; init; }
	public float? BillionLaughsAttackProtectionMaximumAmplification { get; init; }
	public XmlEntityParsing EntityParsing { get; init; }
	public ulong? HashSalt { get; init; }
}
