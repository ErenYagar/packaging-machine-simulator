using NUnit.Framework;
using UnityEngine;

namespace PackagingSim.Tests
{
    public sealed class MachineTests
    {
        [Test]
        public void CannotStartWithoutSample()
        {
            var machine = new MachineStateMachine(new SensorLimits());
            Assert.That(machine.Start(), Is.False);
            Assert.That(machine.State, Is.EqualTo(MachineState.Idle));
        }

        [TestCase(0.45f, 150f)]
        [TestCase(0.70f, 190f)]
        public void ExactLimitsAreAllowed(float pressure, float temperature)
        {
            var machine = new MachineStateMachine(new SensorLimits());
            machine.Sample(pressure, temperature);
            Assert.That(machine.Start(), Is.True);
            machine.Stop();
            Assert.That(machine.State, Is.EqualTo(MachineState.Idle));
        }

        [TestCase(0.449f, 170f, "壓力過低")]
        [TestCase(0.701f, 170f, "壓力過高")]
        [TestCase(0.57f, 149.9f, "溫度過低")]
        [TestCase(0.57f, 190.1f, "溫度過高")]
        public void FaultLatchesUntilHealthyAndReset(float pressure, float temperature, string reason)
        {
            var machine = new MachineStateMachine(new SensorLimits());
            machine.Sample(0.57f, 170f);
            machine.Start();
            machine.Sample(pressure, temperature);
            Assert.That(machine.State, Is.EqualTo(MachineState.Alarm));
            Assert.That(machine.ActiveFault, Does.Contain(reason));
            Assert.That(machine.ResetAlarm(), Is.False);
            Assert.That(machine.Start(), Is.False);
            machine.Stop();
            Assert.That(machine.State, Is.EqualTo(MachineState.Alarm));
            machine.Sample(0.57f, 170f);
            Assert.That(machine.State, Is.EqualTo(MachineState.Alarm));
            Assert.That(machine.LatchedFault, Does.Contain(reason));
            Assert.That(machine.ResetAlarm(), Is.True);
            Assert.That(machine.State, Is.EqualTo(MachineState.Idle));
            Assert.That(machine.Start(), Is.True);
        }

        [Test]
        public void SimultaneousFaultsMustBothRecover()
        {
            var machine = new MachineStateMachine(new SensorLimits());
            machine.Sample(0.1f, 210f);
            Assert.That(machine.ActiveFault, Does.Contain("壓力過低").And.Contain("溫度過高"));
            machine.Sample(0.57f, 210f);
            Assert.That(machine.CanReset, Is.False);
            machine.Sample(0.57f, 170f);
            Assert.That(machine.CanReset, Is.True);
        }

        [TestCase(float.NaN, 170f)]
        [TestCase(0.57f, float.PositiveInfinity)]
        public void InvalidSensorDataTriggersAlarm(float pressure, float temperature)
        {
            var machine = new MachineStateMachine(new SensorLimits());
            machine.Sample(pressure, temperature);
            Assert.That(machine.State, Is.EqualTo(MachineState.Alarm));
            Assert.That(machine.ResetAlarm(), Is.False);
        }

        [Test]
        public void InvalidConfigurationBlocksOperation()
        {
            var machine = new MachineStateMachine(new SensorLimits { minPressure = 0.9f, maxPressure = 0.7f });
            machine.Sample(0.6f, 170f);
            Assert.That(machine.Start(), Is.False);
            Assert.That(machine.ActiveFault, Does.Contain("閾值設定無效"));
        }

        [Test]
        public void StateEventIsNotRepeatedForEveryBadSample()
        {
            var machine = new MachineStateMachine(new SensorLimits());
            int events = 0;
            machine.StateChanged += _ => events++;
            for (int i = 0; i < 100; i++) machine.Sample(0.1f, 170f);
            Assert.That(events, Is.EqualTo(1));
        }

        [TestCase(SimulatedFault.LowPressure)]
        [TestCase(SimulatedFault.HighPressure)]
        [TestCase(SimulatedFault.LowTemperature)]
        [TestCase(SimulatedFault.HighTemperature)]
        public void SensorInjectionAndRepairDriveTheStateMachine(SimulatedFault fault)
        {
            var go = new GameObject("Sensor under test");
            try
            {
                var sensor = go.AddComponent<SensorSimulator>();
                sensor.Initialize();
                var machine = new MachineStateMachine(sensor.limits);
                machine.Sample(sensor.Pressure, sensor.Temperature);
                machine.Start();
                sensor.Inject(fault);
                for (int i = 0; i < 200; i++)
                {
                    sensor.Tick(0.02f, machine.State == MachineState.Running);
                    machine.Sample(sensor.Pressure, sensor.Temperature);
                }
                Assert.That(machine.State, Is.EqualTo(MachineState.Alarm));
                Assert.That(machine.CanReset, Is.False, "Stopping must not remove the injected fault.");
                sensor.Repair();
                Assert.That(machine.ResetAlarm(), Is.False, "Repair must not instantly reset out-of-range readings.");
                for (int i = 0; i < 200; i++)
                {
                    sensor.Tick(0.02f, false);
                    machine.Sample(sensor.Pressure, sensor.Temperature);
                }
                Assert.That(machine.State, Is.EqualTo(MachineState.Alarm));
                Assert.That(machine.ResetAlarm(), Is.True);
            }
            finally { Object.DestroyImmediate(go); }
        }
    }
}
