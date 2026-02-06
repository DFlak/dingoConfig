using System.Text.Json.Serialization;
using domain.Common;
using domain.Devices.dingoPdm.Enums;
using domain.Interfaces;
using domain.Models;

namespace domain.Devices.dingoPdm.Functions;

public class Wiper : IDeviceFunction
{
    [JsonIgnore] public const int BaseIndex = 0x1900;
    [JsonPropertyName("name")] public string Name {get; set;}
    [JsonIgnore] public int Number => 1; // Singleton function
    [JsonPropertyName("enabled")] public bool Enabled {get; set;}
    [JsonPropertyName("mode")] public WiperMode Mode { get; set; }
    [JsonPropertyName("slowInput")] public DeviceVariable SlowInput { get; set; } = new();
    [JsonPropertyName("fastInput")] public DeviceVariable FastInput { get; set; } = new();
    [JsonPropertyName("interInput")] public DeviceVariable InterInput { get; set; } = new();
    [JsonPropertyName("onInput")] public DeviceVariable OnInput { get; set; } = new();
    [JsonPropertyName("speedInput")] public DeviceVariable SpeedInput { get; set; } = new();
    [JsonPropertyName("parkInput")] public DeviceVariable ParkInput { get; set; } = new();
    [JsonPropertyName("parkStopLevel")] public bool ParkStopLevel { get; set; }
    [JsonPropertyName("swipeInput")] public DeviceVariable SwipeInput { get; set; } = new();
    [JsonPropertyName("washInput")] public DeviceVariable WashInput { get; set; } = new();
    [JsonPropertyName("washWipeCycles")] public int WashWipeCycles { get; set; }
    [JsonPropertyName("speedMap")] public WiperSpeed[] SpeedMap { get; set; } = new WiperSpeed[8];
    [JsonPropertyName("intermitTime")] public double[] IntermitTime { get; set; } = new double[6];

    [JsonIgnore][Plotable(displayName:"SlowState")] public bool SlowState { get; set; }
    [JsonIgnore][Plotable(displayName:"FastState")] public bool FastState { get; set; }
    [JsonIgnore][Plotable(displayName:"State")] public WiperState State { get; set; }
    [JsonIgnore][Plotable(displayName:"Speed")] public WiperSpeed Speed { get; set; }

    [JsonIgnore] public List<DeviceParameter> Params { get; }

    [JsonConstructor]
    public Wiper(string name)
    {
        Name = name;
        Params = InitParams();
    }

