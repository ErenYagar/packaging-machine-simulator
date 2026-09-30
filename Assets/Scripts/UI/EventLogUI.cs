using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace WaferSaw
{
    public sealed class EventLogUI : MonoBehaviour
    {
        public ScrollRect Scroll;
        public Text Text;
        public RectTransform Content;
        private EventLogger lastLogger;
        private int revision = -1;

        public void Refresh(EventLogger logger)
        {
            if (logger == lastLogger && revision == logger.Revision) return;
            lastLogger = logger; revision = logger.Revision;
            var builder = new StringBuilder();
            foreach (var entry in logger.Entries)
            {
                Color tint = entry.Level == LogLevel.Alarm ? UiFactory.Red : entry.Level == LogLevel.Warning ? UiFactory.Yellow
                    : entry.Level == LogLevel.Maintenance ? UiFactory.Orange : entry.Level == LogLevel.Recovery ? UiFactory.Green : Color.white;
                builder.Append("<color=#").Append(ColorUtility.ToHtmlStringRGB(tint)).Append('>')
                    .Append(entry.Time).Append("  ").Append(entry.Message).Append("</color>\n");
            }
            Text.text = builder.ToString();
            Canvas.ForceUpdateCanvases();
            float height = Mathf.Max(Scroll.viewport.rect.height, Text.preferredHeight + 12);
            Content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
            Text.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
            Canvas.ForceUpdateCanvases(); Scroll.verticalNormalizedPosition = 0;
        }
    }
}
