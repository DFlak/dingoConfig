using domain.Devices.dingoPdm.Enums;
using domain.Models;

namespace domain.Common;

public abstract class ParamCodec
{
    public static CanFrame ToFrame(MessageCommand cmd, DeviceParameter param, int baseId)
    {
        var payload = new byte[8];
        
        //Command
        DbcSignalCodec.InsertSignal(payload, (double)cmd, 0, 8);
        //Index
        DbcSignalCodec.InsertSignal(payload, param.Index, 8, 16);
        
        //SubIndex
        DbcSignalCodec.InsertSignal(payload, param.SubIndex, 24, 8);
        
        //Value
        var val = param.GetValue();
        DbcSignalCodec.InsertSignal(payload, (double)val, 32, 32);
        
        return new CanFrame
        (
            Id : baseId,
            Len : 8,
            Payload : payload
        );
    }
}