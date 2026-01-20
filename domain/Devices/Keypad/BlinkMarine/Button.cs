namespace domain.Devices.Keypad.BlinkMarine;

public class Button
{
    public int Number { get; }
    public string Name { get; set; }
    public bool State { get; set; }  // Pressed/released

    // RGB LED properties (read-only from keypad perspective)
    public byte RedValue { get; set; }
    public byte GreenValue { get; set; }
    public byte BlueValue { get; set; }
    public bool BlinkEnabled { get; set; }

    public Button(int number, string name)
    {
        Number = number;
        Name = name;
    }
}