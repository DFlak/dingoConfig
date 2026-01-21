namespace domain.Devices.Keypad.BlinkMarine;

public static class BlinkMarineModels
{
    public static BlinkMarineKeypadDevice Create(string name, int baseId, string model)
    {
        if (string.IsNullOrEmpty(model))
            throw new ArgumentException("BlinkMarine keypad requires a model specification (e.g., 'blinkkeypad-pkp2200si')");

        var config = Lookup(model);
        return new BlinkMarineKeypadDevice(name, baseId, config.numButtons, config.numDials, config.numAnalogInputs);
    }
    
    private static (int numButtons, int numDials, int numAnalogInputs) Lookup(string model)
    {
        return model switch
        {
            "pkp1600li" => (6,0,0),
            "pkp2200si" => (4, 0, 0),
            "pkp2300si" => (6, 0, 0),
            "pkp2400si" => (8, 0, 0),
            "pkp2500si" => (10, 0, 0),
            "pkp2600si" => (12, 0, 0),
            "pkp3500si" => (15, 0, 0),
            "pkp1100li" => (1, 0, 0),
            "pkp1200li" => (2, 0, 0),
            "pkp1500li" => (5, 0, 0),
            "pkp2200li" => (4, 0, 0),
            "pkp2400li" => (8, 0, 0),
            "pkp2300sifr" => (6, 0, 0),
            "pkp3500simt" => (13, 2, 4),
            "pkp2600sifr" => (12, 0, 0),
            "racepad" => (8, 4, 4),
            _ => (0, 0, 0)
        };
    }
    
    
}