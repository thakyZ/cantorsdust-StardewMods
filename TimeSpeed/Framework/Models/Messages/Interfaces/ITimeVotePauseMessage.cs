namespace TimeSpeed.Framework.Models.Messages.Interfaces
{
    internal interface ITimeVotePauseMessage : ITimeMessage
    {
        public bool? VoteCast { get; init; }
        public bool? Finish { get; init; }
    }
}
