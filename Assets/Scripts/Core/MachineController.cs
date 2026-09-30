using System;
using System.Collections.Generic;
using UnityEngine;

namespace WaferSaw
{
    [DefaultExecutionOrder(-100)]
    public sealed class MachineController : MonoBehaviour
    {
        public MachineState State { get; private set; }
        public SensorSimulator Sensors { get; private set; }
        public FaultManager Faults { get; private set; }
        public AlarmManager Alarms { get; private set; }
        public TroubleshootingManager Troubleshooting { get; private set; }
        public PMManager PM { get; private set; }
        public EventLogger Log { get; private set; }
        public KPIManager Kpi { get; private set; }
        public float Elapsed { get; private set; }
        public bool AutomaticUpdates = true;
        public bool DemoActive { get; private set; }
        public string Notice { get; private set; }
        public bool MotionEnabled => State == MachineState.Running || State == MachineState.Warning || State == MachineState.Verifying;
        public bool SafeForService => State == MachineState.Idle || State == MachineState.Alarm || State == MachineState.Maintenance;
        public float VerificationProgress => Mathf.Clamp01(verifyTime / 8);
        public int SamplesVersion { get; private set; }
        public bool TrendsFrozen { get; private set; }
        public IReadOnlyList<SensorData> TrendSamples => TrendsFrozen ? tripSamples : samples;
        private readonly List<SensorData> samples = new List<SensorData>();
        private readonly List<SensorData> tripSamples = new List<SensorData>();
        private float sampleTime, verifyTime, stableTime, runningTime;
        private int demoStep;

        private void Awake() { ResetDemo(); }
        private void Update() { if (AutomaticUpdates) Advance(Time.deltaTime); }

        public void ResetDemo()
        {
            State = MachineState.Idle;
            Sensors = new SensorSimulator(); Faults = new FaultManager(); Alarms = new AlarmManager();
            Troubleshooting = new TroubleshootingManager(); PM = new PMManager(); Log = new EventLogger(); Kpi = new KPIManager();
            Elapsed = sampleTime = verifyTime = stableTime = runningTime = 0;
            DemoActive = TrendsFrozen = false; demoStep = 0;
            samples.Clear(); tripSamples.Clear(); SamplesVersion++;
            Notice = "Ready. Start the machine, then inject a fault.";
            Write("NEW SIMULATION SESSION - counters reset (not an alarm reset)");
            Write("STATE -> IDLE");
        }

        public void Advance(float dt)
        {
            if (dt <= 0 || float.IsNaN(dt) || float.IsInfinity(dt)) return;
            Elapsed += dt;
            Kpi.Tick(dt, State);
            Faults.Tick(dt);
            Sensors.Tick(dt, MotionEnabled, State == MachineState.Running || State == MachineState.Warning, Faults);
            if (State == MachineState.Running || State == MachineState.Warning)
            {
                runningTime += dt;
                if (Faults.Active != FaultKind.None && runningTime >= 2)
                {
                    // Latch at the trip threshold; returning sensor values alone never clears it.
                    if (AlarmManager.Trip(Faults.Active, Sensors.Data)) RaiseAlarm("Threshold exceeded");
                    else if (State == MachineState.Running && AlarmManager.Warning(Faults.Active, Sensors.Data))
                    {
                        SetState(MachineState.Warning);
                        Write("WARNING: " + FaultScenario.For(Faults.Active).Description, LogLevel.Warning);
                    }
                }
            }
            else if (State == MachineState.Verifying)
            {
                verifyTime += dt;
                stableTime = Sensors.Data.ReadyForProduction && Faults.Repaired ? stableTime + dt : 0;
                if (verifyTime >= 8)
                {
                    if (stableTime >= 2.5f && Troubleshooting.CalibrationPassed)
                    {
                        Write("TEST WAFER PASS - vibration / vacuum / flow / temperature / alignment", LogLevel.Recovery);
                        Alarms.ClearAfterVerification(); Faults.Reset(); Kpi.Recover();
                        SetState(MachineState.Running);
                        Notice = "Verification PASS. Production released.";
                        Write("MACHINE RECOVERED - alarm cleared", LogLevel.Recovery);
                    }
                    else RaiseAlarm("Verification FAILED - repeat diagnosis; alarm remains latched");
                }
            }
            sampleTime += dt;
            if (sampleTime >= 0.2f)
            {
                sampleTime %= 0.2f;
                samples.Add(Sensors.Data);
                if (samples.Count > 120) samples.RemoveAt(0);
                SamplesVersion++;
            }
            if (DemoActive) AdvanceDemo();
        }

        public bool StartMachine()
        {
            if (State != MachineState.Idle || Alarms.Latched || Faults.Active != FaultKind.None) return Reject("Start blocked: unresolved alarm or machine not idle.");
            Kpi.Start(); runningTime = 0;
            Write("MACHINE START"); SetState(MachineState.Running); Notice = "Production active. Sensors sampled every 0.2 s.";
            return true;
        }

