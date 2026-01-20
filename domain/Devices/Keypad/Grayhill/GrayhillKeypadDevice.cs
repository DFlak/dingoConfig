using System.Collections.Concurrent;
using System.Text.Json.Serialization;
using domain.Devices.Keypad.Enums;
using domain.Models;
using Microsoft.Extensions.Logging;

namespace domain.Devices.Keypad.Grayhill;

[method: JsonConstructor]
public class GrayhillKeypadDevice(string name, int baseId, int numButtons) : KeypadDevice(name, baseId)
{
    [JsonPropertyName("brand")] public override KeypadBrand Brand { get; set; } = KeypadBrand.Grayhill;
    
    [JsonPropertyName("buttons")] public Button[] Buttons { get; set; } = new Button[numButtons];
    
    protected override void Clear()
    {
        Logger.LogInformation($"Grayhill keypad {Name} cleared");
    }
    
    public override void Read(int id, byte[] data, ref ConcurrentDictionary<(int BaseId, int Prefix, int Index), DeviceCanFrame> queue)
    {

    }

    public override bool InIdRange(int id)
    {
        return false;
    }

    public override IEnumerable<(int MessageId, DbcSignal Signal)> GetStatusSignals()
    {
        return Enumerable.Empty<(int MessageId, DbcSignal Signal)>();
    }
    
    public override List<DeviceCanFrame> GetCyclicMsgs()
    {
        return [];
    }
    
}