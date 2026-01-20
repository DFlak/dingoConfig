namespace domain.Devices.Keypad.BlinkMarine;

public class AnalogInput
{
    public int Number { get; }
    public string Name { get; set; }
    public double Voltage { get; set; }  // Millivolts

    public AnalogInput(int number, string name)
    {
        Number = number;
        Name = name;
    }
}