using System.Collections.Concurrent;
using System.Text.Json.Serialization;
using domain.Common;
using domain.Devices.dingoPdm.Enums;
using domain.Devices.dingoPdm.Functions;
using domain.Enums;
using domain.Interfaces;
using domain.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using static domain.Common.DbcSignalCodec;
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable VirtualMemberCallInConstructor

namespace domain.Devices.dingoPdm;

public class PdmDevice : IDeviceConfigurable
{
    [JsonIgnore] protected ILogger<PdmDevice> Logger = null!;

    [JsonIgnore] protected virtual int MinMajorVersion => 0;
    [JsonIgnore] protected virtual int MinMinorVersion => 4;
    [JsonIgnore] protected virtual int MinBuildVersion => 27;

    [JsonIgnore] protected virtual int NumDigitalInputs => 2;
    [JsonIgnore] protected virtual int NumOutputs => 8;
    [JsonIgnore] protected virtual int NumCanInputs => 32;
    [JsonIgnore] protected virtual int NumVirtualInputs => 16;
    [JsonIgnore] protected virtual int NumFlashers => 4;
    [JsonIgnore] protected virtual int NumCounters => 4;
    [JsonIgnore] protected virtual int NumConditions => 32;
    [JsonIgnore] protected virtual int NumKeypads => 2;
    [JsonIgnore] protected virtual int KeypadMaxButtons => 20;
    [JsonIgnore] protected virtual int KeypadMaxDials => 4;
    [JsonIgnore] protected virtual int KeypadMaxAnalogInputs => 4;
    

    [JsonIgnore] public const int BaseIndex = 0x0000;
    [JsonIgnore] protected virtual int PdmType => 0; //0=dingoPDM, 1=dingoPDM-Max
    [JsonIgnore] protected bool PdmTypeOk;
    
    [JsonIgnore] public Guid Guid { get; }
    [JsonIgnore] public virtual string Type => "dingoPDM";
    [JsonPropertyName("name")] public string Name { get; set; }
    [JsonPropertyName("baseId")] public int BaseId { get; set; }
    [JsonIgnore] public List<DeviceVariable> VarMap { get; set; } = null!;
    [JsonIgnore] public List<DeviceParameter> Params { get; set; } = null!;

    
    [JsonIgnore][Plotable(displayName:"DevState")] public DeviceState DeviceState { get; private set; }
    [JsonIgnore][Plotable(displayName:"TotalCurrent", unit:"A")] public double TotalCurrent { get; private set; }
    [JsonIgnore][Plotable(displayName:"BatteryVoltage", unit:"V")] public double BatteryVoltage { get; private set; }
    [JsonIgnore][Plotable(displayName:"Temperature", unit:"degC")] public double BoardTempC { get; private set; }
    [JsonIgnore] public string Version { get; private set; } = "v0.0.0";
    
    [JsonPropertyName("sleepEnabled")] public bool SleepEnabled { get; set; }
    [JsonPropertyName("filtersEnabled")] public bool CanFiltersEnabled { get; set; }
    [JsonPropertyName("bitrate")] public CanBitRate BitRate { get; set; }
    [JsonIgnore] public TimeSpan CyclicGap { get; } =  TimeSpan.FromSeconds(0);
    [JsonIgnore] public TimeSpan CyclicPause { get; } = TimeSpan.FromMilliseconds(0);
    
    [JsonPropertyName("inputs")] public List<Input> Inputs { get; init; } = [];
    [JsonPropertyName("outputs")] public List<Output> Outputs { get; init; } = [];
    [JsonPropertyName("canInputs")] public List<CanInput> CanInputs { get; init; } = [];
    [JsonPropertyName("virtualInputs")] public List<VirtualInput> VirtualInputs { get; init; } = [];
    [JsonPropertyName("wipers")] public Wiper Wipers { get; protected set; } = null!;
    [JsonPropertyName("flashers")] public List<Flasher> Flashers { get; init; } = [];
    [JsonPropertyName("starterDisable")] public StarterDisable StarterDisable { get; protected set; } = null!;
    [JsonPropertyName("counters")] public List<Counter> Counters { get; init; } = [];
    [JsonPropertyName("conditions")] public List<Condition> Conditions { get; init; } = [];
    
    [JsonIgnore] private DateTime LastRxTime { get; set; }

    [JsonIgnore] private Dictionary<int, List<(DbcSignal Signal, Action<double> SetValue)>> StatusSigs { get; set; } = null!;

    [JsonIgnore] private Dictionary<(int Index, int SubIndex), object> TempParamValues { get; set; } = new();
    [JsonIgnore] private int _readAllCount;
    [JsonIgnore] private int _writeAllCount;

    [JsonIgnore]
    public bool Connected
    {
        get;
        private set
        {
            if (field && !value)
            {
                Clear();
            }

            field = value;
        }
    }
    
