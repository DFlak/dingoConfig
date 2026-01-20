namespace domain.Devices.Keypad.Grayhill;

public class Button
{
    public int Number { get; }
    public string Name { get; set; }
    public bool State { get; set; }

    public Button(int number, string name)
    {
        Number = number;
        Name = name;
    }
}