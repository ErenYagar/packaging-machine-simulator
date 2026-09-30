namespace WaferSaw
{
    public sealed class TroubleshootingManager
    {
        public int NextStep { get; private set; }
        public string Feedback { get; private set; } = "Waiting for an alarm.";
        public bool CalibrationPassed => NextStep >= 5;
        public void Reset() { NextStep = 0; Feedback = "Follow the numbered diagnostic steps."; }
        public bool Accept(int step)
        {
            if (step != NextStep || step < 0 || step > 5)
            { Feedback = "Diagnostic Step Incorrect"; return false; }
            NextStep++;
            Feedback = step == 5 ? "Verification run in progress..." : "Step completed. Continue with step " + (NextStep + 1) + ".";
            return true;
        }
        public string Label(int step, FaultScenario scenario)
        {
            switch (step)
            {
                case 0: return "Check Alarm History";
                case 1: return "View Sensor Trend";
                case 2: return "Inspect " + (scenario == null ? "Component" : scenario.Component);
                case 3: return scenario == null ? "Perform Maintenance" : scenario.Repair;
                case 4: return "Calibrate / Check Setup";
                default: return "Verification Run";
            }
        }
    }
}