    [JsonConstructor]
    public PdmDevice(string name, int baseId)
    {
        Name = name;
        BaseId = baseId;
        Guid = Guid.NewGuid();

        InitFunctions();
        InitVarMap();
        InitParams();
    }

    public void SetLogger(ILogger<PdmDevice> logger)
    {
        Logger = logger;
    }

    protected virtual void InitFunctions()
    {
        for (var i = 0; i < NumDigitalInputs; i++)
            Inputs.Add(new Input(i + 1, "digitalInput" + (i + 1)));

        for (var i = 0; i < NumOutputs; i++)
            Outputs.Add(new Output(i + 1, "output" + (i + 1)));

        for (var i = 0; i < NumCanInputs; i++)
            CanInputs.Add(new CanInput(i + 1, "canInput" + (i + 1)));

        for (var i = 0; i < NumVirtualInputs; i++)
            VirtualInputs.Add(new VirtualInput(i + 1, "virtualInput" + (i + 1)));

        for (var i = 0; i < NumFlashers; i++)
            Flashers.Add(new Flasher(i + 1,  "flasher" + (i + 1)));

        for (var i = 0; i < NumCounters; i++)
            Counters.Add(new Counter(i  + 1, "counter" + (i + 1)));

        for (var i = 0; i < NumConditions; i++)
            Conditions.Add(new Condition(i + 1, "condition" + (i + 1)));
        
        StarterDisable = new StarterDisable("starterDisable", NumOutputs);

        Wipers = new Wiper("wiper");

        InitStatusSigs();
    }

