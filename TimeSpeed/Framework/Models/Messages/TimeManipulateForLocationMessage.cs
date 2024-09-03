using TimeSpeed.Framework.Models.Messages.Interfaces;
using TimeSpeed.Framework.Models.Enum;

namespace TimeSpeed.Framework.Models.Messages
{
    internal class TimeManipulateForLocationMessage : ITimeManipulateForLocationMessage
    {
        public long FarmerID { get; init; }
        public FreezeTimeMethod FreezeTimeMethod { get; init; }
        public TickIntervalState TickIntervalState { get; init; }
        public byte[] Location { get; init; } = [];
    }
}