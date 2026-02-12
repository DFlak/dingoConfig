using System.Text.Json.Serialization;
using domain.Enums;
using domain.Interfaces;
using domain.Models;

namespace domain.Devices.dingoPdm.Functions;

public class CanOutput : IDeviceFunction
{
    [JsonIgnore] public const int BaseIndex = 0x2000;
    [JsonPropertyName("name")] public string Name {get; set; }
    [JsonPropertyName("number")] public int Number {get;}
    [JsonPropertyName("enabled")] public bool Enabled {get; set;}
    [JsonPropertyName("input")] public int Input {get; set;}
    [JsonPropertyName("ide")] public bool Ide {get; set;}
    [JsonPropertyName("sid")] public int Sid {get; set;}
    [JsonPropertyName("eid")] public int Eid {get; set;}
    [JsonPropertyName("startBit")] public int StartBit {get; set;}
    [JsonPropertyName("bitLength")] public int BitLength {get; set;}
    [JsonPropertyName("factor")] public double Factor {get; set;}
    [JsonPropertyName("offset")] public double Offset {get; set;}
    [JsonPropertyName("byteOrder")] public ByteOrder ByteOrder {get; set;}
    [JsonPropertyName("signed")] public bool Signed {get; set;}
    [JsonPropertyName("interval")] public int Interval {get; set;}
    
    [JsonPropertyName("id")]
    public int Id
    {
        get;
        set
        {
            field = value;
            Ide = (field > 2047);
            if (field > 2047)
                Eid = field;
            else
                Sid = field;
        }
    }

    [JsonIgnore] public List<DeviceParameter> Params { get; }

    [JsonConstructor]
    public CanOutput(int number, string name)
    {
        Number = number;
        Name = name;

        Params = InitParams();
    }

    private List<DeviceParameter> InitParams()
    {
        var subIndex = 0;
        return
        [
            new DeviceParameter
            {
                ParentName = Name, Name = "enabled", Index = BaseIndex + (Number - 1), SubIndex = subIndex++,
                GetValue = () => Enabled, SetValue = val => Enabled = (bool)val,
                ValueType = Enabled.GetType(),
                DefaultValue = false
            },
            new DeviceParameter
            {
                ParentName = Name, Name = "input", Index = BaseIndex + (Number - 1), SubIndex = subIndex++,
                GetValue = () => Input, SetValue = val => Input = (int)val,
                ValueType = Input.GetType(),
                DefaultValue = false
            },
            new DeviceParameter
            {
                ParentName = Name, Name = "ide", Index = BaseIndex + (Number - 1), SubIndex = subIndex++,
                GetValue = () => Ide, SetValue = val => Ide = (bool)val,
                ValueType = Ide.GetType(),
                DefaultValue = false
            },
            new DeviceParameter
            {
                ParentName = Name, Name = "sid", Index = BaseIndex + (Number - 1), SubIndex = subIndex++,
                GetValue = () => Sid, SetValue = val => Sid = (int)val,
                ValueType = Sid.GetType(),
                DefaultValue = false
            },
            new DeviceParameter
            {
                ParentName = Name, Name = "eid", Index = BaseIndex + (Number - 1), SubIndex = subIndex++,
                GetValue = () => Eid, SetValue = val => Eid = (int)val,
                ValueType = Eid.GetType(),
                DefaultValue = false
            },
            new DeviceParameter
            {
                ParentName = Name, Name = "startBit", Index = BaseIndex + (Number - 1), SubIndex = subIndex++,
                GetValue = () => StartBit, SetValue = val => StartBit = (int)val,
                ValueType = StartBit.GetType(),
                DefaultValue = false
            },
            new DeviceParameter
            {
                ParentName = Name, Name = "bitLength", Index = BaseIndex + (Number - 1), SubIndex = subIndex++,
                GetValue = () => BitLength, SetValue = val => BitLength = (int)val,
                ValueType = BitLength.GetType(),
                DefaultValue = false
            },
            new DeviceParameter
            {
                ParentName = Name, Name = "factor", Index = BaseIndex + (Number - 1), SubIndex = subIndex++,
                GetValue = () => Factor, SetValue = val => Factor = (double)val,
                ValueType = Factor.GetType(),
                DefaultValue = false
            },
            new DeviceParameter
            {
                ParentName = Name, Name = "offset", Index = BaseIndex + (Number - 1), SubIndex = subIndex++,
                GetValue = () => Offset, SetValue = val => Offset = (double)val,
                ValueType = Offset.GetType(),
                DefaultValue = false
            },
            new DeviceParameter
            {
                ParentName = Name, Name = "byteOrder", Index = BaseIndex + (Number - 1), SubIndex = subIndex++,
                GetValue = () => ByteOrder, SetValue = val => ByteOrder = (ByteOrder)val,
                ValueType = ByteOrder.GetType(),
                DefaultValue = false
            },
            new DeviceParameter
            {
                ParentName = Name, Name = "signed", Index = BaseIndex + (Number - 1), SubIndex = subIndex++,
                GetValue = () => Signed, SetValue = val => Signed = (bool)val,
                ValueType = Signed.GetType(),
                DefaultValue = false
            },
            new DeviceParameter
            {
                ParentName = Name, Name = "interval", Index = BaseIndex + (Number - 1), SubIndex = subIndex++,
                GetValue = () => Interval, SetValue = val => Interval = (int)val,
                ValueType = Interval.GetType(),
                DefaultValue = false
            },
        ];
    }
}