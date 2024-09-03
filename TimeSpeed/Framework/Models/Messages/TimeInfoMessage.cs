using TimeSpeed.Framework.Models.Messages.Interfaces;

namespace TimeSpeed.Framework.Models.Messages
{
    internal class TimeInfoMessage : ITimeInfoMessage
    {
        public long FarmerID { get; init; }
        public byte[] Message { get; init; } = [];
    }
}