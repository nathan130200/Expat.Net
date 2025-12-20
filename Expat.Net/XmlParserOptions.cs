using System.Text;

namespace Expat;

public class XmlParserOptions
{
	/// <summary>
	/// Default expat parsing options.
	/// </summary>
	public static XmlParserOptions Default { get; } = new()
	{
		Encoding = Encoding.UTF8,
		HashSalt = 0
	};

	/// <summary>
	/// Specifies a character encoding to use for the document. If this value is <see langword="null" /> it defaults to <see cref="Encoding.UTF8"/>.
	/// </summary>
	public Encoding Encoding
	{
		get => field ?? Encoding.UTF8;
		init => field = value;
	}

	/// <summary>
	/// Sets number of output bytes (including amplification from entity expansion and reading DTD files) needed to activate protection against billion laughs attacks (default: <c>8 MiB</c>)
	/// </summary>
	public ulong? BillionLaughsAttackProtectionActivationThreshold { get; init; }

	/// <summary>
	/// Sets the maximum tolerated amplification factor for protection against billion laughs attacks (default: <c>100</c>)
	/// <para>
	/// Once the threshold for activation is reached, the amplification factor is calculated as:
	/// <code>amplification := (direct + indirect) / direct</code>
	/// </para>
	/// </summary>
	public float? BillionLaughsAttackProtectionMaximumAmplification { get; init; }

	/// <summary>
	/// This enables parsing of parameter entities, including the external parameter entity that is the external DTD subset.
	/// </summary>
	public XmlEntityParsing? EntityParsing { get; init; }

	/// <summary>
	/// Sets the hash salt to use for internal hash calculations. Helps in preventing DoS attacks based on predicting hash function behavior. In order to have an effect this must be called before parsing has started.
	/// </summary>
	public ulong HashSalt { get; init; }
}
