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
    public override KeypadBrand Brand { get; set; } = KeypadBrand.BlinkMarine;
    public override int NumButtons { get; set; } = numButtons;
    public override int NumDials { get; set; } = numDials;
    public override int NumAnalogInputs { get; set; } = numAnalogInputs;
    
    public BacklightColor BacklightColor { get; set; }
    
    private byte TickTimer { get; set; }

    protected override void InitializeCollections()
    {
        for (var i = 0; i < NumButtons; i++)
            Buttons.Add(new Button(i + 1, $"button{i + 1}"));

        for (var i = 0; i < NumDials; i++)
            Dials.Add(new Dial(i + 1, $"dial{i + 1}"));

        for (var i = 0; i < NumAnalogInputs; i++)
            AnalogInputs.Add(new AnalogInput(i + 1, $"analogInput{i + 1}"));
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
        
        // Dial Direction
        StatusMessageSignals[1] = [];
        for (var i = 0; i < NumDials; i++)
        {
            var dial = (Dial)Dials[i];
            StatusMessageSignals[1].Add((
                new DbcSignal { Name = $"Dial{i}Direction", StartBit = (i * 32), Length = 1},
                val => dial.Direction = (DialDirection)val
            ));
        }
        
        // Dial Position
        StatusMessageSignals[2] = [];
        for (var i = 0; i < NumDials; i++)
        {
            var dial = (Dial)Dials[i];
            StatusMessageSignals[2].Add((
                new DbcSignal { Name = $"Dial{i}Position", StartBit = (i * 32) + 1, Length = 7},
                val => dial.Position = (int)val
            ));
        }
        
        // Dial Counter
        StatusMessageSignals[3] = [];
        for (var i = 0; i < NumDials; i++)
        {
            var dial = (Dial)Dials[i];
            StatusMessageSignals[3].Add((
                new DbcSignal { Name = $"Dial{i}Counter", StartBit = (i * 8), Length = 16},
                val => dial.Position = (int)val
            ));
        }
        
        // Analog Inputs
        StatusMessageSignals[4] = [];
        for (var i = 0; i < NumAnalogInputs; i++)
        {
            var input = (AnalogInput)AnalogInputs[i];
            StatusMessageSignals[4].Add((
                new DbcSignal { Name = $"AnalogInput{i}Value", StartBit = i * 16, Length = 16, Factor = 0.01},
                val => input.Voltage = val
            ));
        }
    }
    
    protected override void Clear()
    {
        foreach (Button btn in Buttons)
            btn.State = false;

        foreach (Dial dial in Dials)
        {
            dial.Position = 0;
            dial.Delta = 0;
            dial.Direction = DialDirection.Clockwise;
        }

        foreach (AnalogInput input in AnalogInputs)
            input.Voltage = 0;

        Logger.LogInformation("{Name} BlinkMarine Keypad cleared", Name);
    }

    public override bool InIdRange(int id)
    {
        // CANopen uses BaseId as node ID (1-127)
        // Message IDs: 0x180 + nodeId, 0x280 + nodeId, etc.
        return id == ((int)MessageId.ButtonState + BaseId) ||
               id == ((int)MessageId.SetLed + BaseId) ||
               id == ((int)MessageId.DialStateA + BaseId) ||
               id == ((int)MessageId.SetLedBlink + BaseId) ||
               id == ((int)MessageId.DialStateB + BaseId) ||
               id == ((int)MessageId.LedBrightness + BaseId) ||
               id == ((int)MessageId.AnalogInput + BaseId) ||
               id == ((int)MessageId.Backlight + BaseId) ||
               id == ((int)MessageId.Heartbeat + BaseId);
    }

    public override void Read(int id, byte[] data, ref ConcurrentDictionary<(int BaseId, int Prefix, int Index), DeviceCanFrame> queue)
    {
        LastRxTime = DateTime.Now;

        switch ((MessageId)id - BaseId)
        {
            case MessageId.ButtonState:
                ParseButtonState(data);
                break;
            case MessageId.SetLed:
                ParseSetLed(data);
                break;
            case MessageId.DialStateA:
                ParseDialState(data, firstDialIndex: 0);
                break;
            case MessageId.SetLedBlink:
                ParseSetLedBlink(data);
                break;
            case MessageId.DialStateB:
                ParseDialState(data, firstDialIndex: 2);
                break;
            case MessageId.LedBrightness:
                ParseLedBrightness(data);
                break;
            case MessageId.AnalogInput:
                ParseAnalogInput(data);
                break;
            case MessageId.Backlight:
                ParseBacklight(data);
                break;
            case MessageId.Heartbeat:
                // Just update connection timestamp
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

    private void ParseSetLed(byte[] data)
    {
        
    }

    private void ParseSetLedBlink(byte[] data)
    {
        
    }

    private void ParseLedBrightness(byte[] data)
    {
        IndicatorBrightness = (int)DbcSignalCodec.ExtractSignalInt(data, startBit: 0, length: 8, factor: 1.58);
    }

    private void ParseBacklight(byte[] data)
    {
        BacklightBrightness = DbcSignalCodec.ExtractSignalInt(data, startBit: 0, length: 1) > 0 ? 100 : 0;
    }

    private void ParseDialState(byte[] data, int firstDialIndex)
    {
        // 2 dials per message
        for (var i = 0; i < 2 && i < Dials.Count; i++)
        {
            var dial = (Dial)Dials[i + firstDialIndex];

            var position = (short)DbcSignalCodec.ExtractSignalInt(
                data,
                startBit: (i * 32) + 1,
                length: 7);

            dial.Delta = position - dial.Position;
            dial.Position = position;

            dial.Direction = (DialDirection)DbcSignalCodec.ExtractSignalInt(
                data,
                startBit: i * 32,
                length: 1);

            dial.Counter = (int)DbcSignalCodec.ExtractSignalInt(
                data,
                startBit: (i * 8),
                length: 16);

            dial.TopPosition = (int)DbcSignalCodec.ExtractSignalInt(
                data,
                startBit: (i * 24),
                length: 8);
        }
    }

    private void ParseAnalogInput(byte[] data)
    {
        // Each analog input is 16-bit unsigned value in millivolts
        var numInputsInMessage = Math.Min(4, NumAnalogInputs);

        for (var i = 0; i < numInputsInMessage && i < AnalogInputs.Count; i++)
        {
            var input = (AnalogInput)AnalogInputs[i];
            var startBit = i * 16;

            input.Voltage = DbcSignalCodec.ExtractSignal(
                data,
                startBit: startBit,
                length: 16);
        }
    }
    
    private CanFrame BuildButtonState()
    {
        var data = new byte[5];

        for (var i = 0; i < Buttons.Count; i++)
        {
            var button = (Button)Buttons[i];
            DbcSignalCodec.InsertBool(data, button.State, i);
        }

        data[4] = TickTimer;
        TickTimer++;
    
        return new CanFrame((int)MessageId.ButtonState + BaseId, 5, data);
    }

    private CanFrame BuildDialState(int firstDialIndex, int numDials)
    {
        var data = new byte[8];
        
        for (var i = 0; i < numDials && i < Dials.Count; i++)
        {
            var dial = (Dial)Dials[i + firstDialIndex];
            
            DbcSignalCodec.InsertBool(data, dial.Direction == DialDirection.CounterClockwise, startBit: 0);
            DbcSignalCodec.InsertSignalInt( data, 
                                            value: dial.Position, 
                                            startBit: (i * 32) + 1, 
                                            length: 7);
            DbcSignalCodec.InsertSignalInt( data, 
                                            value: dial.Counter,
                                            startBit: (i * 8) + 1,
                                            length: 16);
            DbcSignalCodec.InsertSignalInt( data,
                                            value: dial.TopPosition,
                                            startBit: (i * 24),
                                            length: 8);
        }

        var id = (int)MessageId.DialStateA;
        if (firstDialIndex > 0) id = (int)MessageId.DialStateB;

        return new CanFrame(id, 8, data);
    }

    private CanFrame BuildAnalogInput()
    {
        var data = new byte[8];

        for (var i = 0; i < AnalogInputs.Count; i++)
        {
            var input = (AnalogInput)AnalogInputs[i];
            DbcSignalCodec.InsertSignal(data, input.Voltage, i * 16, 16, factor: 0.01);
        }
        
        return new  CanFrame((int)MessageId.AnalogInput + BaseId, 8, data);
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
        
        var msgs = new List<CanFrame>();
        
        msgs.Add(BuildButtonState());

        switch (NumDials)
        {
            case 1:
                msgs.Add(BuildDialState(0, 1));
                break;
            case 2:
                msgs.Add(BuildDialState(0, 1));
                msgs.Add(BuildDialState(1, 1));
                break;
            case 4:
                msgs.Add(BuildDialState(0, 2));
                msgs.Add(BuildDialState(2, 2));
                break;
        }
 
        if (NumAnalogInputs > 0)
            msgs.Add(BuildAnalogInput());
        
        return msgs;
    }
}