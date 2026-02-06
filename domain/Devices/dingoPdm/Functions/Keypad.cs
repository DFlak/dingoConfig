using System.Text.Json.Serialization;
using domain.Devices.dingoPdm.Enums;
using domain.Interfaces;
using domain.Models;

namespace domain.Devices.dingoPdm.Functions;

public class Keypad : IDeviceFunction
{
    [JsonIgnore] public const int BaseIndex = 0x3000;

    public int Number { get; }
    public string Name { get; }
    [JsonIgnore] public List<DeviceParameter> Params { get; } = [];
    public static int ExtractIndex(byte data, MessageCommand command)
    {
        throw new NotImplementedException();
    }

    public bool Receive(byte[] data, MessageCommand command)
    {
        throw new NotImplementedException();
    }

    public DeviceCanFrame? CreateUploadRequest(int baseId, MessageCommand command)
    {
        throw new NotImplementedException();
    }

    public DeviceCanFrame? CreateDownloadRequest(int baseId, MessageCommand command)
    {
        throw new NotImplementedException();
    }
}