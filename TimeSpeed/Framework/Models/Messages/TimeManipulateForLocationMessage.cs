using TimeSpeed.Framework.Models.Messages.Interfaces;
using TimeSpeed.Framework.Models.Enum;

#nullable enable

namespace TimeSpeed.Framework.Models.Messages;

internal class TimeManipulateForLocationMessage : ITimeManipulateForLocationMessage
{
    public long FarmerID { get; init; }
    public FreezeTimeMethod FreezeTimeMethod { get; init; }
    public bool? Increase { get; init; } = null;
    public string? Location { get; init; } = null;
}
