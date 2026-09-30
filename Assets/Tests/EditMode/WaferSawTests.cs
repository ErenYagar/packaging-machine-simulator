using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace WaferSaw.Tests
{
    public sealed class WaferSawTests
    {
        private GameObject root;
        private MachineController machine;
        [SetUp] public void SetUp() { root = new GameObject("Test Machine"); machine = root.AddComponent<MachineController>(); machine.AutomaticUpdates = false; machine.ResetDemo(); }
        [TearDown] public void TearDown() { Object.DestroyImmediate(root); }
        private void Tick(float seconds) { for (int i = 0; i < Mathf.CeilToInt(seconds / 0.1f); i++) machine.Advance(0.1f); }
        private void Trip(FaultKind kind)
        {
            Assert.That(machine.StartMachine(), Is.True); Tick(3); Assert.That(machine.InjectFault(kind), Is.True);
            bool warning = false;
            for (int i = 0; i < 400 && machine.State != MachineState.Alarm; i++) { machine.Advance(0.1f); warning |= machine.State == MachineState.Warning; }
            Assert.That(warning, Is.True); Assert.That(machine.State, Is.EqualTo(MachineState.Alarm));
        }

        [TestCase(FaultKind.SpindleVibration, "E203")]
        [TestCase(FaultKind.VacuumLow, "E117")]
        [TestCase(FaultKind.CoolingFlowLow, "E305")]
        public void FaultWarnsTripsAndRecoversOnlyAfterOrderedMaintenance(FaultKind kind, string code)
        {
            Trip(kind); Assert.That(machine.Alarms.Current.Scenario.Code, Is.EqualTo(code));
            Assert.That(machine.MotionEnabled, Is.False); Assert.That(machine.Sensors.Data.Rpm, Is.Zero); Assert.That(machine.Sensors.Data.CoolingFlow, Is.Zero);
            Assert.That(machine.DiagnosticStep(3), Is.False); Assert.That(machine.Notice, Is.EqualTo("Diagnostic Step Incorrect"));
            Assert.That(machine.DiagnosticStep(5), Is.False); Assert.That(machine.StartMachine(), Is.False);
            machine.StopMachine(); Assert.That(machine.State, Is.EqualTo(MachineState.Alarm));
            for (int step = 0; step < 6; step++) { Assert.That(machine.DiagnosticStep(step), Is.True); Tick(2); }
            Assert.That(machine.State, Is.EqualTo(MachineState.Verifying)); Tick(7);
            Assert.That(machine.State, Is.EqualTo(MachineState.Running)); Assert.That(machine.Alarms.Latched, Is.False);
            Assert.That(machine.Kpi.TotalAlarms, Is.EqualTo(1)); Assert.That(machine.Kpi.HasCompletedRepair, Is.True); Assert.That(machine.Kpi.Mttr, Is.GreaterThan(8));
            Assert.That(machine.Log.Entries.Any(entry => entry.Message.Contains("MACHINE RECOVERED")), Is.True);
            if (kind == FaultKind.SpindleVibration) Assert.That(machine.Sensors.Data.BladeWear, Is.LessThan(1));
        }

        [Test] public void FailedVerificationRelatchesAndRequiresNewDiagnosis()
        {
            Trip(FaultKind.SpindleVibration);
            for (int step = 0; step < 6; step++) machine.DiagnosticStep(step);
            machine.Faults.Inject(FaultKind.SpindleVibration); machine.Faults.Tick(30); Tick(9);
            Assert.That(machine.State, Is.EqualTo(MachineState.Alarm)); Assert.That(machine.Troubleshooting.NextStep, Is.Zero);
            Assert.That(machine.Kpi.TotalAlarms, Is.EqualTo(2)); Assert.That(machine.Kpi.HasCompletedRepair, Is.False);
            Assert.That(machine.DiagnosticStep(5), Is.False); Assert.That(machine.Sensors.Data.Rpm, Is.Zero);
        }

        [Test] public void StopCannotBypassInjectedFaultOrVerification()
        {
            machine.StartMachine(); Tick(3); machine.InjectFault(FaultKind.VacuumLow); machine.StopMachine();
            Assert.That(machine.State, Is.EqualTo(MachineState.Alarm)); Assert.That(machine.StartMachine(), Is.False);
            for (int i = 0; i < 6; i++) machine.DiagnosticStep(i);
            machine.StopMachine(); Assert.That(machine.State, Is.EqualTo(MachineState.Alarm)); Assert.That(machine.MotionEnabled, Is.False);
        }

        [Test] public void TrendHistoryAtTripSurvivesLongDiagnosis()
        {
            Trip(FaultKind.SpindleVibration); var history = machine.TrendSamples.ToArray();
            Assert.That(history.Length, Is.InRange(2, 120)); Assert.That(history.Max(sample => sample.Vibration), Is.GreaterThanOrEqualTo(4.5f));
            Assert.That(history.Min(sample => sample.Vibration), Is.LessThan(1.5f)); Tick(80);
            Assert.That(machine.TrendsFrozen, Is.True); CollectionAssert.AreEqual(history, machine.TrendSamples);
            Assert.That(machine.Alarms.Latched, Is.True);
        }

        [Test] public void PmRequiresStopAndNeverClearsAlarm()
        {
            machine.StartMachine(); Assert.That(machine.SetPM(0, true), Is.False); machine.StopMachine();
            for (int i = 0; i < 6; i++) Assert.That(machine.SetPM(i, true), Is.True);
            machine.SetPM(5, true);
            Assert.That(machine.PM.Completed, Is.True); Assert.That(machine.Log.Entries.Count(entry => entry.Message.StartsWith("PM COMPLETED")), Is.EqualTo(1));
            Trip(FaultKind.CoolingFlowLow); Assert.That(machine.Alarms.Latched, Is.True); Assert.That(machine.State, Is.EqualTo(MachineState.Alarm));
        }

        [Test] public void ResetRestoresEverySubsystemAndStartsNewSession()
        {
            Trip(FaultKind.SpindleVibration); machine.DiagnosticStep(0); machine.SetPM(0, true); Tick(12); machine.ResetDemo();
            Assert.That(machine.State, Is.EqualTo(MachineState.Idle)); Assert.That(machine.Elapsed, Is.Zero);
            Assert.That(machine.Faults.Active, Is.EqualTo(FaultKind.None)); Assert.That(machine.Alarms.Latched, Is.False);
            Assert.That(machine.TrendSamples.Count, Is.Zero); Assert.That(machine.PM.Count, Is.Zero);
            Assert.That(machine.Kpi.TotalAlarms, Is.Zero); Assert.That(machine.Kpi.PlannedTime, Is.Zero);
            Assert.That(machine.Log.Entries.Count, Is.EqualTo(2)); Assert.That(machine.DemoActive, Is.False); Assert.That(machine.StartMachine(), Is.True);
        }

        [Test] public void KpiUsesProductionTimeAndMeanOfCompletedRepairs()
        {
            var kpi = new KPIManager(); kpi.Tick(20, MachineState.Idle); Assert.That(kpi.PlannedTime, Is.Zero);
            kpi.Start(); kpi.Tick(40, MachineState.Running); kpi.Alarm(); kpi.Tick(10, MachineState.Alarm); kpi.Tick(10, MachineState.Verifying); kpi.Recover();
            Assert.That(kpi.WafersProcessed, Is.EqualTo(4)); Assert.That(kpi.Downtime, Is.EqualTo(20));
            Assert.That(kpi.Mttr, Is.EqualTo(20)); Assert.That(kpi.Oee, Is.EqualTo(40d / 60 * 0.95 * 0.99).Within(0.0001));
            kpi.Alarm(); kpi.Tick(10, MachineState.Maintenance); kpi.Recover(); Assert.That(kpi.Mttr, Is.EqualTo(15));
        }

        [Test] public void AutoDemoCompletesWithinThreeMinutes()
        {
            machine.StartDemo(); Tick(96);
            Assert.That(machine.DemoActive, Is.False); Assert.That(machine.State, Is.EqualTo(MachineState.Running));
            Assert.That(machine.Kpi.HasCompletedRepair, Is.True); Assert.That(machine.Kpi.TotalAlarms, Is.EqualTo(1));
            Assert.That(machine.Log.Entries.Any(entry => entry.Message.StartsWith("DEMO COMPLETE")), Is.True);
        }

        [Test] public void SensorsAreSmoothAndNormalAtSteadyProduction()
        {
            machine.StartMachine(); Tick(4); SensorData last = machine.Sensors.Data;
            for (int i = 0; i < 300; i++)
            {
                machine.Advance(0.1f); var next = machine.Sensors.Data;
                Assert.That(next.ReadyForProduction, Is.True); Assert.That(Mathf.Abs(next.Vibration - last.Vibration), Is.LessThan(0.06f)); last = next;
            }
            Assert.That(machine.TrendSamples.Count, Is.EqualTo(120));
        }

        [Test] public void CoolingFaultRaisesTemperatureAndMaintenanceCoolsIt()
        {
            Trip(FaultKind.CoolingFlowLow); float hot = machine.Sensors.Data.Temperature;
            Assert.That(hot, Is.GreaterThan(45)); for (int step = 0; step <= 3; step++) machine.DiagnosticStep(step);
            Tick(12); Assert.That(machine.Sensors.Data.Temperature, Is.LessThan(hot)); Assert.That(machine.MotionEnabled, Is.False);
        }
    }
}
