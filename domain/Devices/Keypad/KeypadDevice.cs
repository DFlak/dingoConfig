using System.Collections.Concurrent;
using System.Text.Json.Serialization;
using domain.Devices.Keypad.Enums;
using domain.Interfaces;
using domain.Models;
using Microsoft.Extensions.Logging;

namespace domain.Devices.Keypad;

public abstract class KeypadDevice : IDevice 
{
    [JsonIgnore] protected ILogger<KeypadDevice> Logger = null!;
    
    [JsonIgnore] public Guid Guid { get; }
    [JsonIgnore] public string Type => "Keypad";
    [JsonPropertyName("name")] public string Name { get; set; }
    [JsonPropertyName("baseId")] public int BaseId { get; set; }
    [JsonPropertyName("cyclicGap")] public TimeSpan CyclicGap { get; } =  TimeSpan.FromSeconds(1);
    [JsonPropertyName("cyclicPause")] public TimeSpan CyclicPause { get; } = TimeSpan.FromMilliseconds(1);
    [JsonIgnore] private DateTime LastRxTime { get; set; }
    
    [JsonIgnore]
    public bool Connected
    {
        get;
        private set
        {
            if (field && !value)
            {
                Clear();
            }

            field = value;
        }
    }
    
    [JsonPropertyName("brand")] public virtual KeypadBrand Brand { get; set; } = KeypadBrand.None;
    [JsonPropertyName("numButtons")] public virtual int NumButtons {get; set;} = 0;
    [JsonPropertyName("numDials")] public virtual int NumDials { get; set; } = 0;
    [JsonPropertyName("numAnalogInputs")] public virtual int NumAnalogInputs { get; set; } = 0;

    // Component collections (polymorphic - different types for different brands)
    [JsonPropertyName("buttons")]
    public List<object> Buttons { get; init; } = [];

    [JsonPropertyName("dials")]
    public List<object> Dials { get; init; } = [];

    [JsonPropertyName("analogInputs")]
    public List<object> AnalogInputs { get; init; } = [];

    [JsonConstructor]
    public KeypadDevice(string name, int baseId)
    {
        Name = name;
        BaseId = baseId;
        Guid =  Guid.NewGuid();
        InitializeCollections();
    }

    protected abstract void InitializeCollections();
    
    public virtual void SetLogger(ILogger<KeypadDevice> logger)
    {
        Logger = logger;
    }
    
    public void UpdateIsConnected()
    {
        TimeSpan timeSpan = DateTime.Now - LastRxTime;
        Connected = timeSpan.TotalMilliseconds < 500;
    }

    protected virtual void Clear()
    {

    }

    public virtual void Read(int id, byte[] data, ref ConcurrentDictionary<(int BaseId, int Prefix, int Index), DeviceCanFrame> queue)
    {
        
    }

    public virtual bool InIdRange(int id)
    {
        return false;
    }

    public virtual IEnumerable<(int MessageId, DbcSignal Signal)> GetStatusSignals()
    {
        return Enumerable.Empty<(int MessageId, DbcSignal Signal)>();
    }
    
    public virtual List<DeviceCanFrame> GetCyclicMsgs()
    {
        return [];
    }
}