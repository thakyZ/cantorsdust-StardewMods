using TimeSpeed.Framework.Models.Messages.Interfaces;

#nullable enable

namespace TimeSpeed.Framework.Models.Messages
{
    internal class TimeInfoMessage : ITimeInfoMessage
    {
        public long FarmerID { get; init; }
        public string? Message { get; init; } = null;
    }
}
