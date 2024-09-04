namespace TimeSpeed.Framework.Models.Messages.Interfaces
{
    internal interface ITimeConfigStateMessage : ITimeMessage
    {
        public bool? HostOnly { get; init; }
        public bool? VoteEnabled { get; init; }
        public double? VoteThreshold { get; init; }
    }
}