    protected virtual void InitStatusSigs()
    {
        StatusSigs = new Dictionary<int, List<(DbcSignal Signal, Action<double> SetValue)>>();

        // Message 0: System status
        StatusSigs[0] = new List<(DbcSignal, Action<double>)>();
        for (var i = 0; i < NumDigitalInputs; i++)
        {
            var inputIndex = i;
            StatusSigs[0].Add((
                new DbcSignal { Name = $"Input{inputIndex + 1}.State", StartBit = i, Length = 1 },
                val => Inputs[inputIndex].State = val != 0
            ));
        }
        StatusSigs[0].AddRange(new List<(DbcSignal, Action<double>)>
        {
            (new DbcSignal { Name = "DeviceState", StartBit = 8, Length = 4 },
                val => DeviceState = (DeviceState)val),
            (new DbcSignal { Name = "PdmType", StartBit = 12, Length = 4 },
                val => PdmTypeOk = PdmType == (int)val),
            (new DbcSignal { Name = "TotalCurrent", StartBit = 16, Length = 16, Factor = 0.1, Unit = "A" },
                val => TotalCurrent = val),
            (new DbcSignal { Name = "BatteryVoltage", StartBit = 32, Length = 16, Factor = 0.1, Unit = "V" },
                val => BatteryVoltage = val),
            (new DbcSignal { Name = "BoardTemp", StartBit = 48, Length = 16, Factor = 0.1, Unit = "°C" },
                val => BoardTempC = Math.Round(val, 1))
        });

        // Message 1: Output currents 0-3
        StatusSigs[1] = [];
        for (var i = 0; i < 4 && i < NumOutputs; i++)
        {
            var outputIndex = i;
            StatusSigs[1].Add((
                new DbcSignal { Name = $"Output{outputIndex + 1}.Current", StartBit = i * 16, Length = 16, Factor = 0.1, Unit = "A" },
                val => Outputs[outputIndex].Current = val
            ));
        }

        // Message 2: Output currents 4-7
        StatusSigs[2] = [];
        for (var i = 4; i < NumOutputs; i++)
        {
            var outputIndex = i;
            StatusSigs[2].Add((
                new DbcSignal { Name = $"Output{outputIndex + 1}.Current", StartBit = (i - 4) * 16, Length = 16, Factor = 0.1, Unit = "A" },
                val => Outputs[outputIndex].Current = val
            ));
        }

        // Message 3: Output states, wiper, flashers
        StatusSigs[3] = [];
        for (var i = 0; i < NumOutputs; i++)
        {
            var outputIndex = i;
            StatusSigs[3].Add((
                new DbcSignal { Name = $"Output{outputIndex + 1}.State", StartBit = i * 4, Length = 4 },
                val => Outputs[outputIndex].State = (OutState)val
            ));
        }
        StatusSigs[3].AddRange(new List<(DbcSignal, Action<double>)>
        {
            (new DbcSignal { Name = "WiperSlowState", StartBit = 32, Length = 1 },
                val => Wipers.SlowState = val != 0),
            (new DbcSignal { Name = "WiperFastState", StartBit = 33, Length = 1 },
                val => Wipers.FastState = val != 0),
            (new DbcSignal { Name = "WiperSpeed", StartBit = 40, Length = 4 },
                val => Wipers.Speed = (WiperSpeed)val),
            (new DbcSignal { Name = "WiperState", StartBit = 44, Length = 4 },
                val => Wipers.State = (WiperState)val)
        });
        for (var i = 0; i < NumFlashers; i++)
        {
            var flasherIndex = i;
            StatusSigs[3].Add((
                new DbcSignal { Name = $"Flasher{flasherIndex + 1}", StartBit = 48 + i, Length = 1 },
                val => Flashers[flasherIndex].Value = val != 0 && Flashers[flasherIndex].Enabled
            ));
        }

        // Message 4: Output reset counts
        StatusSigs[4] = [];
        for (var i = 0; i < NumOutputs; i++)
        {
            var outputIndex = i;
            StatusSigs[4].Add((
                new DbcSignal { Name = $"Output{outputIndex + 1}.ResetCount", StartBit = i * 8, Length = 8 },
                val => Outputs[outputIndex].ResetCount = (int)val
            ));
        }

        // Message 5: CAN inputs & virtual inputs
        StatusSigs[5] = [];
        for (var i = 0; i < NumCanInputs; i++)
        {
            var canInputIndex = i;
            StatusSigs[5].Add((
                new DbcSignal { Name = $"CanInput{canInputIndex + 1}", StartBit = i, Length = 1 },
                val => CanInputs[canInputIndex].Output = val != 0
            ));
        }
        for (var i = 0; i < NumVirtualInputs; i++)
        {
            var virtualInputIndex = i;
            StatusSigs[5].Add((
                new DbcSignal { Name = $"VirtualInput{virtualInputIndex + 1}", StartBit = 32 + i, Length = 1 },
                val => VirtualInputs[virtualInputIndex].Value = val != 0
            ));
        }

        // Message 6: Counters & conditions
        StatusSigs[6] = [];
        for (var i = 0; i < NumCounters; i++)
        {
            var counterIndex = i;
            StatusSigs[6].Add((
                new DbcSignal { Name = $"Counter{counterIndex + 1}", StartBit = i * 8, Length = 8 },
                val => Counters[counterIndex].Value = (int)val
            ));
        }
        for (var i = 0; i < NumConditions; i++)
        {
            var conditionIndex = i;
            StatusSigs[6].Add((
                new DbcSignal { Name = $"Condition{conditionIndex + 1}", StartBit = 32 + i, Length = 1 },
                val => Conditions[conditionIndex].Value = (int)val
            ));
        }

        // Messages 7-14: CAN input values (4 per message)
        for (var msg = 7; msg <= 14; msg++)
        {
            StatusSigs[msg] = [];
            for (var i = 0; i < 4; i++)
            {
                var canInputIndex = (msg - 7) * 4 + i;
                if (canInputIndex < NumCanInputs)
                {
                    StatusSigs[msg].Add((
                        new DbcSignal { Name = $"CanInput{canInputIndex + 1}.Value", StartBit = i * 16, Length = 16 },
                        val => CanInputs[canInputIndex].Value = (ushort)val
                    ));
                }
            }
        }

        // Message 15: Output duty cycles
        StatusSigs[15] = [];
        for (var i = 0; i < NumOutputs; i++)
        {
            var outputIndex = i;
            StatusSigs[15].Add((
                new DbcSignal { Name = $"Output{outputIndex + 1}.DutyCycle", StartBit = i * 8, Length = 8, Unit = "%" },
                val => Outputs[outputIndex].CurrentDutyCycle = val
            ));
        }
    }

