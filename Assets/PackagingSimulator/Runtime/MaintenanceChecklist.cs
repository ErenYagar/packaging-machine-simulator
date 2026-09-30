using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PackagingSim
{
    public sealed class MaintenanceChecklist : MonoBehaviour
    {
        public string[] tasks = { "檢查氣源與管路是否洩漏", "確認模具表面清潔", "確認溫控與冷卻功能", "檢查防護罩與緊急停止裝置" };
        public Toggle[] Toggles { get; private set; }
        public int CompletedCount { get; private set; }
        private TMP_Text progress;

        internal void Build(PackagingUI ui, Transform parent)
        {
            ui.Label(parent, "04  /  保養檢查清單", 22, 14, 530, 32, 23);
            progress = ui.Label(parent, "", 608, 14, 150, 32, 22);
            Toggles = new Toggle[tasks.Length];
            for (int i = 0; i < tasks.Length; i++)
            {
                var row = PackagingUI.Rect(parent, "Task " + i, 22, 62 + i * 38, 730, 34);
                var toggle = row.gameObject.AddComponent<Toggle>();
                var box = ui.Panel(row, "Checkbox", 0, 3, 26, 26, new Color(0.24f, 0.34f, 0.43f));
                var mark = ui.Panel(box, "Checked", 5, 5, 16, 16, new Color(0.19f, 0.85f, 0.71f));
                toggle.targetGraphic = box.GetComponent<Image>();
                toggle.graphic = mark.GetComponent<Image>();
                toggle.isOn = false;
                ui.Label(row, tasks[i], 44, 0, 680, 34, 21);
                toggle.onValueChanged.AddListener(_ => UpdateProgress());
                Toggles[i] = toggle;
            }
            ui.Label(parent, "勾選僅記錄本次檢查，不會排除故障或解除警報。", 22, 224, 730, 26, 17);
            UpdateProgress();
        }

        private void UpdateProgress()
        {
            CompletedCount = 0;
            foreach (var toggle in Toggles) if (toggle.isOn) CompletedCount++;
            progress.text = $"{CompletedCount} / {Toggles.Length} 完成";
        }
    }
}
