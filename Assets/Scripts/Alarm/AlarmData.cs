namespace WaferSaw
{
    public sealed class AlarmData
    {
        public FaultScenario Scenario { get; }
        public SensorData AtTrip { get; }
        public float Time { get; }
        public AlarmData(FaultScenario scenario, SensorData atTrip, float time)
        { Scenario = scenario; AtTrip = atTrip; Time = time; }
    }
}