        public void StopMachine()
        {
            DemoActive = false;
            if (State == MachineState.Verifying || (Faults.Active != FaultKind.None && (State == MachineState.Running || State == MachineState.Warning)))
                RaiseAlarm("Operator stopped an unresolved fault / verification");
            else if (State == MachineState.Running) { SetState(MachineState.Idle); Write("OPERATOR STOP"); }
            else Write("STOP - machine already in safe stopped state");
        }

        public bool InjectFault(FaultKind kind)
        {
            if (State != MachineState.Running || Faults.Active != FaultKind.None || kind == FaultKind.None)
                return Reject("Fault injection requires normal production with no active fault.");
            Faults.Inject(kind); Write("FAULT INJECTED: " + FaultScenario.For(kind).Description, LogLevel.Warning);
            Notice = "Observe the sensor trend as the fault gradually develops.";
            return true;
        }

        public bool DiagnosticStep(int step)
        {
            if (!Alarms.Latched || (State != MachineState.Alarm && State != MachineState.Maintenance)) return Reject("Diagnostic Step Incorrect");
            if (!Troubleshooting.Accept(step)) return Reject("Diagnostic Step Incorrect");
            FaultScenario scenario = Alarms.Current.Scenario;
            if (step != 3) Write(Troubleshooting.Label(step, scenario) + (step == 5 ? " requested" : " completed"), LogLevel.Maintenance);
            if (step == 0) Write("ENGINEER DIAGNOSIS START", LogLevel.Maintenance);
            if (step == 2) Write("Inspection supports " + scenario.Repair + "; verify repair with a test wafer", LogLevel.Maintenance);
            if (step == 3)
            {
                SetState(MachineState.Maintenance);
                Write("SIMULATED ISOLATION CONFIRMED - zero speed / coolant off", LogLevel.Maintenance);
                Faults.Repair();
                if (scenario.Kind == FaultKind.SpindleVibration) Sensors.ReplaceBlade();
                Write(scenario.Repair + " performed", LogLevel.Maintenance);
            }
            if (step == 4) Write("Calibration PASS - guards closed before test run", LogLevel.Maintenance);
            if (step == 5)
            {
                verifyTime = stableTime = 0; TrendsFrozen = false;
                SetState(MachineState.Verifying); Write("Verification Run - test wafer, 8 s; all channels must stabilize");
            }
            Notice = Troubleshooting.Feedback;
            return true;
        }

        public bool SetPM(int index, bool value)
        {
            if (index < 0 || index >= PMManager.Items.Length || !SafeForService) return Reject("PM requires a stopped machine.");
            bool completed = PM.Completed;
            PM.Set(index, value);
            if (!completed && PM.Completed) Write("PM COMPLETED - all six checklist items", LogLevel.Maintenance);
            return true;
        }

        public void StartDemo()
        {
            ResetDemo(); DemoActive = true; StartMachine();
            Write("DEMO MODE - guided automatic sequence; STOP cancels automation");
        }

        private void AdvanceDemo()
        {
            float[] times = { 12, 38, 44, 50, 58, 68, 74, 94 };
            if (demoStep >= times.Length || Elapsed < times[demoStep]) return;
            bool success;
            if (demoStep == 0) success = InjectFault(FaultKind.SpindleVibration);
            else if (demoStep < 7) success = DiagnosticStep(demoStep - 1);
            else { DemoActive = false; Write("DEMO COMPLETE - recovery and KPI review", LogLevel.Recovery); return; }
            if (success) demoStep++;
            else { DemoActive = false; Write("DEMO PAUSED - inspect current state", LogLevel.Warning); }
        }

        private void RaiseAlarm(string reason)
        {
            if (State == MachineState.Alarm) return;
            Alarms.Latch(Faults.Active, Sensors.Data, Elapsed);
            tripSamples.Clear(); tripSamples.AddRange(samples); tripSamples.Add(Sensors.Data);
            if (tripSamples.Count > 120) tripSamples.RemoveAt(0);
            TrendsFrozen = true; SamplesVersion++; Troubleshooting.Reset(); Kpi.Alarm();
            SetState(MachineState.Alarm);
            Write("ALARM " + Alarms.Current.Scenario.Code + " - " + reason, LogLevel.Alarm);
            Write("MACHINE STOPPED - spindle / stage / coolant stopped", LogLevel.Alarm);
            Notice = "Alarm latched. Complete all six troubleshooting steps.";
        }

        private void SetState(MachineState next)
        {
            if (next == State) return;
            State = next;
            if (!MotionEnabled) Sensors.StopMotion();
            Write("STATE -> " + next.ToString().ToUpperInvariant(), next == MachineState.Alarm ? LogLevel.Alarm : next == MachineState.Warning ? LogLevel.Warning : next == MachineState.Maintenance ? LogLevel.Maintenance : next == MachineState.Running ? LogLevel.Recovery : LogLevel.Info);
        }
        private bool Reject(string message) { Notice = message; Write(message, LogLevel.Warning); return false; }
        private void Write(string message, LogLevel level = LogLevel.Info) { Log.Write(Elapsed, message, level); }
    }
}
