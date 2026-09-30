using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PackagingSim
{
    [RequireComponent(typeof(SensorSimulator))]
    public sealed class MachineController : MonoBehaviour
    {
        public MachineStateMachine Machine { get; private set; }
        public SensorSimulator Sensors { get; private set; }
        public string LogText => string.Join("\n", log);
        private readonly Queue<string> log = new Queue<string>();
        private InputActionMap controls;

        private void Awake()
        {
            Sensors = GetComponent<SensorSimulator>();
            Sensors.Initialize();
            Machine = new MachineStateMachine(Sensors.limits);
            Machine.StateChanged += OnStateChanged;
            Machine.Sample(Sensors.Pressure, Sensors.Temperature);
            WriteLog("機台就緒；按啟動開始模擬。");
            controls = new InputActionMap("Machine");
            Bind("Start", "<Keyboard>/s", StartMachine);
            Bind("Stop", "<Keyboard>/x", StopMachine);
            Bind("LowPressure", "<Keyboard>/l", () => Inject(SimulatedFault.LowPressure));
            Bind("Repair", "<Keyboard>/f", Repair);
            Bind("Reset", "<Keyboard>/r", ResetAlarm);
        }

        private void Bind(string name, string binding, Action callback)
        {
            controls.AddAction(name, InputActionType.Button, binding).performed += _ => callback();
        }

        private void OnEnable() => controls?.Enable();
        private void OnDisable() => controls?.Disable();
        private void OnDestroy()
        {
            if (Machine != null) Machine.StateChanged -= OnStateChanged;
            controls?.Dispose();
        }

        private void Update()
        {
            Sensors.Tick(Time.deltaTime, Machine.State == MachineState.Running);
            Machine.Sample(Sensors.Pressure, Sensors.Temperature);
        }

        public void StartMachine()
        {
            if (!Machine.Start()) WriteLog("無法啟動：需在 Idle，且感測值正常。");
        }

        public void StopMachine() => Machine.Stop();

        public void Inject(SimulatedFault fault)
        {
            Sensors.Inject(fault);
            WriteLog("模擬故障：" + FaultName(fault));
        }

        public void Repair()
        {
            Sensors.Repair();
            WriteLog("已排除模擬故障；等待讀值回到範圍，再按警報復歸。");
        }

        public void ResetAlarm()
        {
            if (!Machine.ResetAlarm()) WriteLog("復歸未執行：需有警報，且壓力與溫度皆恢復正常。");
        }

        public void WriteLog(string message)
        {
            log.Enqueue(DateTime.Now.ToString("HH:mm:ss") + "  " + message);
            while (log.Count > 6) log.Dequeue();
        }

        private void OnStateChanged(MachineState state) => WriteLog("狀態 → " + state);

        public static string FaultName(SimulatedFault fault)
        {
            switch (fault)
            {
                case SimulatedFault.LowPressure: return "低壓";
                case SimulatedFault.HighPressure: return "高壓";
                case SimulatedFault.LowTemperature: return "低溫";
                case SimulatedFault.HighTemperature: return "高溫";
                default: return "無";
            }
        }
    }
}
