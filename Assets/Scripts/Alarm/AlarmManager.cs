namespace WaferSaw
{
    public sealed class AlarmManager
    {
        public AlarmData Current { get; private set; }
        public bool Latched => Current != null;
        public void Latch(FaultKind kind, SensorData data, float time) { Current = new AlarmData(FaultScenario.For(kind), data, time); }
        public void ClearAfterVerification() { Current = null; }
        public static bool Warning(FaultKind kind, SensorData data)
        {
            return kind == FaultKind.SpindleVibration && data.Vibration >= 2.5f
                || kind == FaultKind.VacuumLow && data.Vacuum >= -65
                || kind == FaultKind.CoolingFlowLow && data.CoolingFlow <= 1.2f;
        }
        public static bool Trip(FaultKind kind, SensorData data)
        {
            return kind == FaultKind.SpindleVibration && data.Vibration >= 4.5f
                || kind == FaultKind.VacuumLow && data.Vacuum >= -50
                || kind == FaultKind.CoolingFlowLow && data.CoolingFlow <= 0.8f;
        }
    }
}
