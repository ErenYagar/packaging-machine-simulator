namespace WaferSaw
{
    public sealed class KPIManager
    {
        public double PlannedTime { get; private set; }
        public double Uptime { get; private set; }
        public double Downtime => PlannedTime - Uptime;
        public int TotalAlarms { get; private set; }
        public int WafersProcessed { get; private set; }
        public double Availability => PlannedTime > 0 ? Uptime / PlannedTime : 0;
        public double Oee => Availability * 0.95 * 0.99;
        public double Mttr => recoveries > 0 ? repairSeconds / recoveries : 0;
        public bool HasCompletedRepair => recoveries > 0;
        private bool started, incident;
        private double currentRepair, repairSeconds, waferSeconds;
        private int recoveries;

        public void Start() { started = true; }
        public void Tick(float dt, MachineState state)
        {
            if (!started) return;
            PlannedTime += dt;
            if (state == MachineState.Running || state == MachineState.Warning)
            {
                Uptime += dt;
                waferSeconds += dt;
                while (waferSeconds >= 10) { WafersProcessed++; waferSeconds -= 10; }
            }
            if (incident) currentRepair += dt;
        }
        public void Alarm()
        {
            TotalAlarms++;
            if (!incident) { incident = true; currentRepair = 0; }
        }
        public void Recover()
        {
            if (!incident) return;
            repairSeconds += currentRepair;
            recoveries++;
            incident = false;
        }
    }
}
