using System.Text.Json.Serialization;
using domain.Devices.Keypad.Enums;

namespace domain.Devices.Keypad.Grayhill.Functions;

public class Button(int number, string name)
{
    [JsonPropertyName("number")] public int Number { get; } = number;
    [JsonPropertyName("name")] public string Name { get; set; } = name;
    [JsonPropertyName("mode")] public ButtonMode Mode { get; set; }
    [JsonIgnore] public bool State { get; set; }
    [JsonIgnore] public bool[] Led { get; set; } = new bool[LedCount];
    [JsonIgnore] public static int LedCount { get; } = 3;
}