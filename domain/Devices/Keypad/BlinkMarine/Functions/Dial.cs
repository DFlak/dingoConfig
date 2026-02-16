using System.Text.Json.Serialization;
using domain.Devices.Keypad.BlinkMarine.Enums;
using domain.Devices.Keypad.Enums;

namespace domain.Devices.Keypad.BlinkMarine.Functions;

public class Dial(int number, string name)
{
    [JsonPropertyName("number")] public int Number { get; } = number;
    [JsonPropertyName("name")] public string Name { get; set; } = name;
    [JsonPropertyName("enabled")] public bool Enabled { get; set; }
    [JsonPropertyName("topPosition")] public int TopPosition { get; set; }
    [JsonPropertyName("minLed")] public int MinLed { get; set; }
    [JsonPropertyName("maxLed")] public int MaxLed { get; set; }
    [JsonPropertyName("ledOffset")] public int LedOffset { get; set; }
    [JsonIgnore] public int Position { get; set; }  // Ticks
    [JsonIgnore] public int Delta { get; set; }
    [JsonIgnore] public DialDirection Direction { get; set; }
    [JsonIgnore] public int Counter { get; set; }
    [JsonIgnore] public bool[] Leds { get; set; } = new bool[16];
    [JsonIgnore] public bool RingLed {get; set;}
    [JsonIgnore] public bool RingLedBlink {get; set;}
}