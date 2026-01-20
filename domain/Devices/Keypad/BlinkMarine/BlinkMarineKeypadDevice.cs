using domain.Devices.Keypad.Enums;

namespace domain.Devices.Keypad.BlinkMarine;

public class BlinkMarineKeypadDevice(string name, int baseId) : KeypadDevice(name, baseId)
{
    public override KeypadBrand Brand { get; set; } = KeypadBrand.BlinkMarine;
}