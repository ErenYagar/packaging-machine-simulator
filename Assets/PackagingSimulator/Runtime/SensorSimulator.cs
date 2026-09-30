using UnityEngine;

namespace PackagingSim
{
    public enum SimulatedFault { None, LowPressure, HighPressure, LowTemperature, HighTemperature }

    public sealed class SensorSimulator : MonoBehaviour
    {
        [Tooltip("教學設定；壓力單位 MPa，溫度單位 °C。")]
        public SensorLimits limits = new SensorLimits();
        public SimulatedFault Fault { get; private set; }
        public float Pressure { get; private set; } = 0.57f;
        public float Temperature { get; private set; } = 170f;
        private float elapsed;

        public void Initialize()
        {
            Pressure = (limits.minPressure + limits.maxPressure) * 0.5f;
            Temperature = (limits.minTemperature + limits.maxTemperature) * 0.5f;
        }

        public void Inject(SimulatedFault fault) => Fault = fault;
        public void Repair() => Fault = SimulatedFault.None;

        public void Tick(float deltaTime, bool running)
        {
            elapsed += deltaTime;
            float pressureSpan = limits.maxPressure - limits.minPressure;
            float temperatureSpan = limits.maxTemperature - limits.minTemperature;
            float pressureTarget = (limits.minPressure + limits.maxPressure) * 0.5f;
            float temperatureTarget = (limits.minTemperature + limits.maxTemperature) * 0.5f;
            pressureTarget += Mathf.Sin(elapsed * 1.7f) * pressureSpan * (running ? 0.04f : 0.015f);
            temperatureTarget += Mathf.Sin(elapsed * 0.65f) * temperatureSpan * 0.025f;
            if (Fault == SimulatedFault.LowPressure) pressureTarget = limits.minPressure - pressureSpan * 0.6f;
            if (Fault == SimulatedFault.HighPressure) pressureTarget = limits.maxPressure + pressureSpan * 0.6f;
            if (Fault == SimulatedFault.LowTemperature) temperatureTarget = limits.minTemperature - temperatureSpan * 0.6f;
            if (Fault == SimulatedFault.HighTemperature) temperatureTarget = limits.maxTemperature + temperatureSpan * 0.6f;
            // Exponential smoothing is independent of frame rate. Faults persist in Alarm.
            float blend = 1f - Mathf.Exp(-2f * deltaTime);
            Pressure = Mathf.Lerp(Pressure, pressureTarget, blend);
            Temperature = Mathf.Lerp(Temperature, temperatureTarget, blend);
        }
    }
}
