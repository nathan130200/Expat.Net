namespace Expat;

public sealed class ExpatException : Exception
{
	internal ExpatException()
	{

	}

	internal ExpatException(string message) : base(message)
	{

	}

	internal ExpatException(string message, Exception innerException) : base(message, innerException)
	{

	}
}