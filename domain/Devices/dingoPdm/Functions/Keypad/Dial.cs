using System.Text.Json.Serialization;
using domain.Enums;
using domain.Interfaces;
using domain.Models;

namespace domain.Devices.dingoPdm.Functions.Keypad;

public class Dial : IDeviceFunction
{
    [JsonIgnore] public const int BaseIndex = 0x3200;
    [JsonPropertyName("name")] public string Name {get; set; }
    [JsonPropertyName("number")] public int Number {get;}
    [JsonPropertyName("enabled")] public bool Enabled {get; set;}
    [JsonPropertyName("minLed")] public int MinLed { get; set; } = 1;
    [JsonPropertyName("maxLed")] public int MaxLed { get; set; } = 16;
    [JsonPropertyName("ledOffset")] public int LedOffset {get; set; }
    [JsonPropertyName("topPosition")] public int TopPosition { get; set; } = 8; //Default = 8
    
    [JsonIgnore] public List<DeviceParameter> Params { get; set; } = null!;
    
    [JsonConstructor]
    public Dial(int number, string name)
    {
        Number = number;
        Name = name;

        InitParams();
    }

    private void InitParams()
    {
        Params = new List<DeviceParameter>();
        var subIndex = 0;
        Params.AddRange(
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
                ParentName = Name, Name = "minLed", Index = BaseIndex + (Number - 1), SubIndex = subIndex++,
                GetValue = () => MinLed, SetValue = val => MinLed = (int)val,
                ValueType = MinLed.GetType(),
                DefaultValue = 1
            },
            new DeviceParameter
            {
                ParentName = Name, Name = "maxLed", Index = BaseIndex + (Number - 1), SubIndex = subIndex++,
                GetValue = () => MaxLed, SetValue = val => MaxLed = (int)val,
                ValueType = MaxLed.GetType(),
                DefaultValue = 16
            },
            new DeviceParameter
            {
                ParentName = Name, Name = "ledOffset", Index = BaseIndex + (Number - 1), SubIndex = subIndex++,
                GetValue = () => LedOffset, SetValue = val => LedOffset = (int)val,
                ValueType = LedOffset.GetType(),
                DefaultValue = 0
            },
            new DeviceParameter
            {
                ParentName = Name, Name = "topPosition", Index = BaseIndex + (Number - 1), SubIndex = subIndex++,
                GetValue = () => TopPosition, SetValue = val => TopPosition = (int)val,
                ValueType = TopPosition.GetType(),
                DefaultValue = 8
            },
        ]);
    }
}