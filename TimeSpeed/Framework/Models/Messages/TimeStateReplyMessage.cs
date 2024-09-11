using TimeSpeed.Framework.Models.Messages.Interfaces;

#nullable enable

namespace TimeSpeed.Framework.Models.Messages;

internal class TimeStateReplyMessage : ITimeStateReplyMessage
{
    public long FarmerID { get; init; }
    public float Timeout { get; init; }
    public string? Message { get; init; } = null;
}
