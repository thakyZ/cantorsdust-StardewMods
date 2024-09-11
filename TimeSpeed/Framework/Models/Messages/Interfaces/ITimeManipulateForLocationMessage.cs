#nullable enable

namespace TimeSpeed.Framework.Models.Messages.Interfaces;

internal interface ITimeManipulateForLocationMessage : ITimeManipulateMessage
{
    public string? Location { get; init; }
}
