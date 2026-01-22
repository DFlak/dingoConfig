using System.Collections.Concurrent;
using System.Text.Json.Serialization;
using domain.Common;
using domain.Devices.Keypad.BlinkMarine.Enums;
using domain.Devices.Keypad.Enums;
using domain.Enums;
using domain.Interfaces;
using domain.Models;
using Microsoft.Extensions.Logging;

namespace domain.Devices.Keypad.BlinkMarine;

public class BlinkMarineKeypadDevice : IDevice
{
    [JsonIgnore] private ILogger<BlinkMarineKeypadDevice>? _logger;
    
    [JsonIgnore] public Guid Guid { get; }
    [JsonIgnore] public string Type => "BlinkMarineKeypad";
    [JsonPropertyName("name")] public string Name { get; set; }
    [JsonPropertyName("baseId")] public int BaseId { get; set; }
    [JsonPropertyName("cyclicGap")] public TimeSpan CyclicGap { get; } = TimeSpan.FromSeconds(1);
    [JsonPropertyName("cyclicPause")] public TimeSpan CyclicPause { get; } = TimeSpan.FromMilliseconds(1);
    [JsonPropertyName("isSim")] public bool IsSim { get; set; }
    [JsonIgnore] private DateTime _lastRxTime = DateTime.Now;

    [JsonIgnore]
    public bool Connected
    {
        get;
        private set
        {
            if (field && !value)
                Clear();
            field = value;
        }
    }
    
    [JsonPropertyName("brand")] public KeypadBrand Brand { get; set; } = KeypadBrand.BlinkMarine;
    [JsonPropertyName("numButtons")] public int NumButtons { get; set; }
    [JsonPropertyName("numDials")] public int NumDials { get; set; }
    [JsonPropertyName("numAnalogInputs")] public int NumAnalogInputs { get; set; }
    [JsonPropertyName("backlightColor")] public BacklightColor BacklightColor { get; set; }
    [JsonIgnore] public int BacklightBrightness { get; set; }
    [JsonIgnore] public int IndicatorBrightness { get; set; }
    [JsonIgnore] private byte TickTimer { get; set; }
    
    [JsonPropertyName("buttons")] public List<Button> Buttons { get; init; } = [];
    [JsonPropertyName("dials")] public List<Dial> Dials { get; init; } = [];
    [JsonPropertyName("analogInputs")] public List<AnalogInput> AnalogInputs { get; init; } = [];

    [JsonIgnore] public Dictionary<int, List<(DbcSignal Signal, Action<double> SetValue)>> StatusMessageSignals { get; set; } = null!;

    [JsonConstructor]
    public BlinkMarineKeypadDevice(string name, int baseId, string model)
    {
        Name = name;
        BaseId = baseId;

        var config = BlinkMarineModels.Lookup(model);
        NumButtons = config.numButtons;
        NumDials = config.numDials;
        NumAnalogInputs = config.numAnalogInputs;
        Guid = Guid.NewGuid();
        InitializeCollections();
        InitializeStatusMessageSignals();
    }

    public void SetLogger(ILogger<BlinkMarineKeypadDevice> logger)
    {
        _logger = logger;
    }

    private void InitializeCollections()
    {
        for (var i = 0; i < NumButtons; i++)
            Buttons.Add(new Button(i + 1, $"button{i + 1}"));

        for (var i = 0; i < NumDials; i++)
            Dials.Add(new Dial(i + 1, $"dial{i + 1}"));

        for (var i = 0; i < NumAnalogInputs; i++)
            AnalogInputs.Add(new AnalogInput(i + 1, $"analogInput{i + 1}"));
    }

