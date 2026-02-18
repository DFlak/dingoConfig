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
    [JsonPropertyName("minCount")] public int MinCount { get; set; }
    [JsonPropertyName("maxCount")] public int MaxCount { get; set; } = 16;
    [JsonPropertyName("ledOffset")] public int LedOffset {get; set; }
    [JsonIgnore] public int TopPosition { get; set; } = 8; //Default = 8
    
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
                DefaultValue = 16
            },
            new DeviceParameter
            {
                ParentName = Name, Name = "ledOffset", Index = BaseIndex + (Number - 1), SubIndex = subIndex++,
                GetValue = () => LedOffset, SetValue = val => LedOffset = (int)val,
                ValueType = LedOffset.GetType(),
                DefaultValue = 0
            }
        ]);
    }
}