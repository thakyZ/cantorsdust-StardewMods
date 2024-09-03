using TimeSpeed.Framework.Models.Enum;

namespace TimeSpeed.Framework.Models.Messages.Interfaces
{
    internal interface ITimeConfigStateMessage : ITimeMessage
    {
        public bool HostOnly { get; set; }
        public bool VoteEnabled { get; set; }
        public double VoteThreshold { get; set; }
    }
}
