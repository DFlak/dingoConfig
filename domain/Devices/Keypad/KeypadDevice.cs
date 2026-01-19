using System.Collections.Concurrent;
using System.Text.Json.Serialization;
using domain.Interfaces;
using domain.Models;
using Microsoft.Extensions.Logging;

namespace domain.Devices.Keypad;

public class KeypadDevice : IDevice 
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
    
    [JsonConstructor]
    public KeypadDevice(string name, int baseId)
    {
        Name = name;
        BaseId = baseId;
        Guid =  Guid.NewGuid();
    }
    
    public void SetLogger(ILogger<KeypadDevice> logger)
    {
        Logger = logger;
    }
    
    public void UpdateIsConnected()
    {
        TimeSpan timeSpan = DateTime.Now - LastRxTime;
        Connected = timeSpan.TotalMilliseconds < 500;
    }

    private void Clear()
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