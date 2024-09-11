using TimeSpeed.Framework.Models.Messages.Interfaces;
using TimeSpeed.Framework.Models.Enum;

namespace TimeSpeed.Framework.Models.Messages;

internal class TimeForbiddenMessage : ITimeForbiddenMessage
{
    public long FarmerID { get; init; }
    public ForbiddenReason Reason { get; init; }
}