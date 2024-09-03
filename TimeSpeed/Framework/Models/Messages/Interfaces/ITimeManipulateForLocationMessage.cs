namespace TimeSpeed.Framework.Models.Messages.Interfaces
{
    internal interface ITimeManipulateForLocationMessage : ITimeManipulateMessage
    {
        public byte[] Location { get; init; }
    }
}