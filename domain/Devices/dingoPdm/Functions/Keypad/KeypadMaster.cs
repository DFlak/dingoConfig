using System.Text.Json.Serialization;
using domain.Devices.dingoPdm.Enums;
using domain.Devices.Keypad.BlinkMarine.Enums;
using domain.Interfaces;
using domain.Models;

namespace domain.Devices.dingoPdm.Functions.Keypad;

public class KeypadMaster : IDeviceFunction
{
    
    [JsonIgnore] public const int BaseIndex = 0x3000;
    [JsonPropertyName("name")] public string Name {get; set; }
    [JsonPropertyName("number")] public int Number {get;}
    [JsonPropertyName("enabled")] public bool Enabled {get; set;}
    [JsonPropertyName("id")] public int Id {get; set; }
    [JsonPropertyName("timeoutEnabled")] public bool TimeoutEnabled {get; set;}
    [JsonPropertyName("timeout")] public int Timeout {get; set;}
    [JsonPropertyName("model")] public KeypadModel Model { get; set; }
    [JsonPropertyName("backlightBrightness")] public int BacklightBrightness {get; set;}
    [JsonPropertyName("dimBacklightBrightness")] public int DimBacklightBrightness {get; set;}
    [JsonPropertyName("backlightButtonColor")] public int BacklightColor {get; set;}
    [JsonPropertyName("dimmingVar")] public int DimmingVar {get; set;}
    [JsonPropertyName("buttonBrightness")] public int ButtonBrightness {get; set;}
    [JsonPropertyName("dimButtonBrightness")] public int DimButtonBrightness {get; set;}
    [JsonPropertyName("buttons")] public List<Button> Buttons { get; init; } = [];
    [JsonPropertyName("dials")] public List<Dial> Dials { get; init; } = [];
    
    [JsonIgnore] public int NumButtons { get; set; }
    [JsonIgnore] public int NumDials { get; set; }
    
    [JsonIgnore]public List<DeviceParameter> Params { get; set; } = null!;
    
    [JsonConstructor]
    public KeypadMaster(int number, string name, int maxButtons, int maxDials)
    {
        Number = number;
        Name = name;

        InitFunctions(maxButtons, maxDials);
        InitParams();
    }

    private void InitFunctions(int maxButtons, int maxDials)
    {
        for (var i = 0; i < maxButtons; i++)
            Buttons.Add(new Button(Number, i + 1, "button" + (i + 1)));
        
        for (var i = 0; i < maxDials; i++)
            Dials.Add(new Dial(Number, i + 1, "dial" + (i + 1)));
    }

