using System.Collections.Concurrent;
using domain.Common;
using domain.Devices.dingoPdm.Enums;
using domain.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace domain.Devices.dingoPdm;

internal class ParamProtocol
{
    private readonly List<DeviceParameter> _params;
    private readonly int _minMajor;
    private readonly int _minMinor;
    private readonly int _minBuild;
    private ILogger _logger = NullLogger.Instance;

    private readonly Dictionary<(int Index, int SubIndex), object> _tempParamValues = new();
    private int _readAllCount;
    private int _readAllAttempts;
    private int _writeAllCount;
    private int _writeAllAttempts;

    public event Action<string>? VersionReceived;

    public ParamProtocol(List<DeviceParameter> @params, int minMajor, int minMinor, int minBuild)
    {
        _params = @params;
        _minMajor = minMajor;
        _minMinor = minMinor;
        _minBuild = minBuild;
    }

    public void SetLogger(ILogger logger) => _logger = logger;

    public void HandleMessage(
        int baseId,
        string name,
        byte[] data,
        ConcurrentDictionary<(int BaseId, int Index, int SubIndex), DeviceCanFrame> queue,
        List<DeviceCanFrame> outgoing)
    {
        DeviceCanFrame canFrame;
        int index, subIndex;
        DeviceParameter? matchingParam;
        double rawValue;
        object convertedValue;
        (int BaseId, int, int) key;

        switch ((MessageCommand)data[0])
        {
            //Error message commands
            case MessageCommand.ReadParamNotFound:
            case MessageCommand.WriteAllParamNotFound:
            case MessageCommand.WriteAllOutOfRange:
                if (data.Length != 8) return;

                index = data[2] << 8 | data[1];
                subIndex = data[3];

                matchingParam = _params.FirstOrDefault(p => p.Index == index && p.SubIndex == subIndex);

                var paramName = "";
                if (matchingParam != null)
                {
                    paramName = matchingParam.Name;
                }

                key = (baseId, index, subIndex);
                if (queue.TryGetValue(key, out canFrame!))
                {
                    canFrame.TimeSentTimer?.Dispose();
                    queue.TryRemove(key, out _);
                }

                var errorType = (MessageCommand)data[0] switch
                {
                    MessageCommand.ReadParamNotFound => "Read Param Not Found",
                    MessageCommand.WriteAllParamNotFound => "Write Param Not Found",
                    MessageCommand.WriteAllOutOfRange => "Write Param Out of Range",
                    _ => "Invalid error type"
                };

                _logger.LogError("{Name} ID: {BaseId}, {ErrorType} - {paramName} - {index}:{subindex}",
                    name, baseId, errorType, paramName, index, subIndex);

                break;

            case MessageCommand.Read:
            case MessageCommand.Write:
            case MessageCommand.WriteAllVal:
                if (data.Length != 8) return;

                index = data[2] << 8 | data[1];
                subIndex = data[3];

                matchingParam = _params.FirstOrDefault(p => p.Index == index && p.SubIndex == subIndex);
                if (matchingParam is null) break;

                if (matchingParam.ValueType == typeof(double))
                {
                    convertedValue = (double)DbcSignalCodec.ExtractSignal(data, startBit: 32, length: 32, isFloat: true);
                }
                else
                {
                    rawValue = DbcSignalCodec.ExtractSignal(data, startBit: 32, length: 32, isSigned: matchingParam.IsSignedInt);

                    // Convert to the appropriate type based on param.ValueType
                    convertedValue = matchingParam.ValueType switch
                    {
                        { } t when t == typeof(bool) => rawValue != 0,
                        { } t when t == typeof(int) => (int)rawValue,
                        { IsEnum: true } t => Enum.ToObject(t, (int)rawValue),
                        _ => rawValue
                    };
                }

                matchingParam.SetValue(convertedValue);

                key = (baseId, index, subIndex);
                if (queue.TryGetValue(key, out canFrame!))
                {
                    canFrame.TimeSentTimer?.Dispose();
                    queue.TryRemove(key, out _);
                }

                break;

            case MessageCommand.ReadAll:
                if (data.Length != 8) return;

                index = data[2] << 8 | data[1];
                subIndex = data[3];

                _tempParamValues.Clear();
                foreach (var param in _params)
                    _tempParamValues[(param.Index, param.SubIndex)] = param.DefaultValue;

                _readAllCount = 0;
                _readAllAttempts = 0;

                key = (baseId, index, subIndex);
                if (queue.TryGetValue(key, out canFrame!))
                {
                    canFrame.TimeSentTimer?.Dispose();
                    queue.TryRemove(key, out _);
                }

                _logger.LogInformation("{Name} ID: {BaseId}, Read All Started", name, baseId);

                break;

            case MessageCommand.ReadAllRsp:
                if (data.Length != 8) return;

                index = data[2] << 8 | data[1];
                subIndex = data[3];

                matchingParam = _params.FirstOrDefault(p => p.Index == index && p.SubIndex == subIndex);
                if (matchingParam is null)
                {
                    _logger.LogWarning("{Name} ID: {BaseId}, Cannot find param {index}:{subIndex}", name, baseId, index, subIndex);
                    break;
                }

                if (matchingParam.ValueType == typeof(double))
                {
                    convertedValue = (double)DbcSignalCodec.ExtractSignal(data, startBit: 32, length: 32, isFloat: true);
                }
                else
                {
                    rawValue = DbcSignalCodec.ExtractSignal(data, startBit: 32, length: 32, isSigned: matchingParam.IsSignedInt);

                    // Convert to the appropriate type based on param.ValueType
                    convertedValue = matchingParam.ValueType switch
                    {
                        { } t when t == typeof(bool) => rawValue != 0,
                        { } t when t == typeof(int) => (int)rawValue,
                        { IsEnum: true } t => Enum.ToObject(t, (int)rawValue),
                        _ => rawValue
                    };
                }

                _tempParamValues[(index, subIndex)] = convertedValue;

                _readAllCount++;

                break;

            case MessageCommand.ReadAllComplete:
                if (data.Length != 8) return;

                var readAllCount = data[2] << 8 | data[1];

                if (readAllCount == _readAllCount)
                {
                    // End of params, apply all temporary values to actual properties
                    foreach (var param in _params)
                    {
                        var paramKey = (param.Index, param.SubIndex);
                        if (_tempParamValues.TryGetValue(paramKey, out var value))
                        {
                            param.SetValue(value);
                        }
                    }

                    _tempParamValues.Clear();
                    _logger.LogInformation("{Name} ID: {BaseId}, Read All Complete {fromPdm}", name, baseId, readAllCount);
                }
                else
                {
                    _tempParamValues.Clear();
                    _logger.LogWarning("{Name} ID: {BaseId}, Read All Incomplete {fromPdm} vs {received}",
                                        name, baseId, readAllCount, _readAllCount);

                    /*
                    if (_readAllAttempts >= 5) break;

                    _readAllAttempts++;

                    outgoing.Add(new DeviceCanFrame
                    {
                        DeviceBaseId = baseId,
                        Frame = new CanFrame(
                            Id: baseId - 1,
                            Len: 8,
                            Payload: [Convert.ToByte(MessageCommand.ReadAll), 0, 0, 0, 0, 0, 0, 0])
                    });
                    */
                }

                break;

            case MessageCommand.WriteAll:
                if (data.Length != 8) return;

                index = data[2] << 8 | data[1];
                subIndex = data[3];

                key = (baseId, index, subIndex);
                if (queue.TryGetValue(key, out canFrame!))
                {
                    canFrame.TimeSentTimer?.Dispose();
                    queue.TryRemove(key, out _);
                }

                //Write all modified values
                outgoing.AddRange(BuildWriteAllMsgs(baseId));

                _logger.LogInformation("{Name} ID: {BaseId}, Write All Started {Count}", name, baseId, _writeAllCount);

                break;

            case MessageCommand.WriteAllComplete:
                if (data.Length != 8) return;

                var writeAllCount = data[2] << 8 | data[1];

                if (writeAllCount == _writeAllCount)
                {
                    _logger.LogInformation("{Name} ID: {BaseId}, Write All Completed {fromPdm}", name, baseId, writeAllCount);
                }
                else
                {
                    //if (_writeAllAttempts > 5)
                    //{
                        _logger.LogError("{Name} ID: {BaseId}, Write All Failed {fromPdm} vs {received}",
                            name, baseId, writeAllCount, _writeAllCount);
                     //   break;
                    //}
                    /*
                    _writeAllAttempts++;

                    _logger.LogWarning("{Name} ID: {BaseId}, Write All Incomplete {fromPdm} vs {received}",
                        name, baseId, writeAllCount, _writeAllCount);

                    outgoing.Add(new DeviceCanFrame
                    {
                        DeviceBaseId = baseId,
                        Frame = new CanFrame(
                            Id: baseId - 1,
                            Len: 8,
                            Payload: [Convert.ToByte(MessageCommand.WriteAll), 0, 0, 0, 0, 0, 0, 0])
                    });
                    */
                }
                break;

            case MessageCommand.Version:
                if (data.Length != 8) return;

                var version = $"v{data[1]}.{data[2]}.{(data[3] << 8) + (data[4])}";
                VersionReceived?.Invoke(version);

                key = (baseId, (int)MessageCommand.Version, 0);
                if (queue.TryGetValue(key, out canFrame!))
                {
                    canFrame.TimeSentTimer?.Dispose();
                    queue.TryRemove(key, out _);
                }

                _logger.LogInformation("{Name} FW version received: {Version}", name, version);

                if (!CheckVersion(data[1], data[2], (data[3] << 8) + (data[4])))
                {
                    _logger.LogError("{Name} ID: {BaseId}, Firmware needs to be updated. V{MinMajorVersion}.{MinMinorVersion}.{MinBuildVersion} or greater",
                                        name, baseId, _minMajor, _minMinor, _minBuild);
                }

                break;

		    case MessageCommand.BurnParams:
                if (data.Length != 8) return;

                if (data[1] == 1) //Successful burn
                {
                    _logger.LogInformation("{Name} ID: {BaseId}, Burn Successful", name, baseId);

                    key = (baseId, (int)MessageCommand.BurnParams, 0);
                    if (queue.TryGetValue(key, out canFrame!))
                    {
                        canFrame.TimeSentTimer?.Dispose();
                        queue.TryRemove(key, out _);
                    }
                }

                if (data[1] == 0) //Unsuccessful burn
                    _logger.LogError("{Name} ID: {BaseId}, Burn Failed", name, baseId);

                break;

            case MessageCommand.Sleep:
                if (data.Length != 8) return;

                if (data[1] == 1) //Successful sleep
                {
                    _logger.LogInformation("{Name} ID: {BaseId}, Sleep Successful", name, baseId);

                    key = (baseId, (int)MessageCommand.Sleep, 0);
                    if (queue.TryGetValue(key, out canFrame!))
                    {
                        canFrame.TimeSentTimer?.Dispose();
                        queue.TryRemove(key, out _);
                    }
                }

                if (data[1] == 0) //Unsuccessful sleep
                    _logger.LogError("{Name} ID: {BaseId}, Sleep Failed", name, baseId);

                break;
        }
    }

