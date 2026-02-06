using System.Text.Json.Serialization;
using domain.Common;
using domain.Devices.dingoPdm.Enums;
using domain.Enums;
using domain.Interfaces;
using domain.Models;

namespace domain.Devices.dingoPdm.Functions;

public class CanInput : IDeviceFunction
{
    [JsonIgnore] public const int BaseIndex = 0x1300;
    [JsonPropertyName("name")] public string Name {get; set; }
    [JsonPropertyName("number")] public int Number {get;}
    [JsonPropertyName("enabled")] public bool Enabled {get; set;}
    [JsonPropertyName("timeoutEnabled")] public bool TimeoutEnabled {get; set;}
    [JsonPropertyName("timeout")] public int Timeout {get; set;}
    [JsonPropertyName("ide")] public bool Ide {get; set;}
    [JsonPropertyName("startingByte")] public int StartingByte {get; set;}
    [JsonPropertyName("dlc")] public int Dlc {get; set;}
    [JsonPropertyName("operator")] public Operator Operator {get; set;}
    [JsonPropertyName("onVal")] public int OnVal {get; set;}
    [JsonPropertyName("mode")] public InputMode Mode {get; set;}

    [JsonPropertyName("id")]
    public int Id
    {
        get;
        set
        {
            field = value;
            Ide = (field > 2047);
        }
    }

    [JsonIgnore] public List<DeviceParameter> Params { get; }

    [JsonIgnore][Plotable(displayName:"State")] public bool Output { get; set; }
    [JsonIgnore][Plotable(displayName:"Value")] public int Value {get; set;}
    

    [JsonConstructor]
    public CanInput(int number, string name)
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
                ParentName = Name, Name = "timeoutEnabled", Index = BaseIndex + (Number - 1), SubIndex = subIndex++,
                GetValue = () => TimeoutEnabled, SetValue = val => TimeoutEnabled = (bool)val,
                ValueType = TimeoutEnabled.GetType(),
                DefaultValue = false
            },
            new DeviceParameter
            {
                ParentName = Name, Name = "timeout", Index = BaseIndex + (Number - 1), SubIndex = subIndex++,
                GetValue = () => Timeout, SetValue = val => Timeout = (int)val,
                ValueType = Timeout.GetType(),
                DefaultValue = 0
            },
            new DeviceParameter
            {
                ParentName = Name, Name = "id", Index = BaseIndex + (Number - 1), SubIndex = subIndex++,
                GetValue = () => Id, SetValue = val => Id = (int)val,
                ValueType = Id.GetType(),
                DefaultValue = 0
            },
            new DeviceParameter
            {
                ParentName = Name, Name = "ide", Index = BaseIndex + (Number - 1), SubIndex = subIndex++,
                GetValue = () => Ide, SetValue = val => Ide = (bool)val,
                ValueType = Ide.GetType(),
                DefaultValue = false
            },
            new DeviceParameter
            {
                ParentName = Name, Name = "startingByte", Index = BaseIndex + (Number - 1), SubIndex = subIndex++,
                GetValue = () => StartingByte, SetValue = val => StartingByte = (int)val,
                ValueType = StartingByte.GetType(),
                DefaultValue = 0
            },
            new DeviceParameter
            {
                ParentName = Name, Name = "dlc", Index = BaseIndex + (Number - 1), SubIndex = subIndex++,
                GetValue = () => Dlc, SetValue = val => Dlc = (int)val,
                ValueType = Dlc.GetType(),
                DefaultValue = 0
            },
            new DeviceParameter
            {
                ParentName = Name, Name = "operator", Index = BaseIndex + (Number - 1), SubIndex = subIndex++,
                GetValue = () => Operator, SetValue = val => Operator = (Operator)val,
                ValueType = Operator.GetType(),
                DefaultValue = Operator.Equal
            },
            new DeviceParameter
            {
                ParentName = Name, Name = "onVal", Index = BaseIndex + (Number - 1), SubIndex = subIndex++,
                GetValue = () => OnVal, SetValue = val => OnVal = (int)val,
                ValueType = OnVal.GetType(),
                DefaultValue = 0
            },
            new DeviceParameter
            {
                ParentName = Name, Name = "mode", Index = BaseIndex + (Number - 1), SubIndex = subIndex++,
                GetValue = () => Mode, SetValue = val => Mode = (InputMode)val,
                ValueType = Mode.GetType(),
                DefaultValue = InputMode.Momentary
            }
        ];
    }
}