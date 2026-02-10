namespace domain.Devices.dingoPdm.Enums;

public enum MessageCommand
{
    Null = 0,
    Read = 1,
    Write = 2,

    ReadAll = 10,
    ReadAllRsp = 11,
    ReadAllComplete = 12,

    WriteAll = 20,
    WriteAllVal = 21,
    WriteAllComplete = 22,

    BurnSettings = 30,
    Version = 31,
    Sleep = 32,
    Bootloader = 33
}