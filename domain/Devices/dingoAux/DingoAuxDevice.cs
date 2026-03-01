using System.Collections.Generic;
using System.Text.Json.Serialization;
using domain.Common;
using domain.Devices.dingoPdm;
using domain.Devices.dingoPdm.Enums;
using domain.Models;

namespace domain.Devices.dingoAux
{
    public class DingoAuxDevice : PdmDevice
    {
        [JsonConstructor]
        public DingoAuxDevice(string name, int baseId) : base(name, baseId)
        {
        }

        [JsonIgnore] protected override int NumOutputs => 15;
        [JsonIgnore] protected override int NumDigitalInputs => 4;
        [JsonIgnore] protected override int PdmType => 2; //0=dingoPDM, 1=dingoPDM-Max, 2=dingoAUX
        [JsonIgnore] public override string Type => "dingoAUX";

        protected override void InitializeStatusMessageSignals()
        {
            StatusMessageSignals = new Dictionary<int, List<(DbcSignal Signal, Action<double> SetValue)>>();

            // Message 0: System status
            StatusMessageSignals[0] = new List<(DbcSignal, Action<double>)>();
            for (var i = 0; i < NumDigitalInputs; i++)
            {
                var inputIndex = i;
                StatusMessageSignals[0].Add((
                    new DbcSignal { Name = $"Input{inputIndex}State", StartBit = i, Length = 1 },
                    val => Inputs[inputIndex].State = val != 0
                ));
            }
            var systemStatusSignals = new List<(DbcSignal, Action<double>)>
            {
                new (new DbcSignal { Name = "DeviceState", StartBit = 8, Length = 4 },
                    (double val) => DeviceState = (domain.Enums.DeviceState)val),
                new (new DbcSignal { Name = "PdmType", StartBit = 12, Length = 4 },
                    (double val) => PdmTypeOk = PdmType == (int)val),
                new (new DbcSignal { Name = "TotalCurrent", StartBit = 16, Length = 16, Factor = 0.1, Unit = "A" },
                    (double val) => TotalCurrent = val),
                new (new DbcSignal { Name = "BatteryVoltage", StartBit = 32, Length = 16, Factor = 0.1, Unit = "V" },
                    (double val) => BatteryVoltage = val),
                new (new DbcSignal { Name = "BoardTemp", StartBit = 48, Length = 16, Factor = 0.1, Unit = "°C" },
                    (double val) => BoardTempC = Math.Round(val, 1))
            };
            StatusMessageSignals[0].AddRange(systemStatusSignals);

            // Message 1: Output currents 0-3
            StatusMessageSignals[1] = new List<(DbcSignal, Action<double>)>();
            for (var i = 0; i < 4; i++)
            {
                var outputIndex = i;
                StatusMessageSignals[1].Add(new (
                    new DbcSignal { Name = $"Output{outputIndex}Current", StartBit = i * 16, Length = 16, Factor = 0.1, Unit = "A" },
                    (double val) => Outputs[outputIndex].Current = val
                ));
            }

            // Message 2: Output currents 4-7
            StatusMessageSignals[2] = new List<(DbcSignal, Action<double>)>();
            for (var i = 4; i < 8; i++)
            {
                var outputIndex = i;
                StatusMessageSignals[2].Add(new (
                    new DbcSignal { Name = $"Output{outputIndex}Current", StartBit = (i - 4) * 16, Length = 16, Factor = 0.1, Unit = "A" },
                    (double val) => Outputs[outputIndex].Current = val
                ));
            }
            
            // Message 16: Output currents 8-11
            StatusMessageSignals[16] = new List<(DbcSignal, Action<double>)>();
            for (var i = 8; i < 12; i++)
            {
                var outputIndex = i;
                StatusMessageSignals[16].Add(new (
                    new DbcSignal { Name = $"Output{outputIndex}Current", StartBit = (i - 8) * 16, Length = 16, Factor = 0.1, Unit = "A" },
                    (double val) => Outputs[outputIndex].Current = val
                ));
            }

            // Message 17: Output currents 12-14
            StatusMessageSignals[17] = new List<(DbcSignal, Action<double>)>();
            for (var i = 12; i < 15; i++)
            {
                var outputIndex = i;
                StatusMessageSignals[17].Add(new (
                    new DbcSignal { Name = $"Output{outputIndex}Current", StartBit = (i - 12) * 16, Length = 16, Factor = 0.1, Unit = "A" },
                    (double val) => Outputs[outputIndex].Current = val
                ));
            }

            // Message 3: Output states 0-7, wiper, flashers
            StatusMessageSignals[3] = new List<(DbcSignal, Action<double>)>();
            for (var i = 0; i < 8; i++)
            {
                var outputIndex = i;
                StatusMessageSignals[3].Add(new (
                    new DbcSignal { Name = $"Output{outputIndex}State", StartBit = i * 4, Length = 4 },
                    (double val) => Outputs[outputIndex].State = (OutState)val
                ));
            }
            var wiperAndFlasherSignals = new List<(DbcSignal, Action<double>)>
            {
                new (new DbcSignal { Name = "WiperSlowState", StartBit = 32, Length = 1 },
                    (double val) => Wipers.SlowState = val != 0),
                new (new DbcSignal { Name = "WiperFastState", StartBit = 33, Length = 1 },
                    (double val) => Wipers.FastState = val != 0),
                new (new DbcSignal { Name = "WiperSpeed", StartBit = 40, Length = 4 },
                    (double val) => Wipers.Speed = (WiperSpeed)val),
                new (new DbcSignal { Name = "WiperState", StartBit = 44, Length = 4 },
                    (double val) => Wipers.State = (WiperState)val)
            };
            StatusMessageSignals[3].AddRange(wiperAndFlasherSignals);
            for (var i = 0; i < NumFlashers; i++)
            {
                var flasherIndex = i;
                StatusMessageSignals[3].Add(new (
                    new DbcSignal { Name = $"Flasher{flasherIndex}", StartBit = 48 + i, Length = 1 },
                    (double val) => Flashers[flasherIndex].Value = val != 0 && Flashers[flasherIndex].Enabled
                ));
            }

            // Message 18: Output states 8-14
            StatusMessageSignals[18] = new List<(DbcSignal, Action<double>)>();
            for (var i = 8; i < 15; i++)
            {
                var outputIndex = i;
                StatusMessageSignals[18].Add(new (
                    new DbcSignal { Name = $"Output{outputIndex}State", StartBit = (i-8) * 4, Length = 4 },
                    (double val) => Outputs[outputIndex].State = (OutState)val
                ));
            }

            // Message 4: Output reset counts 0-7
            StatusMessageSignals[4] = new List<(DbcSignal, Action<double>)>();
            for (var i = 0; i < 8; i++)
            {
                var outputIndex = i;
                StatusMessageSignals[4].Add(new (
                    new DbcSignal { Name = $"Output{outputIndex}ResetCount", StartBit = i * 8, Length = 8 },
                    (double val) => Outputs[outputIndex].ResetCount = (int)val
                ));
            }

            // Message 19: Output reset counts 8-14
            StatusMessageSignals[19] = new List<(DbcSignal, Action<double>)>();
            for (var i = 8; i < 15; i++)
            {
                var outputIndex = i;
                StatusMessageSignals[19].Add(new (
                    new DbcSignal { Name = $"Output{outputIndex}ResetCount", StartBit = (i-8) * 8, Length = 8 },
                    (double val) => Outputs[outputIndex].ResetCount = (int)val
                ));
            }

            // Message 5: CAN inputs & virtual inputs
            StatusMessageSignals[5] = new List<(DbcSignal, Action<double>)>();
            for (var i = 0; i < NumCanInputs; i++)
            {
                var canInputIndex = i;
                StatusMessageSignals[5].Add(new (
                    new DbcSignal { Name = $"CanInput{canInputIndex}", StartBit = i, Length = 1 },
                    (double val) => CanInputs[canInputIndex].Output = val != 0
                ));
            }
            for (var i = 0; i < NumVirtualInputs; i++)
            {
                var virtualInputIndex = i;
                StatusMessageSignals[5].Add(new (
                    new DbcSignal { Name = $"VirtualInput{virtualInputIndex}", StartBit = 32 + i, Length = 1 },
                    (double val) => VirtualInputs[virtualInputIndex].Value = val != 0
                ));
            }

            // Message 6: Counters & conditions
            StatusMessageSignals[6] = new List<(DbcSignal, Action<double>)>();
            for (var i = 0; i < NumCounters; i++)
            {
                var counterIndex = i;
                StatusMessageSignals[6].Add(new (
                    new DbcSignal { Name = $"Counter{counterIndex}", StartBit = i * 8, Length = 8 },
                    (double val) => Counters[counterIndex].Value = (int)val
                ));
            }
            for (var i = 0; i < NumConditions; i++)
            {
                var conditionIndex = i;
                StatusMessageSignals[6].Add(new (
                    new DbcSignal { Name = $"Condition{conditionIndex}", StartBit = 32 + i, Length = 1 },
                    (double val) => Conditions[conditionIndex].Value = (int)val
                ));
            }

            // Messages 7-14: CAN input values (4 per message)
            for (var msg = 7; msg <= 14; msg++)
            {
                StatusMessageSignals[msg] = new List<(DbcSignal, Action<double>)>();
                for (var i = 0; i < 4; i++)
                {
                    var canInputIndex = (msg - 7) * 4 + i;
                    if (canInputIndex < NumCanInputs)
                    {
                        StatusMessageSignals[msg].Add(new (
                            new DbcSignal { Name = $"CanInput{canInputIndex}Value", StartBit = i * 16, Length = 16 },
                            (double val) => CanInputs[canInputIndex].Value = (ushort)val
                        ));
                    }
                }
            }

            // Message 15: Output duty cycles 0-7
            StatusMessageSignals[15] = new List<(DbcSignal, Action<double>)>();
            for (var i = 0; i < 8; i++)
            {
                var outputIndex = i;
                StatusMessageSignals[15].Add(new (
                    new DbcSignal { Name = $"Output{outputIndex}DutyCycle", StartBit = i * 8, Length = 8, Unit = "%" },
                    (double val) => Outputs[outputIndex].CurrentDutyCycle = val
                ));
            }

            // Message 20: Output duty cycles 8-14
            StatusMessageSignals[20] = new List<(DbcSignal, Action<double>)>();
            for (var i = 8; i < 15; i++)
            {
                var outputIndex = i;
                StatusMessageSignals[20].Add(new (
                    new DbcSignal { Name = $"Output{outputIndex}DutyCycle", StartBit = (i-8) * 8, Length = 8, Unit = "%" },
                    (double val) => Outputs[outputIndex].CurrentDutyCycle = val
                ));
            }
        }
    }
}
