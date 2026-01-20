namespace domain.Devices.Keypad.BlinkMarine;

public class Dial
{
    public int Number { get; }
    public string Name { get; set; }
    public int Position { get; set; }  // Encoder count/position
    public int Delta { get; set; }     // Change since last read

    public Dial(int number, string name)
    {
        Number = number;
        Name = name;
    }
}