    private void InitParams()
    {
        var allParams = new List<DeviceParameter>();
        var subIndex = 0;
        allParams.AddRange(
        [
            new DeviceParameter
            {
                ParentName = Name, Name = $"keypad[{Number}].enabled", Index = BaseIndex + (Number - 1), SubIndex = subIndex++,
                GetValue = () => Enabled, SetValue = val => Enabled = (bool)val,
                ValueType = Enabled.GetType(),
                DefaultValue = false
            },
            new DeviceParameter
            {
                ParentName = Name, Name = $"keypad[{Number}].id", Index = BaseIndex + (Number - 1), SubIndex = subIndex++,
                GetValue = () => Id, SetValue = val => Id = (int)val,
                ValueType = Id.GetType(),
                DefaultValue = 0
            },
            new DeviceParameter
            {
                ParentName = Name, Name = $"keypad[{Number}].timeoutEnabled", Index = BaseIndex + (Number - 1), SubIndex = subIndex++,
                GetValue = () => TimeoutEnabled, SetValue = val => TimeoutEnabled = (bool)val,
                ValueType = TimeoutEnabled.GetType(),
                DefaultValue = false
            },
            new DeviceParameter
            {
                ParentName = Name, Name = $"keypad[{Number}].timeout", Index = BaseIndex + (Number - 1), SubIndex = subIndex++,
                GetValue = () => Timeout, SetValue = val => Timeout = (int)val,
                ValueType = Timeout.GetType(),
                DefaultValue = 0
            },
            new DeviceParameter
            {
                ParentName = Name, Name = $"keypad[{Number}].model", Index = BaseIndex + (Number - 1), SubIndex = subIndex++,
                GetValue = () => Model, SetValue = val => Model = (KeypadModel)val,
                ValueType = Model.GetType(),
                DefaultValue = KeypadModel.Blink12Key
            },
            new DeviceParameter
            {
                ParentName = Name, Name = $"keypad[{Number}].backlightBrightness", Index = BaseIndex + (Number - 1),
                SubIndex = subIndex++,
                GetValue = () => BacklightBrightness, SetValue = val => BacklightBrightness = (int)val,
                ValueType = BacklightBrightness.GetType(),
                DefaultValue = 100
            },
            new DeviceParameter
            {
                ParentName = Name, Name = $"keypad[{Number}].dimBacklightBrightness", Index = BaseIndex + (Number - 1),
                SubIndex = subIndex++,
                GetValue = () => DimBacklightBrightness, SetValue = val => DimBacklightBrightness = (int)val,
                ValueType = DimBacklightBrightness.GetType(),
                DefaultValue = 50
            },
            new DeviceParameter
            {
                ParentName = Name, Name = $"keypad[{Number}].backlightColor", Index = BaseIndex + (Number - 1), SubIndex = subIndex++,
                GetValue = () => BacklightColor, SetValue = val => BacklightColor = (int)val,
                ValueType = BacklightColor.GetType(),
                DefaultValue = 0
            },
            new DeviceParameter
            {
                ParentName = Name, Name = $"keypad[{Number}].dimmingVar", Index = BaseIndex + (Number - 1), SubIndex = subIndex++,
                GetValue = () => DimmingVar, SetValue = val => DimmingVar = (int)val,
                ValueType = DimmingVar.GetType(),
                DefaultValue = 0
            },
            new DeviceParameter
            {
                ParentName = Name, Name = $"keypad[{Number}].buttonBrightness", Index = BaseIndex + (Number - 1), SubIndex = subIndex++,
                GetValue = () => ButtonBrightness, SetValue = val => ButtonBrightness = (int)val,
                ValueType = ButtonBrightness.GetType(),
                DefaultValue = 100
            },
            new DeviceParameter
            {
                ParentName = Name, Name = $"keypad[{Number}].dimButtonBrightness", Index = BaseIndex + (Number - 1),
                SubIndex = subIndex++,
                GetValue = () => DimButtonBrightness, SetValue = val => DimButtonBrightness = (int)val,
                ValueType = DimButtonBrightness.GetType(),
                DefaultValue = 50
            },
        ]);

        foreach (var button in Buttons) allParams.AddRange(button.Params);
        foreach (var dial in Dials) allParams.AddRange(dial.Params);
        Params = allParams;
    }

    public bool IsBlinkMarine()
    {
        return Model is KeypadModel.Blink2Key or 
                        KeypadModel.Blink4Key or 
                        KeypadModel.Blink5Key or 
                        KeypadModel.Blink6Key or 
                        KeypadModel.Blink8Key or 
                        KeypadModel.Blink10Key or 
                        KeypadModel.Blink12Key or 
                        KeypadModel.Blink15Key or 
                        KeypadModel.Blink13Key2Dial or 
                        KeypadModel.BlinkRacepad or 
                        KeypadModel.Blink1Key;
    }

    public bool IsGrayhill()
    {
        return Model is KeypadModel.Grayhill6Key or
                        KeypadModel.Grayhill8Key or
                        KeypadModel.Grayhill12Key or
                        KeypadModel.Grayhill15Key or
                        KeypadModel.Grayhill20Key;
    }
    
}