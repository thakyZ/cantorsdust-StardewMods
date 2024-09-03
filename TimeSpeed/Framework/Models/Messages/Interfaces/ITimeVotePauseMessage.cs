using TimeSpeed.Framework.Models.Enum;

namespace TimeSpeed.Framework.Models.Messages.Interfaces
{
    internal interface ITimeVotePauseMessage : ITimeMessage
    {
        public VoteCast VoteCast { get; init; }
        public bool Finish { get; init; }
    }
}