    private void InitializeStatusMessageSignals()
    {
        StatusMessageSignals = new Dictionary<int, List<(DbcSignal Signal, Action<double> SetValue)>>();

        // Button States
        StatusMessageSignals[0] = [];
        for (var i = 0; i < NumButtons; i++)
        {
            var button = Buttons[i];
            StatusMessageSignals[0].Add((
                new DbcSignal { Name = $"Button{i + 1}.State", StartBit = i, Length = 1},
                val => button.State = val != 0
            ));
        }
        
        // Dial Direction
        StatusMessageSignals[1] = [];
        for (var i = 0; i < NumDials; i++)
        {
            var dial = Dials[i];
            StatusMessageSignals[1].Add((
                new DbcSignal { Name = $"Dial{i + 1}.Direction", StartBit = (i * 32), Length = 1},
                val => dial.Direction = (DialDirection)val
            ));
        }
        
        // Dial Position
        StatusMessageSignals[2] = [];
        for (var i = 0; i < NumDials; i++)
        {
            var dial = Dials[i];
            StatusMessageSignals[2].Add((
                new DbcSignal { Name = $"Dial{i + 1}.Position", StartBit = (i * 32) + 1, Length = 7},
                val => dial.Position = (int)val
            ));
        }
        
        // Dial Counter
        StatusMessageSignals[3] = [];
        for (var i = 0; i < NumDials; i++)
        {
            var dial = Dials[i];
            StatusMessageSignals[3].Add((
                new DbcSignal { Name = $"Dial{i + 1}.Counter", StartBit = (i * 8), Length = 16},
                val => dial.Position = (int)val
            ));
        }
        
        // Analog Inputs
        StatusMessageSignals[4] = [];
        for (var i = 0; i < NumAnalogInputs; i++)
        {
            var input = AnalogInputs[i];
            StatusMessageSignals[4].Add((
                new DbcSignal { Name = $"AnalogInput{i + 1}.Value", StartBit = i * 16, Length = 16, Factor = 0.01}, // 5/500
                val => input.Voltage = val
            ));
        }
    }

    public void UpdateIsConnected()
    {
        var timeSpan = DateTime.Now - _lastRxTime;
        Connected = timeSpan.TotalMilliseconds < 500;
    }

    private void Clear()
    {
        foreach (var btn in Buttons)
            btn.State = false;

        foreach (var dial in Dials)
        {
            dial.Position = 0;
            dial.Delta = 0;
            dial.Direction = DialDirection.Clockwise;
        }

        foreach (var input in AnalogInputs)
            input.Voltage = 0;

        _logger?.LogInformation("{Name} BlinkMarine Keypad cleared", Name);
    }

    public bool InIdRange(int id)
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

    public void Read(int id, byte[] data, ref ConcurrentDictionary<(int BaseId, int Prefix, int Index), DeviceCanFrame> queue)
    {
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
            case MessageId.Nmt:
            case MessageId.SdoResponse:
            case MessageId.SdoRequest:
                break;
            default:
                //Don't update LastRxTime, not a valid message
                return;
        }

        _lastRxTime = DateTime.Now;
    }

    private void ParseButtonState(byte[] data)
    {
        // Button states are packed as bits
        // Each byte contains 8 button states
        for (var i = 0; i < NumButtons && i < Buttons.Count; i++)
        {
            var button = Buttons[i];
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
        IndicatorBrightness = (int)DbcSignalCodec.ExtractSignalInt(data, startBit: 0, length: 8, factor: 1.58); //100% / 63 = 1.58

        foreach (var button in Buttons)
            button.IndicatorBrightness = IndicatorBrightness;
    }

    private void ParseBacklight(byte[] data)
    {
        BacklightBrightness = DbcSignalCodec.ExtractSignalInt(data, startBit: 0, length: 1) > 0 ? 100 : 0;
        
        foreach (var button in Buttons)
            button.BacklightBrightness = BacklightBrightness;
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
            var input = AnalogInputs[i];
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
            var button = Buttons[i];
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
            var input = AnalogInputs[i];
            DbcSignalCodec.InsertSignal(data, input.Voltage, i * 16, 16, factor: 0.01);
        }
        
        return new  CanFrame((int)MessageId.AnalogInput + BaseId, 8, data);
    }

    public IEnumerable<(int MessageId, DbcSignal Signal)> GetStatusSignals()
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

    public List<CanFrame> GetCyclicMsgs()
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