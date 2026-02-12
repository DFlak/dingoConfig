using domain.Interfaces;
using domain.Models;

namespace domain.Devices.dingoPdm.Functions.Keypad;

public class Button : IDeviceFunction
{
    public int Number { get; }
    public string Name { get; }
    public List<DeviceParameter> Params { get; }
}