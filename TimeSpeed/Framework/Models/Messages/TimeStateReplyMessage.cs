using TimeSpeed.Framework.Models.Messages.Interfaces;

namespace TimeSpeed.Framework.Models.Messages
{
    internal class TimeStateReplyMessage : ITimeStateReplyMessage
    {
        public long FarmerID { get; init; }
        public int Timeout { get; init; }
        public byte[] Message { get; init; } = [];
    }
}