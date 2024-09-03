using TimeSpeed.Framework.Models.Messages.Interfaces;

namespace TimeSpeed.Framework.Models.Messages
{
    internal class TimeConfigStateMessage : ITimeConfigStateMessage
    {
        public long FarmerID { get; init; }
        public bool HostOnly { get; set; }
        public bool VoteEnabled { get; set; }
        public double VoteThreshold { get; set; }
    }
}