    private void InitVarMap()
    {
        VarMap = [];
        
        var index = 0;

        VarMap.Add(new DeviceVariable
        {
            FunctionName = "None",
            FunctionIndex = 0,
            PropertyName = "Value",
            DataType = "bool",
            VariableIndex = index++,
            SingleVariable = true
        });
        
        VarMap.Add(new DeviceVariable
        {
            FunctionName = "AlwaysOn",
            FunctionIndex = 0,
            PropertyName = "Value",
            DataType = "bool",
            VariableIndex = index++,
            SingleVariable = true
        });
        
        VarMap.Add(new DeviceVariable
        {
            FunctionName = "State",
            FunctionIndex = 0,
            PropertyName = "Value",
            DataType = "int",
            VariableIndex = index++,
            SingleVariable = true
        });
        
        VarMap.Add(new DeviceVariable
        {
            FunctionName = "Temperature",
            FunctionIndex = 0,
            PropertyName = "Value",
            DataType = "float",
            VariableIndex = index++,
            SingleVariable = true
        });
        
        VarMap.Add(new DeviceVariable
        {
            FunctionName = "Battery Voltage",
            FunctionIndex = 0,
            PropertyName = "Value",
            DataType = "float",
            VariableIndex = index++,
            SingleVariable = true
        });
        
        if (NumDigitalInputs > 0)
        {
            for (var i = 0; i < NumDigitalInputs; i++)
            {
                VarMap.Add(new DeviceVariable
                {
                    FunctionName = "Input",
                    FunctionIndex = i + 1,
                    PropertyName = "State",
                    DataType = "bool",
                    VariableIndex = index++,
                    SingleVariable = false
                });
            }
        }
        
        if (NumCanInputs > 0)
        {
            for (var i = 0; i < NumCanInputs; i++)
            {
                VarMap.Add(new DeviceVariable
                {
                    FunctionName = "CANInput",
                    FunctionIndex = i + 1,
                    PropertyName = "State",
                    DataType = "bool",
                    VariableIndex = index++,
                    SingleVariable = false
                });
                VarMap.Add(new DeviceVariable
                {
                    FunctionName = "CANInput",
                    FunctionIndex = i + 1,
                    PropertyName = "Value",
                    DataType = "float",
                    VariableIndex = index++,
                    SingleVariable = false
                });
            }
        }
        
        if (NumVirtualInputs > 0)
        {
            for(var i=0; i< NumVirtualInputs; i++)
            {
                VarMap.Add(new DeviceVariable
                {
                    FunctionName = "VirtualInput",
                    FunctionIndex = i + 1,
                    PropertyName = "State",
                    DataType = "bool",
                    VariableIndex = index++,
                    SingleVariable = false
                });
            }  
        }
        
        if (NumOutputs > 0)
        {
            for (var i = 0; i < NumOutputs; i++)
            {
                VarMap.Add(new DeviceVariable
                {
                    FunctionName = "Output",
                    FunctionIndex = i + 1,
                    PropertyName = "On",
                    DataType = "bool",
                    VariableIndex = index++,
                    SingleVariable = false
                });
                VarMap.Add(new DeviceVariable
                {
                    FunctionName = "Output",
                    FunctionIndex = i + 1,
                    PropertyName = "Current",
                    DataType = "float",
                    VariableIndex = index++,
                    SingleVariable = false
                });
                VarMap.Add(new DeviceVariable
                {
                    FunctionName = "Output",
                    FunctionIndex = i + 1,
                    PropertyName = "Overcurrent",
                    DataType = "bool",
                    VariableIndex = index++,
                    SingleVariable = false
                });
                VarMap.Add(new DeviceVariable
                {
                    FunctionName = "Output",
                    FunctionIndex = i + 1,
                    PropertyName = "Fault",
                    DataType = "bool",
                    VariableIndex = index++,
                    SingleVariable = false
                });
            }
        }
        
        if (NumFlashers > 0)
        {
            for (var i = 0; i < NumFlashers; i++)
            {
                VarMap.Add(new DeviceVariable
                {
                    FunctionName = "Flasher",
                    FunctionIndex = i + 1,
                    PropertyName = "State",
                    DataType = "bool",
                    VariableIndex = index++,
                    SingleVariable = false
                });
            }
        }
        
        if (NumConditions > 0)
        {
            for (var i = 0; i < NumConditions; i++)
            {
                VarMap.Add(new DeviceVariable
                {
                    FunctionName = "Condition",
                    FunctionIndex = i + 1,
                    PropertyName = "Value",
                    DataType = "bool",
                    VariableIndex = index++,
                    SingleVariable = false
                });
            }
        }
        
        if (NumCounters > 0)
        {
            for (var i = 0; i < NumCounters; i++)
            {
                VarMap.Add(new DeviceVariable
                {
                    FunctionName = "Counter",
                    FunctionIndex = i + 1,
                    PropertyName = "Value",
                    DataType = "int",
                    VariableIndex = index++,
                    SingleVariable = false
                });
            }
        }
        
        VarMap.Add(new DeviceVariable
        {
            FunctionName = "Wiper.Slow",
            FunctionIndex = 0,
            PropertyName = "Output",
            DataType = "bool",
            VariableIndex = index++,
            SingleVariable = true
        });
        
        VarMap.Add(new DeviceVariable
        {
            FunctionName = "Wiper.Fast",
            FunctionIndex = 0,
            PropertyName = "Output",
            DataType = "bool",
            VariableIndex = index++,
            SingleVariable = true
        });
        
        VarMap.Add(new DeviceVariable
        {
            FunctionName = "Wiper.Park",
            FunctionIndex = 0,
            PropertyName = "Output",
            DataType = "bool",
            VariableIndex = index++,
            SingleVariable = true
        });
        
        VarMap.Add(new DeviceVariable
        {
            FunctionName = "Wiper.Inter",
            FunctionIndex = 0,
            PropertyName = "Output",
            DataType = "bool",
            VariableIndex = index++,
            SingleVariable = true
        });
        
        VarMap.Add(new DeviceVariable
        {
            FunctionName = "Wiper.Wash",
            FunctionIndex = 0,
            PropertyName = "Output",
            DataType = "bool",
            VariableIndex = index++,
            SingleVariable = true
        });
        
        VarMap.Add(new DeviceVariable
        {
            FunctionName = "Wiper.Swipe",
            FunctionIndex = 0,
            PropertyName = "Output",
            DataType = "bool",
            VariableIndex = index++,
            SingleVariable = true
        });
        
        if (NumKeypads > 0)
        {
            for (var i = 0; i < NumKeypads; i++)
            {
                for (var j = 0; j < KeypadMaxButtons; j++)
                {
                    VarMap.Add(new DeviceVariable
                    {
                        FunctionName = $"Keypad{i + 1}.Button",
                        FunctionIndex = j + 1,
                        PropertyName = "State",
                        DataType = "bool",
                        VariableIndex = index++,
                        SingleVariable = false
                    });
                }
                
                for (var j = 0; j < KeypadMaxDials; j++)
                {
                    VarMap.Add(new DeviceVariable
                    {
                        FunctionName = $"Keypad{i + 1}.Dial",
                        FunctionIndex = j + 1,
                        PropertyName = "Position",
                        DataType = "int",
                        VariableIndex = index++,
                        SingleVariable = false
                    });
                }
                
                for (var j = 0; j < KeypadMaxAnalogInputs; j++)
                {
                    VarMap.Add(new DeviceVariable
                    {
                        FunctionName = $"Keypad{i + 1}.AnalogInput",
                        FunctionIndex = j + 1,
                        PropertyName = "Value",
                        DataType = "float",
                        VariableIndex = index++,
                        SingleVariable = false
                    });
                }
            }
        }
    }

