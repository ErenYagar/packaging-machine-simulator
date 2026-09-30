using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace PackagingSim
{
    [RequireComponent(typeof(MachineController), typeof(MaintenanceChecklist))]
    public sealed class PackagingUI : MonoBehaviour
    {
        public TMP_FontAsset font;
        public Canvas Canvas { get; private set; }
        public GameObject AlarmPanel { get; private set; }
        public Button StartButton { get; private set; }
        public Button ResetButton { get; private set; }
        public Button RepairButton { get; private set; }
        public Button LowPressureButton { get; private set; }
        private MachineController controller;
        private TMP_Text stateText, pressureText, temperatureText, alarmText, logText, faultText;
        private GameObject normalPanel;
        private Button stopButton;
        private static readonly Color Ink = new Color(0.88f, 0.93f, 0.97f);
        private static readonly Color Teal = new Color(0.19f, 0.85f, 0.71f);
        private static readonly Color PanelColor = new Color(0.07f, 0.12f, 0.18f, 0.98f);

        private void Start()
        {
            controller = GetComponent<MachineController>();
            var canvasObject = new GameObject("Operator Canvas", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            Canvas = canvasObject.GetComponent<Canvas>();
            Canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600, 900);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            if (EventSystem.current == null)
            {
                var events = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
                events.transform.SetParent(transform, false);
            }
            Transform root = Canvas.transform;
            var header = Panel(root, "Header", 40, 22, 1520, 76, PanelColor);
            Label(header, "PACKAGING / 封裝機台模擬", 22, 8, 1080, 35, 28);
            Label(header, "TRAINING DEMO     •     狀態機 / 感測器 / 保養檢查", 22, 44, 1080, 22, 16);
            stateText = Label(header, "", 1180, 16, 320, 45, 25);
            Label(root, "01  /  MOLDING PRESS", 48, 126, 700, 38, 25);
            faultText = Label(root, "", 48, 167, 710, 32, 19);
            Label(root, "示意機構｜Running 時壓頭往復；Alarm 時停止", 48, 510, 710, 30, 18);

            var sensorPanel = Panel(root, "Sensors", 780, 125, 780, 150, PanelColor);
            Label(sensorPanel, "02  /  即時感測數據", 22, 12, 730, 30, 22);
            pressureText = Label(sensorPanel, "", 22, 52, 370, 46, 32);
            temperatureText = Label(sensorPanel, "", 410, 52, 350, 46, 32);
            SensorLimits limits = controller.Sensors.limits;
            Label(sensorPanel, $"壓力 {limits.minPressure:F2}–{limits.maxPressure:F2} MPa", 22, 107, 370, 28, 19);
            Label(sensorPanel, $"溫度 {limits.minTemperature:F0}–{limits.maxTemperature:F0} °C", 410, 107, 350, 28, 19);

            normalPanel = Panel(root, "Normal Status", 780, 294, 780, 250, PanelColor).gameObject;
            Label(normalPanel.transform, "03  /  警報監控", 22, 16, 730, 32, 22);
            Label(normalPanel.transform, "目前無鎖存警報", 22, 71, 730, 45, 30).color = Teal;
            Label(normalPanel.transform, "按「注入低壓」開始排故練習。\n壓力、溫度皆在上下限內時才允許啟動。", 22, 135, 730, 85, 21);
            AlarmPanel = Panel(root, "Alarm Panel", 780, 294, 780, 250, new Color(0.25f, 0.08f, 0.10f)).gameObject;
            Label(AlarmPanel.transform, "03  /  ALARM · 機台已停止", 22, 12, 730, 34, 24).color = new Color(1f, 0.52f, 0.45f);
            alarmText = Label(AlarmPanel.transform, "", 22, 52, 734, 178, 21);

            var controls = Panel(root, "Controls", 40, 560, 710, 124, PanelColor);
            StartButton = MakeButton(controls, "啟動 [S]", 14, 14, 159, controller.StartMachine);
            stopButton = MakeButton(controls, "停止 [X]", 187, 14, 159, controller.StopMachine);
            RepairButton = MakeButton(controls, "模擬排故 [F]", 360, 14, 159, controller.Repair);
            ResetButton = MakeButton(controls, "警報復歸 [R]", 533, 14, 159, controller.ResetAlarm);
            LowPressureButton = MakeButton(controls, "注入低壓 [L]", 14, 69, 159, () => controller.Inject(SimulatedFault.LowPressure));
            MakeButton(controls, "注入高壓", 187, 69, 159, () => controller.Inject(SimulatedFault.HighPressure));
            MakeButton(controls, "注入低溫", 360, 69, 159, () => controller.Inject(SimulatedFault.LowTemperature));
            MakeButton(controls, "注入高溫", 533, 69, 159, () => controller.Inject(SimulatedFault.HighTemperature));
            var logs = Panel(root, "Event Log", 40, 702, 710, 124, PanelColor);
            logText = Label(logs, "", 14, 8, 684, 110, 16);

            var maintenance = Panel(root, "Maintenance", 780, 562, 780, 264, PanelColor);
            GetComponent<MaintenanceChecklist>().Build(this, maintenance);
            Label(root, "流程：啟動 → 注入故障 → 觀察告警 → 模擬排故 → 讀值回常 → 復歸 → 重新啟動", 40, 848, 1510, 28, 21);
            Refresh();
        }

        private void LateUpdate() { if (Canvas != null) Refresh(); }

        private void Refresh()
        {
            var machine = controller.Machine;
            bool alarm = machine.State == MachineState.Alarm;
            stateText.text = machine.State == MachineState.Idle ? "●  Idle / 空閒" :
                machine.State == MachineState.Running ? "●  Running / 運行" : "●  Alarm / 警報";
            stateText.color = alarm ? new Color(1f, 0.52f, 0.45f) : Teal;
            pressureText.text = $"{machine.Pressure:F3} <size=20>MPa</size>";
            temperatureText.text = $"{machine.Temperature:F1} <size=20>°C</size>";
            faultText.text = "注入故障：" + MachineController.FaultName(controller.Sensors.Fault);
            AlarmPanel.SetActive(alarm);
            normalPanel.SetActive(!alarm);
            alarmText.text = machine.CanReset
                ? "感測值已恢復正常；警報仍鎖存。\n請按「警報復歸 [R]」回到 Idle。\n\n最初告警：\n" + machine.LatchedFault
                : machine.ActiveFault + "\n\n處置：按「模擬排故 [F]」，等待讀值回常。";
            StartButton.interactable = machine.State == MachineState.Idle && machine.HasSample && machine.ActiveFault.Length == 0;
            stopButton.interactable = machine.State == MachineState.Running;
            ResetButton.interactable = machine.CanReset;
            logText.text = controller.LogText;
        }

        internal RectTransform Panel(Transform parent, string name, float x, float y, float w, float h, Color color)
        {
            var rect = Rect(parent, name, x, y, w, h);
            rect.gameObject.AddComponent<Image>().color = color;
            return rect;
        }

        internal TMP_Text Label(Transform parent, string value, float x, float y, float w, float h, int size)
        {
            var text = Rect(parent, "Text", x, y, w, h).gameObject.AddComponent<TextMeshProUGUI>();
            text.font = font;
            text.text = value;
            text.fontSize = size;
            text.enableAutoSizing = true;
            text.fontSizeMin = size * 0.75f;
            text.fontSizeMax = size;
            text.color = Ink;
            text.raycastTarget = false;
            text.enableWordWrapping = true;
            return text;
        }

        internal Button MakeButton(Transform parent, string title, float x, float y, float width, UnityAction action)
        {
            var rect = Panel(parent, title, x, y, width, 40, new Color(0.15f, 0.28f, 0.35f));
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = rect.GetComponent<Image>();
            button.onClick.AddListener(action);
            var label = Label(rect, title, 4, 3, width - 8, 34, 19);
            label.alignment = TextAlignmentOptions.Center;
            return button;
        }

        internal static RectTransform Rect(Transform parent, string name, float x, float y, float w, float h)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(w, h);
            return rect;
        }
    }
}
