namespace WaferSaw
{
    public enum FaultKind { None, SpindleVibration, VacuumLow, CoolingFlowLow }

    public sealed class FaultScenario
    {
        public readonly FaultKind Kind;
        public readonly string Code, Description, NormalRange, PossibleCauses, Component, Repair;
        private FaultScenario(FaultKind kind, string code, string description, string range, string causes, string component, string repair)
        { Kind = kind; Code = code; Description = description; NormalRange = range; PossibleCauses = causes; Component = component; Repair = repair; }

        public static FaultScenario For(FaultKind kind)
        {
            switch (kind)
            {
                case FaultKind.SpindleVibration: return new FaultScenario(kind, "E203", "SPINDLE VIBRATION HIGH", "0.8 - 1.5 mm/s", "Blade wear / blade imbalance", "Blade", "Replace Blade");
                case FaultKind.VacuumLow: return new FaultScenario(kind, "E117", "WAFER CHUCK VACUUM LOW", "-85 to -75 kPa", "Vacuum leakage / chuck contamination / supply abnormal", "Chuck", "Clean Chuck");
                case FaultKind.CoolingFlowLow: return new FaultScenario(kind, "E305", "COOLING WATER FLOW LOW", "1.5 - 2.0 L/min", "Filter restriction / line restriction / flow sensor abnormal", "Cooling Line", "Clean Filter");
                default: return null;
            }
        }
        public string Value(SensorData data)
        {
            switch (Kind)
            {
                case FaultKind.SpindleVibration: return data.Vibration.ToString("F2") + " mm/s";
                case FaultKind.VacuumLow: return data.Vacuum.ToString("F1") + " kPa";
                default: return data.CoolingFlow.ToString("F2") + " L/min";
            }
        }
    }
}
