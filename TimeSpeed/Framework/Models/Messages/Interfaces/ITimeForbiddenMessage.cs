using TimeSpeed.Framework.Models.Enum;

namespace TimeSpeed.Framework.Models.Messages.Interfaces
{
    internal interface ITimeForbiddenMessage : ITimeMessage
    {
        public ForbiddenReason Reason { get; init; }
    }
}