    private List<DeviceParameter> InitParams()
    {
        var subIndex = 0;
        var parameters = new List<DeviceParameter>
        {
            new DeviceParameter
            {
                ParentName = Name, Name = "enabled", Index = BaseIndex + (Number - 1), SubIndex = subIndex++,
                GetValue = () => Enabled, SetValue = val => Enabled = (bool)val,
                ValueType = Enabled.GetType(),
                DefaultValue = false
            },
            new DeviceParameter
            {
                ParentName = Name, Name = "mode", Index = BaseIndex + (Number - 1), SubIndex = subIndex++,
                GetValue = () => Mode, SetValue = val => Mode = (WiperMode)val,
                ValueType = Mode.GetType(),
                DefaultValue = WiperMode.DigIn
            },
            new DeviceParameter
            {
                ParentName = Name, Name = "slowInput", Index = BaseIndex + (Number - 1), SubIndex = subIndex++,
                GetValue = () => SlowInput.VariableIndex, SetValue = val => SlowInput.VariableIndex = (int)val,
                ValueType = SlowInput.VariableIndex.GetType(),
                DefaultValue = 0
            },
            new DeviceParameter
            {
                ParentName = Name, Name = "fastInput", Index = BaseIndex + (Number - 1), SubIndex = subIndex++,
                GetValue = () => FastInput.VariableIndex, SetValue = val => FastInput.VariableIndex = (int)val,
                ValueType = FastInput.VariableIndex.GetType(),
                DefaultValue = 0
            },
            new DeviceParameter
            {
                ParentName = Name, Name = "interInput", Index = BaseIndex + (Number - 1), SubIndex = subIndex++,
                GetValue = () => InterInput.VariableIndex, SetValue = val => InterInput.VariableIndex = (int)val,
                ValueType = InterInput.VariableIndex.GetType(),
                DefaultValue = 0
            },
            new DeviceParameter
            {
                ParentName = Name, Name = "onInput", Index = BaseIndex + (Number - 1), SubIndex = subIndex++,
                GetValue = () => OnInput.VariableIndex, SetValue = val => OnInput.VariableIndex = (int)val,
                ValueType = OnInput.VariableIndex.GetType(),
                DefaultValue = 0
            },
            new DeviceParameter
            {
                ParentName = Name, Name = "speedInput", Index = BaseIndex + (Number - 1), SubIndex = subIndex++,
                GetValue = () => SpeedInput.VariableIndex, SetValue = val => SpeedInput.VariableIndex = (int)val,
                ValueType = SpeedInput.VariableIndex.GetType(),
                DefaultValue = 0
            },
            new DeviceParameter
            {
                ParentName = Name, Name = "parkInput", Index = BaseIndex + (Number - 1), SubIndex = subIndex++,
                GetValue = () => ParkInput.VariableIndex, SetValue = val => ParkInput.VariableIndex = (int)val,
                ValueType = ParkInput.VariableIndex.GetType(),
                DefaultValue = 0
            },
            new DeviceParameter
            {
                ParentName = Name, Name = "parkStopLevel", Index = BaseIndex + (Number - 1), SubIndex = subIndex++,
                GetValue = () => ParkStopLevel, SetValue = val => ParkStopLevel = (bool)val,
                ValueType = ParkStopLevel.GetType(),
                DefaultValue = false
            },
            new DeviceParameter
            {
                ParentName = Name, Name = "swipeInput", Index = BaseIndex + (Number - 1), SubIndex = subIndex++,
                GetValue = () => SwipeInput.VariableIndex, SetValue = val => SwipeInput.VariableIndex = (int)val,
                ValueType = SwipeInput.VariableIndex.GetType(),
                DefaultValue = 0
            },
            new DeviceParameter
            {
                ParentName = Name, Name = "washInput", Index = BaseIndex + (Number - 1), SubIndex = subIndex++,
                GetValue = () => WashInput.VariableIndex, SetValue = val => WashInput.VariableIndex = (int)val,
                ValueType = WashInput.VariableIndex.GetType(),
                DefaultValue = 0
            },
            new DeviceParameter
            {
                ParentName = Name, Name = "washWipeCycles", Index = BaseIndex + (Number - 1), SubIndex = subIndex++,
                GetValue = () => WashWipeCycles, SetValue = val => WashWipeCycles = (int)val,
                ValueType = WashWipeCycles.GetType(),
                DefaultValue = 0
            }
        };

        for (var i = 0; i < 8; i++)
        {
            var idx = i;
            parameters.Add(new DeviceParameter
            {
                ParentName = Name, Name = $"speedMap[{i}]", Index = BaseIndex + (Number - 1), SubIndex = subIndex++,
                GetValue = () => SpeedMap[idx], SetValue = val => SpeedMap[idx] = (WiperSpeed)val,
                ValueType = SpeedMap[idx].GetType(),
                DefaultValue = WiperSpeed.Park
            });
        }

        for (var i = 0; i < 6; i++)
        {
            var idx = i;
            parameters.Add(new DeviceParameter
            {
                ParentName = Name, Name = $"intermitTime[{i}]", Index = BaseIndex + (Number - 1), SubIndex = subIndex++,
                GetValue = () => IntermitTime[idx], SetValue = val => IntermitTime[idx] = (double)val,
                ValueType = IntermitTime[idx].GetType(),
                DefaultValue = 0.0
            });
        }

        return parameters;
    }
}