using System.Text.Json.Serialization;
using domain.Interfaces;
using domain.Models;

namespace domain.Devices.dingoPdm.Functions;

public class StarterDisable : IDeviceFunction
{
    [JsonIgnore] public const int BaseIndex = 0x1800;
    [JsonPropertyName("name")] public string Name {get; set;}
    [JsonIgnore] public int Number => 1;
    [JsonPropertyName("enabled")] public bool Enabled {get; set;}
    [JsonPropertyName("input")] public DeviceVariable Input {get; set;} = new();
    [JsonPropertyName("outputsDisabled")] public List<bool> OutputsDisabled {get; set;}

    [JsonIgnore] public List<DeviceParameter> Params { get; }

    [JsonConstructor]
    public StarterDisable(string name, int outputCount)
    {
        Name = name;
        OutputsDisabled = [..new bool[outputCount]];
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
                ParentName = Name, Name = "input", Index = BaseIndex + (Number - 1), SubIndex = subIndex++,
                GetValue = () => Input.VariableIndex, SetValue = val => Input.VariableIndex = (int)val,
                ValueType = Input.VariableIndex.GetType(),
                DefaultValue = 0
            }
        };

        for (var i = 0; i < OutputsDisabled.Count; i++)
        {
            var idx = i;
            parameters.Add(new DeviceParameter
            {
                Name = $"outputsDisabled[{i}]", Index = BaseIndex + (Number - 1), SubIndex = subIndex++,
                GetValue = () => OutputsDisabled[idx], SetValue = val => OutputsDisabled[idx] = (bool)val,
                ValueType = OutputsDisabled[i].GetType(),
                DefaultValue = false
            });
        }

        return parameters;
    }
}