    private void InitParams()
    {
        var allParams = new List<DeviceParameter>();
        foreach (var input in Inputs) allParams.AddRange(input.Params);
        foreach (var output in Outputs) allParams.AddRange(output.Params);
        foreach (var canInput in CanInputs) allParams.AddRange(canInput.Params);
        foreach (var virtualInput in VirtualInputs) allParams.AddRange(virtualInput.Params);
        foreach (var flasher in Flashers) allParams.AddRange(flasher.Params);
        foreach (var counter in Counters) allParams.AddRange(counter.Params);
        foreach (var condition in Conditions) allParams.AddRange(condition.Params);
        allParams.AddRange(Wipers.Params);
        allParams.AddRange(StarterDisable.Params);
        Params = allParams;
    }

    private void Clear()
    {
        foreach(var input in Inputs)
            input.State = false;

        foreach(var output in Outputs)
        {
            output.Current = 0;
            output.State = OutState.Off;
        }

        foreach(var input in VirtualInputs)
            input.Value = false;

        foreach(var canInput in CanInputs)
            canInput.Output = false;
        
        Logger.LogDebug("PDM {Name} cleared", Name);
    }

    public void UpdateIsConnected()
    {
        var timeSpan = DateTime.Now - LastRxTime;
        Connected = timeSpan.TotalMilliseconds < 500;
    }
    
    public bool InIdRange(int id)
    {
        return (id >= BaseId - 1) && (id <= BaseId + 31);
    }
    
    public void Read(int id, byte[] data, 
            ref ConcurrentDictionary<(int BaseId, int Index, int SubIndex), DeviceCanFrame> queue, 
            List<DeviceCanFrame> outgoing)
    {
        var offset = id - BaseId;

        // Use dictionary lookup for status messages 0-15
        if (StatusSigs.TryGetValue(offset, out var signals))
        {
            foreach (var (signal, setValue) in signals)
            {
                var value = ExtractSignal(data, signal);
                setValue(value);
            }
        }
        // Handle special messages with custom logic
        else
        {
            switch (offset)
            {
                case 30: ReadParamResponse(data, queue, outgoing); break;
                case 31: ReadInfoWarnErrorMessage(data); break;
            }
        }

        LastRxTime = DateTime.Now;
    }

