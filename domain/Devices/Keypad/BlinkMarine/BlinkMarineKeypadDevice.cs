using System.Collections.Concurrent;
using System.Text.Json.Serialization;
using domain.Common;
using domain.Devices.Keypad.BlinkMarine.Enums;
using domain.Devices.Keypad.Enums;
using domain.Enums;
using domain.Models;
using Microsoft.Extensions.Logging;

namespace domain.Devices.Keypad.BlinkMarine;

[method: JsonConstructor]
public class BlinkMarineKeypadDevice(string name, int baseId, int numButtons, int numDials, int numAnalogInputs) : KeypadDevice(name, baseId)
{
    [JsonIgnore] private DateTime _lastRxTime = DateTime.Now;

    public override KeypadBrand Brand { get; set; } = KeypadBrand.BlinkMarine;
    public override int NumButtons { get; set; } = numButtons;
    public override int NumDials { get; set; } = numDials;
    public override int NumAnalogInputs { get; set; } = numAnalogInputs;

    protected override void InitializeCollections()
    {
        for (var i = 0; i < NumButtons; i++)
            Buttons.Add(new Button(i + 1, $"button{i + 1}"));

        for (var i = 0; i < NumDials; i++)
            Dials.Add(new Dial(i + 1, $"dial{i + 1}"));

        for (var i = 0; i < NumAnalogInputs; i++)
            AnalogInputs.Add(new AnalogInput(i + 1, $"analogInput{i + 1}"));
    }

    protected override void Clear()
    {
        foreach (Button btn in Buttons)
            btn.State = false;

        foreach (Dial dial in Dials)
        {
            dial.Position = 0;
            dial.Delta = 0;
        }

        foreach (AnalogInput input in AnalogInputs)
            input.Voltage = 0;

        Logger?.LogInformation("{Name} (BlinkMarine Keypad) cleared", Name);
    }

    public override bool InIdRange(int id)
    {
        // CANopen uses BaseId as node ID (1-127)
        // Message IDs: 0x180 + nodeId, 0x280 + nodeId, etc.
        var nodeId = BaseId;
        return id == ((int)MessageId.ButtonState + nodeId) ||
               id == ((int)MessageId.DialStateA + nodeId) ||
               id == ((int)MessageId.DialStateB + nodeId) ||
               id == ((int)MessageId.AnalogInput + nodeId) ||
               id == ((int)MessageId.Heartbeat + nodeId);
    }

    public override void Read(int id, byte[] data, ref ConcurrentDictionary<(int BaseId, int Prefix, int Index), DeviceCanFrame> queue)
    {
        _lastRxTime = DateTime.Now;

        var nodeId = BaseId;
        var baseId = id - nodeId;  // Extract message type

        switch ((MessageId)baseId)
        {
            case MessageId.ButtonState:
                ParseButtonState(data);
                break;
            case MessageId.DialStateA:
                ParseDialStateA(data);
                break;
            case MessageId.DialStateB:
                ParseDialStateB(data);
                break;
            case MessageId.AnalogInput:
                ParseAnalogInput(data);
                break;
            case MessageId.Heartbeat:
                // Just update connection timestamp
                break;
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

    private void ParseDialStateA(byte[] data)
    {
        // Parse first set of dials (typically first 2 dials)
        // Each dial position is 16-bit signed integer
        var numDialsInMessage = Math.Min(4, NumDials);  // Max 4 dials per message (8 bytes / 2 bytes per dial)

        for (int i = 0; i < numDialsInMessage && i < Dials.Count; i++)
        {
            var dial = (Dial)Dials[i];
            var startBit = i * 16;

            var position = (short)DbcSignalCodec.ExtractSignalInt(
                data,
                startBit: startBit,
                length: 16,
                ByteOrder.LittleEndian
            );

            dial.Delta = position - dial.Position;
            dial.Position = position;
        }
    }

    private void ParseDialStateB(byte[] data)
    {
        // Parse second set of dials (dials 5-8 if present)
        var offset = 4;  // Start from dial index 4
        var numDialsInMessage = Math.Min(4, NumDials - offset);

        for (int i = 0; i < numDialsInMessage && (offset + i) < Dials.Count; i++)
        {
            var dial = (Dial)Dials[offset + i];
            var startBit = i * 16;

            var position = (short)DbcSignalCodec.ExtractSignalInt(
                data,
                startBit: startBit,
                length: 16,
                ByteOrder.LittleEndian
            );

            dial.Delta = position - dial.Position;
            dial.Position = position;
        }
    }

    private void ParseAnalogInput(byte[] data)
    {
        // Each analog input is 16-bit unsigned value in millivolts
        var numInputsInMessage = Math.Min(4, NumAnalogInputs);

        for (int i = 0; i < numInputsInMessage && i < AnalogInputs.Count; i++)
        {
            var input = (AnalogInput)AnalogInputs[i];
            var startBit = i * 16;

            input.Voltage = DbcSignalCodec.ExtractSignal(
                data,
                startBit: startBit,
                length: 16,
                ByteOrder.LittleEndian
            );
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