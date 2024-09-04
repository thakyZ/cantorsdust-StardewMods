using TimeSpeed.Framework.Models.Enum;

namespace TimeSpeed.Framework.Models.Messages.Interfaces
{
    internal interface ITimeManipulateMessage : ITimeMessage
    {
        public FreezeTimeMethod FreezeTimeMethod { get; init; }
        public bool? Increase { get; init; }
    }
}
