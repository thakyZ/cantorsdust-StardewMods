namespace TimeSpeed.Framework.Models.Messages.Interfaces
{
    internal interface ITimeStateReplyMessage : ITimeInfoMessage
    {
        public int Timeout { get; init; }
    }
}