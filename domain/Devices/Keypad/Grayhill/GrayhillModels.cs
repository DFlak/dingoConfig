namespace domain.Devices.Keypad.Grayhill;

public static class GrayhillModels
{
    public static GrayhillKeypadDevice Create(string name, int baseId, string model)
    {
        if (string.IsNullOrEmpty(model))
            throw new ArgumentException("Grayhill keypad requires a model specification (e.g., 'grayhillkeypad-8b')");

        var config = Lookup(model);
        return new GrayhillKeypadDevice(name, baseId, config.numButtons);
    }
    
    private static (int numButtons, int numDials, int numAnalogInputs) Lookup(string model)
    {
        //3Kxyy
        //x = button symbol (doesn't matter)
        //y = num buttons
        return model switch
        {
            "3kx06" => (6,0,0),
            "3kx08" => (8,0,0),
            "3kx12" => (12,0,0),
            "3kx15" => (15,0,0),
            "3kx20" => (20,0,0),
            _ => (0,0,0)
        };
    }
}