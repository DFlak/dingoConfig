using System.Collections.Concurrent;
using System.Text.Json.Serialization;
using domain.Common;
using domain.Devices.Keypad.BlinkMarine;
using domain.Devices.Keypad.Enums;
using domain.Devices.Keypad.Grayhill.Enums;
using domain.Models;
using Microsoft.Extensions.Logging;

namespace domain.Devices.Keypad.Grayhill;

[method: JsonConstructor]
public class GrayhillKeypadDevice(string name, int baseId, int numButtons) : KeypadDevice(name, baseId)
{
    public override KeypadBrand Brand { get; set; } = KeypadBrand.Grayhill;
    public override int NumButtons { get; set; } = numButtons;

    protected override void InitializeCollections()
    {
        for (var i = 0; i < NumButtons; i++)
            Buttons.Add(new Button(i + 1, $"button{i + 1}"));
    }
    
    protected override void InitializeStatusMessageSignals()
    {
        StatusMessageSignals = new Dictionary<int, List<(DbcSignal Signal, Action<double> SetValue)>>();

        // Button States
        StatusMessageSignals[0] = [];
        for (var i = 0; i < NumButtons; i++)
        {
            var button = (Button)Buttons[i];
            StatusMessageSignals[0].Add((
                new DbcSignal { Name = $"Button{i}State", StartBit = i, Length = 1},
                val => button.State = val != 0
            ));
        }
    }

    protected override void Clear()
    {
        foreach (Button btn in Buttons)
            btn.State = false;

        Logger.LogInformation("{Name} Grayhill Keypad cleared", Name);
    }

    public override bool InIdRange(int id)
    {
        // CANopen uses BaseId as node ID (1-127)
        // Message IDs: 0x180 + nodeId, 0x200 + nodeId, etc.
        return id == ((int)MessageId.ButtonState + BaseId) ||
               id == ((int)MessageId.LedControl + BaseId);
    }

    public override void Read(int id, byte[] data, ref ConcurrentDictionary<(int BaseId, int Prefix, int Index), DeviceCanFrame> queue)
    {
        LastRxTime = DateTime.Now;

        switch ((MessageId)id - BaseId)
        {
            case MessageId.ButtonState:
                //Only read button state when not sim
                //Sim maintains its own button state
                if(!IsSim)
                    ParseButtonState(data);
                break;
            case MessageId.LedControl:
                ParseLedControl(data);
                break;
            case MessageId.BrightnessControl:
                ParseBrightnessControl(data);
                break;
        }
    }

    private void ParseButtonState(byte[] data)
    {
        // Button states are packed as bits
        // Each byte contains 8 button states
        for (var i = 0; i < NumButtons && i < Buttons.Count; i++)
        {
            var button = (Button)Buttons[i];
            var byteIndex = i / 8;
            var bitIndex = i % 8;

            if (byteIndex < data.Length)
                button.State = (data[byteIndex] & (1 << bitIndex)) != 0;
        }
    }

    private void ParseLedControl(byte[] data)
    {
        // LEDs are packed as 3 bits per button
        for (var i = 0; i < NumButtons && i < Buttons.Count; i++)
        {
            var button = (Button)Buttons[i];
            
            for (var j = 0; j < Button.LedCount; j++)
            {
                var bitPos = (i * Button.LedCount) + j;
                var byteIndex = bitPos / 8;
                var bitIndex = bitPos % 8;
                
                if (byteIndex < data.Length)
                    button.Led[j] = (data[byteIndex] & (1 << bitIndex)) != 0;
                else
                    button.Led[j] = false;
            }
        }
    }

    private void ParseBrightnessControl(byte[] data)
    {
        if (data.Length < 3) return;

        // Brightness is scaled 0% (0), 50% (128), 100% (255)
        IndicatorBrightness = (int)DbcSignalCodec.ExtractSignalInt(data, 0, 16, factor: 0.392);
        BacklightBrightness = (int)DbcSignalCodec.ExtractSignalInt(data, 16, 16, factor: 0.392);
    }

    private CanFrame BuildButtonState()
    {
        var data = new byte[3];

        for (var i = 0; i < Buttons.Count; i++)
        {
            var button = (Button)Buttons[i];
            DbcSignalCodec.InsertBool(data, button.State, i);
        }
    
        return new CanFrame((int)MessageId.ButtonState + BaseId, 3, data);
    }

    public override IEnumerable<(int MessageId, DbcSignal Signal)> GetStatusSignals()
    {
        foreach (var kvp in StatusMessageSignals)
        {
            var messageId = BaseId + kvp.Key;
            foreach (var (signal, _) in kvp.Value)
            {
                // Create a copy with the ID populated
                var signalCopy = new DbcSignal
                {
                    Name = signal.Name,
                    Id = messageId,
                    StartBit = signal.StartBit,
                    Length = signal.Length,
                    ByteOrder = signal.ByteOrder,
                    IsSigned = signal.IsSigned,
                    Factor = signal.Factor,
                    Offset = signal.Offset,
                    Unit = signal.Unit,
                    Min = signal.Min,
                    Max = signal.Max
                };
                yield return (messageId, signalCopy);
            }
        }
    }

    public override List<CanFrame> GetCyclicMsgs()
    {
        if (!IsSim) return [];
        
        //Transmit button states
        return
        [
            BuildButtonState(),
        ];
    }
}