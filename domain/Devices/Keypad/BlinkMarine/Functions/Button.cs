using System.Text.Json.Serialization;
using domain.Devices.Keypad.BlinkMarine.Enums;
using domain.Devices.Keypad.Enums;

namespace domain.Devices.Keypad.BlinkMarine;

public class Button(int number, string name)
{
    [JsonPropertyName("number")] public int Number { get; } = number;
    [JsonPropertyName("name")] public string Name { get; set; } = name;
    [JsonPropertyName("mode")] public ButtonMode Mode { get; set; } //For sim
    [JsonIgnore] public bool State { get; set; }
    
    [JsonIgnore] public ButtonColor Color { get; set; }
    [JsonIgnore] public byte RedValue { get; set; }
    [JsonIgnore] public byte GreenValue { get; set; }
    [JsonIgnore] public byte BlueValue { get; set; }
    [JsonIgnore] public bool Blink { get; set; }
}