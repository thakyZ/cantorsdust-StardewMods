using TimeSpeed.Framework.Models.Messages.Interfaces;
using TimeSpeed.Framework.Models.Enum;

namespace TimeSpeed.Framework.Models.Messages
{
    internal class TimeManipulateMessage : ITimeManipulateMessage
    {
        public long FarmerID { get; init; }
        public FreezeTimeMethod FreezeTimeMethod { get; init; }
        public bool? Increase { get; init; } = null;
    }
}
