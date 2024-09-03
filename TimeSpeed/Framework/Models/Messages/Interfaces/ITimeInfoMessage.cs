namespace TimeSpeed.Framework.Models.Messages.Interfaces
{
    internal interface ITimeInfoMessage : ITimeMessage
    {
        public byte[] Message { get; init; }
    }
}