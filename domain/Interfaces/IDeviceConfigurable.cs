using domain.Models;

namespace domain.Interfaces;

public interface IDeviceConfigurable : IDevice
{
    Dictionary<string, Dictionary<string, int>> VarMap { get; set; }
    List<DeviceCanFrame> GetReadMsgs();
    List<DeviceCanFrame> GetWriteMsgs();
    List<DeviceCanFrame> GetModifyMsgs(int newId);
    DeviceCanFrame GetBurnMsg();
    DeviceCanFrame GetSleepMsg();
    DeviceCanFrame GetVersionMsg();
    DeviceCanFrame GetWakeupMsg();
    DeviceCanFrame GetBootloaderMsg();
}