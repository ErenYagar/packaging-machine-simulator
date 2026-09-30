using System;

namespace WaferSaw
{
    [Serializable]
    public struct SensorData
    {
        public float Rpm, Vibration, Vacuum, Temperature, CoolingFlow, BladeWear, Alignment;
        public bool ReadyForProduction => Rpm >= 29000 && Vibration >= 0.8f && Vibration <= 1.5f
            && Vacuum >= -85 && Vacuum <= -75 && Temperature >= 35 && Temperature <= 45
            && CoolingFlow >= 1.5f && CoolingFlow <= 2 && Alignment >= 0 && Alignment <= 5;
    }
}
