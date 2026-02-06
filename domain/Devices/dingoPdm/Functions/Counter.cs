using System.Text.Json.Serialization;
using domain.Common;
using domain.Devices.dingoPdm.Enums;
using domain.Interfaces;
using domain.Models;

namespace domain.Devices.dingoPdm.Functions;

public class Counter : IDeviceFunction
{
    [JsonIgnore] public const int BaseIndex = 0x1600;
    [JsonPropertyName("name")] public string Name {get; set; }
    [JsonPropertyName("number")] public int Number {get;}
    [JsonPropertyName("enabled")] public bool Enabled {get; set;}
    [JsonPropertyName("incInput")] public DeviceVariable IncInput { get; set; } = new();
    [JsonPropertyName("decInput")] public DeviceVariable DecInput { get; set; } = new();
    [JsonPropertyName("resetInput")] public  DeviceVariable ResetInput { get; set; } = new();
    [JsonPropertyName("minCount")] public int  MinCount {get; set;}
    [JsonPropertyName("maxCount")] public int  MaxCount {get; set;}
    [JsonPropertyName("incEdge")] public InputEdge IncEdge {get; set;}
    [JsonPropertyName("decEdge")] public InputEdge DecEdge {get; set;}
    [JsonPropertyName("resetEdge")] public InputEdge ResetEdge {get; set;}
    [JsonPropertyName("wrapAround")] public bool WrapAround {get; set;}
    [JsonPropertyName("holdToReset")] public bool HoldToReset {get; set;}
    [JsonPropertyName("resetTime")] public int  ResetTime {get; set;}
    
    [JsonIgnore][Plotable(displayName:"State")] public int Value {get; set;}

    [JsonIgnore] public List<DeviceParameter> Params { get; }

    [JsonConstructor]
    public Counter(int number, string name)
    {
        Number = number;
        Name = name;
        Params = InitParams();
    }

    private List<DeviceParameter> InitParams()
    {
        var subIndex = 0;
        return
        [
            new DeviceParameter
            {
                ParentName = Name, Name = "enabled", Index = BaseIndex + (Number - 1), SubIndex = subIndex++,
                GetValue = () => Enabled, SetValue = val => Enabled = (bool)val,
                ValueType = Enabled.GetType(),
                DefaultValue = false
            },
            new DeviceParameter
            {
                ParentName = Name, Name = "incInput", Index = BaseIndex + (Number - 1), SubIndex = subIndex++,
                GetValue = () => IncInput.VariableIndex, SetValue = val => IncInput.VariableIndex = (int)val,
                ValueType = IncInput.VariableIndex.GetType(),
                DefaultValue = 0
            },
            new DeviceParameter
            {
                ParentName = Name, Name = "decInput", Index = BaseIndex + (Number - 1), SubIndex = subIndex++,
                GetValue = () => DecInput.VariableIndex, SetValue = val => DecInput.VariableIndex = (int)val,
                ValueType = DecInput.VariableIndex.GetType(),
                DefaultValue = 0
            },
            new DeviceParameter
            {
                ParentName = Name, Name = "resetInput", Index = BaseIndex + (Number - 1), SubIndex = subIndex++,
                GetValue = () => ResetInput.VariableIndex, SetValue = val => ResetInput.VariableIndex = (int)val,
                ValueType = ResetInput.VariableIndex.GetType(),
                DefaultValue = 0
            },
            new DeviceParameter
            {
                ParentName = Name, Name = "minCount", Index = BaseIndex + (Number - 1), SubIndex = subIndex++,
                GetValue = () => MinCount, SetValue = val => MinCount = (int)val,
                ValueType = MinCount.GetType(),
                DefaultValue = 0
            },
            new DeviceParameter
            {
                ParentName = Name, Name = "maxCount", Index = BaseIndex + (Number - 1), SubIndex = subIndex++,
                GetValue = () => MaxCount, SetValue = val => MaxCount = (int)val,
                ValueType = MaxCount.GetType(),
                DefaultValue = 0
            },
            new DeviceParameter
            {
                ParentName = Name, Name = "incEdge", Index = BaseIndex + (Number - 1), SubIndex = subIndex++,
                GetValue = () => IncEdge, SetValue = val => IncEdge = (InputEdge)val,
                ValueType = IncEdge.GetType(),
                DefaultValue = InputEdge.Rising
            },
            new DeviceParameter
            {
                ParentName = Name, Name = "decEdge", Index = BaseIndex + (Number - 1), SubIndex = subIndex++,
                GetValue = () => DecEdge, SetValue = val => DecEdge = (InputEdge)val,
                ValueType = DecEdge.GetType(),
                DefaultValue = InputEdge.Rising
            },
            new DeviceParameter
            {
                ParentName = Name, Name = "resetEdge", Index = BaseIndex + (Number - 1), SubIndex = subIndex++,
                GetValue = () => ResetEdge, SetValue = val => ResetEdge = (InputEdge)val,
                ValueType = ResetEdge.GetType(),
                DefaultValue = InputEdge.Rising
            },
            new DeviceParameter
            {
                ParentName = Name, Name = "wrapAround", Index = BaseIndex + (Number - 1), SubIndex = subIndex++,
                GetValue = () => WrapAround, SetValue = val => WrapAround = (bool)val,
                ValueType = WrapAround.GetType(),
                DefaultValue = false
            },
            new DeviceParameter
            {
                ParentName = Name, Name = "holdToReset", Index = BaseIndex + (Number - 1), SubIndex = subIndex++,
                GetValue = () => HoldToReset, SetValue = val => HoldToReset = (bool)val,
                ValueType = HoldToReset.GetType(),
                DefaultValue = false
            },
            new DeviceParameter
            {
                ParentName = Name, Name = "resetTime", Index = BaseIndex + (Number - 1), SubIndex = subIndex++,
                GetValue = () => ResetTime, SetValue = val => ResetTime = (int)val,
                ValueType = ResetTime.GetType(),
                DefaultValue = 0
            }
        ];
    }
}