    private List<DeviceCanFrame> BuildWriteAllMsgs(int baseId)
    {
        var modifiedParams = _params.Where(p => p.IsModified).ToList();
        List<DeviceCanFrame> msgs = [];
        _writeAllCount = modifiedParams.Count;

        foreach (var parameter in modifiedParams)
        {
            msgs.Add(new DeviceCanFrame
            {
                DeviceBaseId = baseId,
                SendOnly = false,
                Frame = ParamCodec.ToFrame(MessageCommand.WriteAllVal, parameter, baseId - 1),
                Name = parameter.Name
            });
        }

        //Write all complete, with num params
        msgs.Add(new DeviceCanFrame
        {
            DeviceBaseId = baseId,
            SendOnly = true,
            Frame = new CanFrame(
                Id: baseId - 1,
                Len: 8,
                Payload: [  Convert.ToByte(MessageCommand.WriteAllComplete),
                    Convert.ToByte(_writeAllCount & 0xFF),
                    Convert.ToByte((_writeAllCount >> 8) & 0xFF),
                    0, 0, 0, 0, 0]),
            Name = "WriteAllComplete"
        });

        return msgs;
    }

    private bool CheckVersion(int major, int minor, int build)
    {
        if (major > _minMajor)
            return true;

        if ((major == _minMajor) && (minor > _minMinor))
            return true;

        if ((major == _minMajor) && (minor == _minMinor) && (build >= _minBuild))
            return true;

        return false;
    }
}
