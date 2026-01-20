using System.Collections.Concurrent;
using System.Text.Json.Serialization;
using domain.Common;
using domain.Devices.Keypad.Enums;
using domain.Models;
using Microsoft.Extensions.Logging;

namespace domain.Devices.Keypad.Grayhill;

[method: JsonConstructor]
public class GrayhillKeypadDevice(string name, int baseId, int numButtons) : KeypadDevice(name, baseId)
{
    [JsonIgnore] private DateTime _lastRxTime = DateTime.Now;

    public override KeypadBrand Brand { get; set; } = KeypadBrand.Grayhill;
    public override int NumButtons { get; set; } = numButtons;

    protected override void InitializeCollections()
    {
        for (var i = 0; i < NumButtons; i++)
            Buttons.Add(new Button(i + 1, $"button{i + 1}"));
    }

    protected override void Clear()
    {
        foreach (Button btn in Buttons)
            btn.State = false;

        Logger?.LogInformation("{Name} (Grayhill Keypad) cleared", Name);
    }

    public override bool InIdRange(int id)
    {
        // Grayhill keypad uses simple CAN ID range
        // Assuming BaseId for button state messages
        // Could be extended for multiple message types
        return id == BaseId;
    }

    public override void Read(int id, byte[] data, ref ConcurrentDictionary<(int BaseId, int Prefix, int Index), DeviceCanFrame> queue)
    {
        _lastRxTime = DateTime.Now;

        if (id == BaseId)
        {
            ParseButtonState(data);
        }
    }

    private void ParseButtonState(byte[] data)
    {
        // Button states are packed as bits in the payload
        // Each byte contains 8 button states
        for (int i = 0; i < NumButtons && i < Buttons.Count; i++)
        {
            var button = (Button)Buttons[i];
            var byteIndex = i / 8;
            var bitIndex = i % 8;

            if (byteIndex < data.Length)
            {
                button.State = (data[byteIndex] & (1 << bitIndex)) != 0;
            }
        }
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