using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace WaferSaw
{
    public sealed class DashboardUI : MonoBehaviour
    {
        public MachineController Machine;
        public Canvas Canvas;
        public Button[] Controls, Steps;
        public Toggle[] PmChecks;
        public Dropdown FaultSelector;
        public TrendGraph[] Graphs;
        public EventLogUI EventLog;
        public GameObject PmPanel;
        public Button ClosePm;
        public Text Status, Safety, AlarmTitle, AlarmDetails, Notice, TrendTitle, PmStatus, DemoStatus;
        public Text[] SensorValues, KpiValues;
        private float refreshTime;
        private int sampleVersion = -1;
        private bool bound;

        public void Build(MachineController machine)
        {
            Machine = machine;
            var canvasObject = new GameObject("Wafer Saw Dashboard", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.layer = 5; canvasObject.transform.SetParent(transform, false);
            Canvas = canvasObject.GetComponent<Canvas>(); Canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080); scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            var root = UiFactory.Rect(Canvas.transform, "16x9 Layout", 0, 0, 1920, 1080);
            root.anchorMin = root.anchorMax = root.pivot = new Vector2(0.5f, 0.5f); root.anchoredPosition = Vector2.zero;
            var header = UiFactory.Panel(root, "Header", 0, 0, 1920, 112, UiFactory.Background);
            UiFactory.Panel(header.transform, "Accent Rule", 24, 27, 5, 58, UiFactory.Cyan);
            UiFactory.Label(header.transform, "Title", "SEMICONDUCTOR EQUIPMENT DIGITAL TWIN", 45, 18, 1310, 48, 34);
            UiFactory.Label(header.transform, "Subtitle", "Wafer Saw Monitoring & Troubleshooting System", 47, 67, 1100, 28, 20, UiFactory.Muted);
            UiFactory.Label(header.transform, "Simulation", "GENERIC WS-01  /  EDUCATIONAL SIMULATION", 1390, 29, 504, 26, 17, UiFactory.Cyan);
            DemoStatus = UiFactory.Label(header.transform, "Demo Status", "SYNTHETIC DATA  |  NO PLC CONNECTION", 1390, 60, 504, 26, 16, UiFactory.Muted);

            UiFactory.Label(root, "Model Title", "01  /  GENERIC WAFER SAW", 30, 130, 680, 32, 22, UiFactory.Cyan);
            UiFactory.Label(root, "Model Legend", "Wafer + tape  /  vacuum chuck  /  XY stage  /  spindle + blade  /  vision", 30, 166, 990, 25, 16, UiFactory.Muted);
            UiFactory.Panel(root, "Motion Status Backdrop", 24, 572, 1004, 38, UiFactory.Background);
            Safety = UiFactory.Label(root, "Safety", "STOPPED  |  drag to orbit / scroll to zoom", 30, 575, 990, 30, 18, UiFactory.Muted);

            var alarm = UiFactory.Panel(root, "Alarm and Diagnosis", 24, 620, 1004, 142);
            AlarmTitle = UiFactory.Label(alarm.transform, "Alarm Title", "SYSTEM READY", 16, 7, 970, 32, 23, UiFactory.Cyan);
            AlarmDetails = UiFactory.Label(alarm.transform, "Alarm Details", "No active alarm. Start production to observe operating values.", 16, 43, 970, 56, 17, UiFactory.Muted);
            Notice = UiFactory.Label(alarm.transform, "Feedback", "Ready.", 16, 103, 970, 30, 17);

            string[] captions = { "START MACHINE", "STOP", "INJECT FAULT", "DIAGNOSE", "MAINTENANCE", "VERIFY", "PM CHECKLIST", "RESET DEMO", "DEMO MODE (AUTO)" };
            Controls = new Button[9];
            for (int i = 0; i < Controls.Length; i++)
                Controls[i] = UiFactory.Button(root, captions[i], captions[i], 24 + i % 5 * 203, 778 + i / 5 * 54, 192, 44);
            BuildSelector(root, 24 + 4 * 203, 832);

            var sequence = UiFactory.Panel(root, "Diagnostic Sequence", 24, 905, 1004, 151);
            UiFactory.Label(sequence.transform, "Sequence Title", "TROUBLESHOOTING SEQUENCE  /  complete in order", 14, 4, 975, 28, 17, UiFactory.Cyan);
            Steps = new Button[6];
            for (int i = 0; i < Steps.Length; i++)
                Steps[i] = UiFactory.Button(sequence.transform, "Step " + i, (i + 1) + ". Diagnostic step", 14 + i % 3 * 328, 39 + i / 3 * 51, 316, 42);

            var statusPanel = UiFactory.Panel(root, "Machine Status", 1054, 128, 842, 65);
            UiFactory.Label(statusPanel.transform, "Status Caption", "MACHINE STATUS", 18, 0, 286, 65, 20, UiFactory.Muted);
            Status = UiFactory.Label(statusPanel.transform, "State", "IDLE", 311, 0, 510, 65, 35, UiFactory.Muted);

            var sensors = UiFactory.Panel(root, "Sensor Grid", 1054, 208, 842, 177);
            string[] names = { "SPINDLE RPM", "VIBRATION", "VACUUM PRESSURE", "TEMPERATURE", "COOLING FLOW", "BLADE WEAR", "ALIGNMENT ERROR", "PM STATUS" };
            SensorValues = new Text[8];
            for (int i = 0; i < names.Length; i++)
            {
                float x = 17 + i % 2 * 417, y = 7 + i / 2 * 42;
                UiFactory.Label(sensors.transform, names[i], names[i], x, y, 215, 36, 16, UiFactory.Muted);
                SensorValues[i] = UiFactory.Label(sensors.transform, names[i] + " Value", "--", x + 220, y, 179, 36, 21, UiFactory.Cyan);
            }

            var trends = UiFactory.Panel(root, "Trends", 1054, 400, 842, 283);
            TrendTitle = UiFactory.Label(trends.transform, "Trend Title", "SENSOR TRENDS  /  24 s window  /  5 Hz", 17, 6, 810, 26, 17, UiFactory.Cyan);
            string[] graphNames = { "VIBRATION\n0 - 6 mm/s", "VACUUM\n-90 to -20 kPa", "TEMP (reference)\n30 - 70 C" };
            float[] minima = { 0, -90, 30 }, maxima = { 6, -20, 70 }, warning = { 2.5f, -65, 45 }, trip = { 4.5f, -50, 55 };
            Graphs = new TrendGraph[3];
            for (int i = 0; i < Graphs.Length; i++)
            {
                UiFactory.Label(trends.transform, "Channel " + i, graphNames[i], 17, 43 + i * 76, 154, 57, 15, UiFactory.Muted);
                var graph = UiFactory.Rect(trends.transform, "Trace " + i, 173, 43 + i * 76, 649, 59).gameObject.AddComponent<TrendGraph>();
                graph.Channel = i; graph.Minimum = minima[i]; graph.Maximum = maxima[i]; graph.WarningThreshold = warning[i]; graph.AlarmThreshold = trip[i];
                graph.color = i == 0 ? UiFactory.Cyan : i == 1 ? UiFactory.Green : UiFactory.Blue; graph.raycastTarget = false; Graphs[i] = graph;
            }

            var kpis = UiFactory.Panel(root, "KPI", 1054, 698, 842, 123);
            string[] kpiNames = { "UPTIME", "DOWNTIME", "OEE  (P 95% / Q 99%)", "MTTR", "TOTAL ALARMS", "WAFERS PROCESSED" };
            KpiValues = new Text[6];
            for (int i = 0; i < kpiNames.Length; i++)
            {
                float x = 17 + i % 3 * 277, y = 3 + i / 3 * 60;
                UiFactory.Label(kpis.transform, kpiNames[i], kpiNames[i], x, y, 258, 24, 14, UiFactory.Muted);
                KpiValues[i] = UiFactory.Label(kpis.transform, kpiNames[i] + " Value", "--", x, y + 23, 258, 31, 25, UiFactory.Green);
            }
            BuildLog(root);
            BuildPm(root);
            if (EventSystem.current == null)
            {
                var events = new GameObject("Wafer Saw EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
                events.transform.SetParent(transform, false);
                events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
            }
        }

        private void BuildSelector(Transform root, float x, float y)
        {
            var image = UiFactory.Panel(root, "Fault Selector", x, y, 192, 44, new Color32(34, 60, 78, 255)); image.raycastTarget = true;
            FaultSelector = image.gameObject.AddComponent<Dropdown>(); FaultSelector.targetGraphic = image;
            FaultSelector.captionText = UiFactory.Label(image.transform, "Caption", "Spindle Vibration", 9, 0, 175, 44, 15);
            FaultSelector.options.Clear();
            foreach (string option in new[] { "Random Fault", "Spindle Vibration", "Vacuum Low", "Cooling Flow Low" }) FaultSelector.options.Add(new Dropdown.OptionData(option));
            var template = UiFactory.Panel(image.transform, "Template", 0, -168, 232, 164); template.raycastTarget = true;
            var scroll = template.gameObject.AddComponent<ScrollRect>(); scroll.horizontal = false;
            var viewport = UiFactory.Rect(template.transform, "Viewport", 0, 0, 232, 164); viewport.gameObject.AddComponent<RectMask2D>();
            var content = UiFactory.Rect(viewport, "Content", 0, 0, 232, 40);
            var itemImage = UiFactory.Panel(content, "Item", 0, 0, 232, 40, new Color32(36, 65, 84, 255)); itemImage.raycastTarget = true;
            var toggle = itemImage.gameObject.AddComponent<Toggle>(); toggle.targetGraphic = itemImage;
            var check = UiFactory.Label(toggle.transform, "Check", ">", 4, 0, 18, 40, 18, UiFactory.Cyan); toggle.graphic = check;
            var itemText = UiFactory.Label(toggle.transform, "Item Label", "Fault", 24, 0, 203, 40, 16);
            scroll.viewport = viewport; scroll.content = content;
            FaultSelector.template = template.rectTransform; FaultSelector.itemText = itemText;
            FaultSelector.value = 1; FaultSelector.RefreshShownValue(); template.gameObject.SetActive(false);
        }

        private void BuildLog(Transform root)
        {
            var panel = UiFactory.Panel(root, "Event Log", 1054, 836, 842, 220);
            UiFactory.Label(panel.transform, "Event Log Title", "EVENT LOG  /  simulation clock  /  scroll for history", 16, 4, 810, 30, 17, UiFactory.Cyan);
            var viewport = UiFactory.Panel(panel.transform, "Log Viewport", 14, 39, 812, 168, new Color32(10, 26, 39, 255));
            viewport.raycastTarget = true; viewport.gameObject.AddComponent<RectMask2D>();
            var scroll = viewport.gameObject.AddComponent<ScrollRect>(); scroll.horizontal = false; scroll.movementType = ScrollRect.MovementType.Clamped; scroll.scrollSensitivity = 28;
            var content = UiFactory.Rect(viewport.transform, "Log Content", 0, 0, 792, 168);
            var text = UiFactory.Label(content, "Log Lines", "Ready", 6, 0, 786, 168, 16);
            text.alignment = TextAnchor.UpperLeft; text.verticalOverflow = VerticalWrapMode.Overflow; text.lineSpacing = 1.25f;
            scroll.viewport = viewport.rectTransform; scroll.content = content;
            EventLog = panel.gameObject.AddComponent<EventLogUI>(); EventLog.Scroll = scroll; EventLog.Text = text; EventLog.Content = content;
        }

        private void BuildPm(Transform root)
        {
            var shade = UiFactory.Panel(root, "PM Modal", 0, 0, 1920, 1080, new Color(0, 0, 0, 0.8f)); shade.raycastTarget = true; PmPanel = shade.gameObject;
            var panel = UiFactory.Panel(shade.transform, "PM Checklist", 604, 265, 712, 550);
            UiFactory.Label(panel.transform, "PM Heading", "PREVENTIVE MAINTENANCE", 24, 17, 607, 44, 28, UiFactory.Cyan);
            UiFactory.Label(panel.transform, "PM Safe Note", "Checklist requires a stopped machine. It does not clear alarms.", 24, 66, 662, 32, 17, UiFactory.Muted);
            PmChecks = new Toggle[6];
            for (int i = 0; i < PmChecks.Length; i++)
            {
                var box = UiFactory.Panel(panel.transform, PMManager.Items[i], 27, 123 + i * 51, 33, 33, new Color32(51, 76, 91, 255)); box.raycastTarget = true;
                var toggle = box.gameObject.AddComponent<Toggle>(); toggle.targetGraphic = box;
                var check = UiFactory.Panel(box.transform, "Check", 6, 6, 21, 21, UiFactory.Green); toggle.graphic = check;
                UiFactory.Label(panel.transform, "PM Label " + i, PMManager.Items[i], 82, 120 + i * 51, 570, 38, 23);
                PmChecks[i] = toggle;
            }
            PmStatus = UiFactory.Label(panel.transform, "PM Completion", "0 / 6 completed", 27, 442, 653, 40, 25, UiFactory.Orange);
            ClosePm = UiFactory.Button(panel.transform, "Close PM", "CLOSE", 502, 492, 184, 39);
            PmPanel.SetActive(false);
        }

        private void Start() { Bind(); Refresh(); }
        public void Bind()
        {
            if (bound) return; bound = true;
            Controls[0].onClick.AddListener(() => Machine.StartMachine()); Controls[1].onClick.AddListener(Machine.StopMachine);
            Controls[2].onClick.AddListener(() => Machine.InjectFault(FaultSelector.value == 0 ? (FaultKind)UnityEngine.Random.Range(1, 4) : (FaultKind)FaultSelector.value));
            Controls[3].onClick.AddListener(() => Machine.DiagnosticStep(0)); Controls[4].onClick.AddListener(() => Machine.DiagnosticStep(3));
            Controls[5].onClick.AddListener(() => Machine.DiagnosticStep(5)); Controls[6].onClick.AddListener(() => PmPanel.SetActive(true));
            Controls[7].onClick.AddListener(() => { Machine.ResetDemo(); PmPanel.SetActive(false); FaultSelector.value = 1; Refresh(); });
            Controls[8].onClick.AddListener(() => { Machine.StartDemo(); PmPanel.SetActive(false); }); ClosePm.onClick.AddListener(() => PmPanel.SetActive(false));
            for (int i = 0; i < 6; i++)
            {
                int index = i; Steps[i].onClick.AddListener(() => Machine.DiagnosticStep(index));
                PmChecks[i].onValueChanged.AddListener(value => { Machine.SetPM(index, value); Refresh(); });
            }
        }
        private void Update() { refreshTime += Time.unscaledDeltaTime; if (refreshTime >= 0.2f) { refreshTime = 0; Refresh(); } }
        public void Refresh()
        {
            if (Machine == null || Machine.Sensors == null) return;
            var m = Machine; var data = m.Sensors.Data;
            Status.text = m.State.ToString().ToUpperInvariant(); Status.color = UiFactory.StateColor(m.State);
            Safety.text = m.MotionEnabled ? "GUARDS CLOSED  |  stage + spindle active  |  drag to orbit / scroll to zoom" : "ZERO SPEED  |  spindle / stage / coolant stopped  |  drag to orbit";
            Safety.color = m.MotionEnabled ? UiFactory.Cyan : UiFactory.Muted;
            string[] readings = { data.Rpm.ToString("N0") + " RPM", data.Vibration.ToString("F2") + " mm/s", data.Vacuum.ToString("F1") + " kPa", data.Temperature.ToString("F1") + " °C", data.CoolingFlow.ToString("F2") + " L/min", data.BladeWear.ToString("F1") + " %", data.Alignment.ToString("F2") + " μm", m.PM.Completed ? "COMPLETED" : m.PM.Count + " / 6 CHECKED" };
            for (int i = 0; i < readings.Length; i++) SensorValues[i].text = readings[i];
            AlarmTitle.text = m.Alarms.Latched ? "ALARM " + m.Alarms.Current.Scenario.Code + "  /  " + m.Alarms.Current.Scenario.Description : m.State == MachineState.Verifying ? "VERIFICATION IN PROGRESS" : "SYSTEM " + (m.State == MachineState.Idle ? "READY" : "MONITORING");
            AlarmTitle.color = m.Alarms.Latched ? UiFactory.Red : UiFactory.Cyan;
            if (m.Alarms.Latched)
            {
                var alarm = m.Alarms.Current; var scenario = alarm.Scenario;
                AlarmDetails.text = "Now: " + scenario.Value(data) + "   |   At trip: " + scenario.Value(alarm.AtTrip) + "   |   Normal: " + scenario.NormalRange + "\nPossible causes: " + scenario.PossibleCauses;
            }
            else AlarmDetails.text = "Normal: 30,000 RPM  |  vibration 0.8-1.5 mm/s  |  vacuum -85 to -75 kPa\nTemperature 35-45 °C  |  flow 1.5-2.0 L/min  |  alignment 0-5 μm";
            Notice.text = m.State == MachineState.Verifying ? "VERIFYING " + (m.VerificationProgress * 100).ToString("F0") + "%  |  Calibration PASS  |  test wafer / all-channel interlock" : m.Notice;
            Notice.color = m.Notice == "Diagnostic Step Incorrect" ? UiFactory.Yellow : Color.white;
            TrendTitle.text = m.TrendsFrozen ? "SENSOR TRENDS  /  FROZEN AT TRIP  /  pre-alarm history" : "SENSOR TRENDS  /  24 s window  /  yellow: warning, red: trip";
            if (sampleVersion != m.SamplesVersion) { foreach (var graph in Graphs) graph.SetSamples(m.TrendSamples); sampleVersion = m.SamplesVersion; }
            KpiValues[0].text = Duration(m.Kpi.Uptime); KpiValues[1].text = Duration(m.Kpi.Downtime);
            KpiValues[2].text = (m.Kpi.Oee * 100).ToString("F1") + "%";
            KpiValues[3].text = m.Kpi.HasCompletedRepair ? Duration(m.Kpi.Mttr) : "--";
            KpiValues[4].text = m.Kpi.TotalAlarms.ToString(); KpiValues[5].text = m.Kpi.WafersProcessed.ToString();
            bool diagnostic = m.Alarms.Latched && (m.State == MachineState.Alarm || m.State == MachineState.Maintenance) && !m.DemoActive;
            Controls[0].interactable = m.State == MachineState.Idle && !m.DemoActive;
            Controls[1].interactable = true;
            Controls[2].interactable = m.State == MachineState.Running && m.Faults.Active == FaultKind.None && !m.DemoActive;
            Controls[3].interactable = Controls[4].interactable = Controls[5].interactable = diagnostic;
            Controls[6].interactable = !m.DemoActive; Controls[8].interactable = !m.DemoActive; FaultSelector.interactable = !m.DemoActive;
            for (int i = 0; i < 6; i++)
            {
                Steps[i].interactable = diagnostic;
                string mark = i == 5 && m.State == MachineState.Verifying ? "RUN  " : m.Troubleshooting.NextStep > i ? "OK  " : m.Troubleshooting.NextStep == i && m.Alarms.Latched ? "> " : "";
                Steps[i].GetComponentInChildren<Text>().text = mark + (i + 1) + ". " + m.Troubleshooting.Label(i, m.Alarms.Current == null ? null : m.Alarms.Current.Scenario);
                PmChecks[i].SetIsOnWithoutNotify(m.PM.IsChecked(i)); PmChecks[i].interactable = m.SafeForService;
            }
            PmStatus.text = m.PM.Completed ? "PM COMPLETED" : m.PM.Count + " / 6 completed";
            PmStatus.color = m.PM.Completed ? UiFactory.Green : UiFactory.Orange;
            DemoStatus.text = m.DemoActive ? "DEMO MODE ACTIVE  /  " + Duration(m.Elapsed) + " of ~01:34" : "SYNTHETIC DATA  |  NO PLC CONNECTION";
            EventLog.Refresh(m.Log);
        }
        private static string Duration(double seconds) { var span = TimeSpan.FromSeconds(seconds); return ((int)span.TotalMinutes).ToString("00") + ":" + span.Seconds.ToString("00"); }
    }
}
