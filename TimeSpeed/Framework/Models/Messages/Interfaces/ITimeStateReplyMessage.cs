namespace TimeSpeed.Framework.Models.Messages.Interfaces;

internal interface ITimeStateReplyMessage : ITimeInfoMessage
{
    public float Timeout { get; init; }
}
