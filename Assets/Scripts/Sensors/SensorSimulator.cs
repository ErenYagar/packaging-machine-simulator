using UnityEngine;

namespace WaferSaw
{
    public sealed class SensorSimulator
    {
        public SensorData Data { get; private set; }
        private float clock;

        public SensorSimulator() { Reset(); }
        public void Reset()
        {
            clock = 0;
            Data = new SensorData { Vacuum = -80, Temperature = 39, BladeWear = 18, Alignment = 2 };
        }

        public void ReplaceBlade()
        {
            var data = Data;
            data.BladeWear = 0;
            Data = data;
        }

        public void StopMotion()
        {
            var data = Data;
            data.Rpm = 0;
            data.CoolingFlow = 0;
            Data = data;
        }

        public void Tick(float dt, bool energized, bool producing, FaultManager faults)
        {
            clock += dt;
            float n = Mathf.PerlinNoise(clock * 0.23f, 3.1f) * 2 - 1;
            float n2 = Mathf.PerlinNoise(7.2f, clock * 0.17f) * 2 - 1;
            float blend = 1 - Mathf.Exp(-dt * 2.8f);
            float severity = faults.Severity;
            var data = Data;
            float vibration = 1.15f + 0.22f * n;
            float vacuum = -80 + 2.5f * n2;
            float flow = 1.78f + 0.13f * n;
            float temperature = 39 + 2 * n2;
            if (faults.Active == FaultKind.SpindleVibration)
            {
                vibration += severity * 4.5f;
                if (!faults.Repaired) data.BladeWear = Mathf.Max(data.BladeWear, 18 + severity * 74);
            }
            if (faults.Active == FaultKind.VacuumLow) vacuum += severity * 46;
            if (faults.Active == FaultKind.CoolingFlowLow)
            {
                flow -= severity * 1.4f;
                temperature += severity * 28;
            }
            data.Rpm = energized ? Mathf.Lerp(data.Rpm, 30000 + n * 90, blend) : 0;
            data.Vibration = Mathf.Lerp(data.Vibration, energized ? vibration : 0.03f, blend);
            data.Vacuum = Mathf.Lerp(data.Vacuum, vacuum, blend);
            data.CoolingFlow = energized ? Mathf.Lerp(data.CoolingFlow, flow, blend) : 0;
            data.Temperature = Mathf.Lerp(data.Temperature, temperature, 1 - Mathf.Exp(-dt * 0.16f));
            data.Alignment = Mathf.Lerp(data.Alignment, 2 + n2 * 0.8f, blend);
            if (producing) data.BladeWear = Mathf.Min(100, data.BladeWear + dt * 0.018f);
            Data = data;
        }
    }
}
