#nullable enable

namespace TimeSpeed.Framework.Models.Messages.Interfaces
{
    internal interface ITimeInfoMessage : ITimeMessage
    {
        public string? Message { get; init; }
    }
}
