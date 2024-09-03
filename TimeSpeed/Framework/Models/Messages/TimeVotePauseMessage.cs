using TimeSpeed.Framework.Models.Messages.Interfaces;
using TimeSpeed.Framework.Models.Enum;

namespace TimeSpeed.Framework.Models.Messages
{
    internal class TimeVotePauseMessage : ITimeVotePauseMessage
    {
        public long FarmerID { get; init; }
        public VoteCast VoteCast { get; init; }
        public bool Finish { get; init; }
    }
}