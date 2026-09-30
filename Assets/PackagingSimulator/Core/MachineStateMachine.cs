using System;
using System.Collections.Generic;

namespace PackagingSim
{
    public enum MachineState { Idle, Running, Alarm }

    [Serializable]
    public sealed class SensorLimits
    {
        public float minPressure = 0.45f;
        public float maxPressure = 0.70f;
        public float minTemperature = 150f;
        public float maxTemperature = 190f;

        public bool IsValid => IsFinite(minPressure) && IsFinite(maxPressure)
            && IsFinite(minTemperature) && IsFinite(maxTemperature)
            && minPressure < maxPressure && minTemperature < maxTemperature;

        public static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }

    // Plain C#: state transitions can be tested without a scene or Unity clock.
    public sealed class MachineStateMachine
    {
        public MachineState State { get; private set; } = MachineState.Idle;
        public float Pressure { get; private set; }
        public float Temperature { get; private set; }
        public bool HasSample { get; private set; }
        public string ActiveFault { get; private set; } = "等待感測器資料";
        public string LatchedFault { get; private set; } = "";
        public bool CanReset => State == MachineState.Alarm && HasSample && ActiveFault.Length == 0;
        public event Action<MachineState> StateChanged;

        private readonly SensorLimits limits;

        public MachineStateMachine(SensorLimits limits)
        {
            this.limits = limits ?? throw new ArgumentNullException(nameof(limits));
        }

        public void Sample(float pressure, float temperature)
        {
            Pressure = pressure;
            Temperature = temperature;
            HasSample = true;
            var faults = new List<string>();
            if (!limits.IsValid) faults.Add("閾值設定無效：下限必須小於上限，且皆為有限數值。");
            else
            {
                if (!SensorLimits.IsFinite(pressure)) faults.Add("壓力訊號無效：檢查感測器連線。");
                else if (pressure < limits.minPressure) faults.Add("壓力過低：檢查氣源、調壓閥與管路洩漏。");
                else if (pressure > limits.maxPressure) faults.Add("壓力過高：檢查調壓閥設定。");
                if (!SensorLimits.IsFinite(temperature)) faults.Add("溫度訊號無效：檢查感測器連線。");
                else if (temperature < limits.minTemperature) faults.Add("溫度過低：檢查加熱器與溫控設定。");
                else if (temperature > limits.maxTemperature) faults.Add("溫度過高：檢查冷卻與溫控設定。");
            }
            ActiveFault = string.Join("\n", faults);
            if (ActiveFault.Length == 0) return; // Alarm stays latched after readings recover.
            if (State != MachineState.Alarm) LatchedFault = ActiveFault;
            SetState(MachineState.Alarm);
        }

        public bool Start()
        {
            if (State != MachineState.Idle || !HasSample || ActiveFault.Length != 0) return false;
            SetState(MachineState.Running);
            return true;
        }

        public void Stop()
        {
            if (State == MachineState.Running) SetState(MachineState.Idle);
        }

        public bool ResetAlarm()
        {
            if (!CanReset) return false;
            LatchedFault = "";
            SetState(MachineState.Idle);
            return true;
        }

        private void SetState(MachineState next)
        {
            if (State == next) return;
            State = next;
            StateChanged?.Invoke(next);
        }
    }
}
