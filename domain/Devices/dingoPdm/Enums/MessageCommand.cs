namespace domain.Devices.dingoPdm.Enums;

public enum MessageCommand
{
    Null = 0,
    ReadParam = 1,
    WriteParam = 2,
    ReadAllParams = 3,
    ResetToDefaults = 4,
    BurnSettings = 5,
    ParamResponse = 10,
    Version = 20,
    Sleep = 21,
    Bootloader = 22,
}