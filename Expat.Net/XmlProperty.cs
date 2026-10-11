namespace Expat;

public enum XmlProperty : int
{
    None = 0,
    AllocationTrackerActivationThreshold = 1,
    AllocationTrackerTrackerMaximumAmplification = 2,
    BillionLaughsActivationThreshold = 3,
    BillionLaughsMaximumAmplification = 4,
    ReparseDeferralEnabled = 5
}

internal enum XmlPropertyResult : int
{
    None = 0,
    InvalidKey = 1,
    InvalidType = 2,
    InvalidValue = 3,
    ParserIsNull = 4,
    ParserIsNotRoot = 5
}