namespace domain.Models;

public class DeviceCanFrame
{
    public required CanFrame Frame { get; set; }
    public bool Sent { get; set; }
    public bool Received { get; set; }
    public Timer? TimeSentTimer { get; set; }
    public int RxAttempts { get; set; }
    public int DeviceBaseId { get; set; }
}