    public IEnumerable<(int MessageId, DbcSignal Signal)> GetStatusSigs()
    {
        foreach (var kvp in StatusSigs)
        {
            int messageId = BaseId + kvp.Key;
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

    protected void ReadParamResponse(byte[] data, 
                    ConcurrentDictionary<(int BaseId, int Index, int SubIndex), DeviceCanFrame> queue, 
                    List<DeviceCanFrame> outgoing)
    {
        DeviceCanFrame canFrame;
        int index, subIndex;
        DeviceParameter matchingParam;
        double rawValue;
        object convertedValue;
        (int BaseId, int, int) key;

        switch ((MessageCommand)data[0])
        {
            case MessageCommand.Read:
            case MessageCommand.Write:
                if (data.Length != 8) return;

                index = data[2] << 8 | data[1];
                subIndex = data[3];

                matchingParam = Params.First(p => p.Index == index && p.SubIndex == subIndex);

                rawValue = DbcSignalCodec.ExtractSignal(data, startBit: 32, length: 32);

                // Convert to the appropriate type based on param.ValueType
                convertedValue = matchingParam.ValueType switch
                {
                    { } t when t == typeof(bool) => rawValue != 0,
                    { } t when t == typeof(int) => (int)rawValue,
                    { } t when t == typeof(uint) => (uint)rawValue,
                    { } t when t == typeof(float) => (float)rawValue,
                    { } t when t == typeof(double) => rawValue,
                    { IsEnum: true } t => Enum.ToObject(t, (int)rawValue),
                    _ => rawValue
                };
                
                matchingParam.SetValue(convertedValue);

                key = (BaseId, index, subIndex);
                if (queue.TryGetValue(key, out canFrame!))
                {
                    canFrame.TimeSentTimer?.Dispose();
                    queue.TryRemove(key, out _);
                }

                break;

            case MessageCommand.ReadAll:
                if (data.Length != 8) return;

                index = data[2] << 8 | data[1];
                subIndex = data[3];
                
                TempParamValues.Clear();
                foreach (var param in Params)
                    TempParamValues[(param.Index, param.SubIndex)] = param.DefaultValue;

                _readAllCount = 0;
                
                key = (BaseId, index, subIndex);
                if (queue.TryGetValue(key, out canFrame!))
                {
                    canFrame.TimeSentTimer?.Dispose();
                    queue.TryRemove(key, out _);
                }
                
                Logger.LogInformation("{Name} ID: {BaseId}, Read All Started", Name, BaseId);

                break;
                
            case MessageCommand.ReadAllRsp:
                if (data.Length != 8) return;

                index = data[2] << 8 | data[1];
                subIndex = data[3];
                
                matchingParam = Params.First(p => p.Index == index && p.SubIndex == subIndex);

                rawValue = DbcSignalCodec.ExtractSignal(data, startBit: 32, length: 32);

                // Convert to the appropriate type based on param.ValueType
                convertedValue = matchingParam.ValueType switch
                {
                    { } t when t == typeof(bool) => rawValue != 0,
                    { } t when t == typeof(int) => (int)rawValue,
                    { } t when t == typeof(uint) => (uint)rawValue,
                    { } t when t == typeof(float) => (float)rawValue,
                    { } t when t == typeof(double) => rawValue,
                    { IsEnum: true } t => Enum.ToObject(t, (int)rawValue),
                    _ => rawValue
                };
                
                TempParamValues[(index, subIndex)] = convertedValue;

                _readAllCount++;
                
                break;
            
            case MessageCommand.ReadAllComplete:
                if (data.Length != 8) return;

                var readAllCount = data[2] << 8 | data[1];

                if (readAllCount == _readAllCount)
                {
                    // End of params, apply all temporary values to actual properties
                    foreach (var param in Params)
                    {
                        var paramKey = (param.Index, param.SubIndex);
                        if (TempParamValues.TryGetValue(paramKey, out var value))
                        {
                            param.SetValue(value);
                        }
                    }

                    TempParamValues.Clear();
                    Logger.LogInformation("{Name} ID: {BaseId}, Read All Complete", Name, BaseId);
                }
                else
                {
                    TempParamValues.Clear();
                    Logger.LogWarning("{Name} ID: {BaseId}, Read All Incomplete {fromPdm} vs {received}", 
                                        Name, BaseId, readAllCount, _readAllCount);

                    outgoing.Add(new DeviceCanFrame
                    {
                        DeviceBaseId = BaseId,
                        SendOnly = true,
                        Frame = new CanFrame(
                            Id: BaseId - 1,
                            Len: 8,
                            Payload: [Convert.ToByte(MessageCommand.ReadAll), 0, 0, 0, 0, 0, 0, 0])
                    });
                }

                break;
            
            case MessageCommand.WriteAll:
                if (data.Length != 8) return;

                index = data[2] << 8 | data[1];
                subIndex = data[3];

                key = (BaseId, index, subIndex);
                if (queue.TryGetValue(key, out canFrame!))
                {
                    canFrame.TimeSentTimer?.Dispose();
                    queue.TryRemove(key, out _);
                }
                
                Logger.LogInformation("{Name} ID: {BaseId}, Write All Started", Name, BaseId);
                
                //Write all modified values
                var modifiedParams = Params.Where(p => p.IsModified).ToList();
                List<DeviceCanFrame> msgs = [];
                _writeAllCount = modifiedParams.Count;

                foreach (var parameter in modifiedParams)
                {
                    msgs.Add(new DeviceCanFrame
                    {
                        DeviceBaseId = BaseId,
                        SendOnly = true,
                        Frame = ParamCodec.ToFrame(MessageCommand.WriteAllVal, parameter, BaseId - 1)
                    });
                }

                //Write all complete, with num params
                msgs.Add(new DeviceCanFrame
                {
                    DeviceBaseId = BaseId,
                    SendOnly = true,
                    Frame = new CanFrame(
                        Id: BaseId - 1,
                        Len: 8,
                        Payload:[   Convert.ToByte(MessageCommand.WriteAllComplete),
                                    Convert.ToByte((_writeAllCount >> 8) & 0xFF),
                                    Convert.ToByte(_writeAllCount & 0xFF), 0, 0, 0, 0, 0])
                });

                outgoing.AddRange(msgs);
                
                break;
            
            case MessageCommand.WriteAllComplete:
                if (data.Length != 8) return;
                
                var writeAllCount = data[2] << 8 | data[1];

                if (writeAllCount == _writeAllCount)
                {
                    Logger.LogInformation("{Name} ID: {BaseId}, Write All Completed", Name, BaseId);
                }
                else
                {
                    Logger.LogWarning("{Name} ID: {BaseId}, Write All Incomplete {fromPdm} vs {received}", 
                                        Name, BaseId, writeAllCount, _writeAllCount);

                    outgoing.Add(new DeviceCanFrame
                    {
                        DeviceBaseId = BaseId,
                        Frame = new CanFrame(
                            Id: BaseId - 1,
                            Len: 8,
                            Payload: [Convert.ToByte(MessageCommand.WriteAll), 0, 0, 0, 0, 0, 0, 0])
                    });
                }
                break;
            
            case MessageCommand.Version:
                if (data.Length != 8) return;
                
                Version = $"v{data[1]}.{data[2]}.{(data[3] << 8) + (data[4])}";
                
                key = (BaseId, (int)MessageCommand.Version, 0);
                if (queue.TryGetValue(key, out canFrame!))
                {
                    canFrame.TimeSentTimer?.Dispose();
                    queue.TryRemove(key, out _);
                }
                
                Logger.LogInformation("{Name} FW version received: {Version}", Name, Version);
                
                if (!CheckVersion(data[1], data[2], (data[3] << 8) + (data[4])))
                {
                    Logger.LogError("{Name} ID: {BaseId}, Firmware needs to be updated. V{MinMajorVersion}.{MinMinorVersion}.{MinBuildVersion} or greater", 
                                        Name, BaseId, MinMajorVersion, MinMinorVersion, MinBuildVersion);
                }

                break;

			case MessageCommand.BurnParams:
                if (data.Length != 8) return;
                
                if (data[1] == 1) //Successful burn
                {
                    Logger.LogInformation("{Name} ID: {BaseId}, Burn Successful", Name, BaseId);

                    key = (BaseId, (int)MessageCommand.BurnParams, 0);
                    if (queue.TryGetValue(key, out canFrame!))
                    {
                        canFrame.TimeSentTimer?.Dispose();
                        queue.TryRemove(key, out _);
                    }
                }

                if (data[1] == 0) //Unsuccessful burn
                    Logger.LogError("{Name} ID: {BaseId}, Burn Failed", Name, BaseId);
                
                break;

            case MessageCommand.Sleep:
                if (data.Length != 8) return;
                
                if (data[1] == 1) //Successful sleep
                {
                    Logger.LogInformation("{Name} ID: {BaseId}, Sleep Successful", Name, BaseId);

                    key = (BaseId, (int)MessageCommand.Sleep, 0);
                    if (queue.TryGetValue(key, out canFrame!))
                    {
                        canFrame.TimeSentTimer?.Dispose();
                        queue.TryRemove(key, out _);
                    }
                }

                if (data[1] == 0) //Unsuccessful sleep
                    Logger.LogError("{Name} ID: {BaseId}, Sleep Failed", Name, BaseId);
                
                break;
        }
    }

    protected void ReadInfoWarnErrorMessage(byte[] data)
    {
        //Response is lowercase version of set/get prefix
        var type = (MessageType)char.ToUpper(Convert.ToChar(data[0]));
        var src = (MessageSrc)data[1];

        switch (type)
        {
            case MessageType.Info:
                Logger.LogInformation("{Name} ID: {BaseId}, Src: {MessageSrc} {I} {I1} {I2}", 
                    Name, BaseId, src, (data[3] << 8) + data[2], (data[5] << 8) + data[4], (data[7] << 8) + data[6]);
                break;
            case MessageType.Warning:
                Logger.LogWarning("{Name} ID: {BaseId}, Src: {MessageSrc} {I} {I1} {I2}", 
                    Name, BaseId, src, (data[3] << 8) + data[2], (data[5] << 8) + data[4], (data[7] << 8) + data[6]);
                break;
            case MessageType.Error:
                Logger.LogError("{Name} ID: {BaseId}, Src: {MessageSrc} {I} {I1} {I2}", 
                    Name, BaseId, src, (data[3] << 8) + data[2], (data[5] << 8) + data[4], (data[7] << 8) + data[6]);
                break;
        }
    }

    public List<DeviceCanFrame> GetReadMsgs()
    {
        var id = BaseId;

        List<DeviceCanFrame>  msgs =
        [
            new()
            {
                DeviceBaseId = BaseId,
                SendOnly = true,
                Frame = new CanFrame(
                    Id: id - 1,
                    Len: 8,
                    Payload: [Convert.ToByte(MessageCommand.ReadAll), 0, 0, 0, 0, 0, 0, 0]),
            }
        ];

		return msgs;
    }

    public List<DeviceCanFrame> GetWriteMsgs()
    {
        //Start WriteAll 
        List<DeviceCanFrame> msgs =
        [
            new()
            {
                DeviceBaseId = BaseId,
                Frame = new CanFrame
                (
                    Id: BaseId - 1,
                    Len: 8,
                    Payload: [Convert.ToByte(MessageCommand.WriteAll), 0, 0, 0, 0, 0, 0, 0]
                )
            }
        ];

        return msgs;
    }

    public List<DeviceCanFrame> GetModifyMsgs(int newId)
    {
        var modifyParams = Params.Where(p => p is { Index: 0x0010, SubIndex: 1 }).ToList();

        List<DeviceCanFrame> msgs = [];

        foreach (var parameter in modifyParams)
        {
            msgs.Add(new DeviceCanFrame
            {
                SendOnly = true,
                DeviceBaseId = newId,
                Frame = ParamCodec.ToFrame(MessageCommand.Write, parameter, BaseId - 1)
            });
        }
        
        return msgs;
    }

    public DeviceCanFrame GetBurnMsg()
    {
        return new DeviceCanFrame
        {
            DeviceBaseId = BaseId,
            Frame = new CanFrame
            (
                Id: BaseId - 1,
                Len: 8,
                Payload: [Convert.ToByte(MessageCommand.BurnParams), 1, 3, 8, 0, 0, 0, 0]
            )
        };
    }

    public DeviceCanFrame GetSleepMsg()
    {
        return new DeviceCanFrame
        {
            SendOnly = true,
            DeviceBaseId = BaseId,
            Frame = new CanFrame
            (
                Id: BaseId - 1,
                Len: 8,
                Payload: [Convert.ToByte(MessageCommand.Sleep), Convert.ToByte('Q'), Convert.ToByte('U'), 
                            Convert.ToByte('I'), Convert.ToByte('T'), 0, 0, 0
                ]
            )
        };
    }

    public DeviceCanFrame GetVersionMsg()
    {
        return new DeviceCanFrame
        {
            DeviceBaseId = BaseId,
            Frame = new CanFrame
            (
                Id: BaseId - 1,
                Len: 8,
                Payload: [Convert.ToByte(MessageCommand.Version), 0, 0, 0, 0, 0, 0, 0]
            )
        };
    }

    public DeviceCanFrame GetWakeupMsg()
    {
        return new DeviceCanFrame
        {
            SendOnly = true,
            DeviceBaseId = BaseId,
            Frame = new CanFrame
            (
                Id: BaseId - 1,
                Len: 8,
                Payload: [Convert.ToByte('!'), 0, 0, 0, 0, 0, 0, 0]
            )
        };
    }

    public DeviceCanFrame GetBootloaderMsg()
    {
        return new DeviceCanFrame
        {
            SendOnly = true,
            DeviceBaseId = BaseId,
            Frame = new CanFrame
            (
                Id: BaseId - 1,
                Len: 8,
                Payload: [
                    Convert.ToByte(MessageCommand.Bootloader), (byte)'B', (byte)'O', (byte)'O', (byte)'T', (byte)'L', 0,
                    0
                ]
            )
        };
    }
    
    public List<CanFrame> GetCyclicMsgs()
    {
        return [];
    }

    // Collection accessors
    public IReadOnlyList<Input> GetInputs() => Inputs.AsReadOnly();
    public IReadOnlyList<Output> GetOutputs() => Outputs.AsReadOnly();
    public IReadOnlyList<CanInput> GetCanInputs() => CanInputs.AsReadOnly();
    public IReadOnlyList<VirtualInput> GetVirtualInputs() => VirtualInputs.AsReadOnly();
    public IReadOnlyList<Flasher> GetFlashers() => Flashers.AsReadOnly();
    public IReadOnlyList<Counter> GetCounters() => Counters.AsReadOnly();
    public IReadOnlyList<Condition> GetConditions() => Conditions.AsReadOnly();
    public Wiper GetWipers() => Wipers;
    public StarterDisable GetStarterDisable() => StarterDisable;

    protected bool CheckVersion(int major, int minor, int build)
    {
        if (major > MinMajorVersion)
            return true;

        if ((major == MinMajorVersion) && (minor > MinMinorVersion))
            return true;

        if ((major == MinMajorVersion) && (minor == MinMinorVersion) && (build >= MinBuildVersion))
            return true;

        return false;
    }
}