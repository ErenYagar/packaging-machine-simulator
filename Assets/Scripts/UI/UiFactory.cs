using UnityEngine;
using UnityEngine.UI;

namespace WaferSaw
{
    public static class UiFactory
    {
        public static readonly Color Background = new Color32(8, 20, 32, 255);
        public static readonly Color PanelColor = new Color32(18, 37, 53, 255);
        public static readonly Color Muted = new Color32(163, 188, 205, 255);
        public static readonly Color Cyan = new Color32(94, 205, 224, 255);
        public static readonly Color Green = new Color32(101, 220, 162, 255);
        public static readonly Color Yellow = new Color32(249, 208, 91, 255);
        public static readonly Color Red = new Color32(255, 111, 113, 255);
        public static readonly Color Orange = new Color32(255, 166, 87, 255);
        public static readonly Color Blue = new Color32(106, 173, 255, 255);
        public static Color StateColor(MachineState state)
        {
            switch (state)
            {
                case MachineState.Running: return Green;
                case MachineState.Warning: return Yellow;
                case MachineState.Alarm: return Red;
                case MachineState.Maintenance: return Orange;
                case MachineState.Verifying: return Blue;
                default: return Muted;
            }
        }
        public static RectTransform Rect(Transform parent, string name, float x, float y, float width, float height)
        {
            var go = new GameObject(name, typeof(RectTransform)); go.layer = 5;
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(x, -y); rect.sizeDelta = new Vector2(width, height);
            return rect;
        }
        public static Image Panel(Transform parent, string name, float x, float y, float width, float height, Color? color = null)
        {
            var image = Rect(parent, name, x, y, width, height).gameObject.AddComponent<Image>();
            image.color = color ?? PanelColor; image.raycastTarget = false; return image;
        }
        public static Text Label(Transform parent, string name, string value, float x, float y, float width, float height, int size = 18, Color? color = null)
        {
            var text = Rect(parent, name, x, y, width, height).gameObject.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); text.text = value;
            text.fontSize = size; text.color = color ?? Color.white; text.alignment = TextAnchor.MiddleLeft;
            text.horizontalOverflow = HorizontalWrapMode.Wrap; text.verticalOverflow = VerticalWrapMode.Truncate;
            text.raycastTarget = false; text.supportRichText = true; return text;
        }
        public static Button Button(Transform parent, string name, string caption, float x, float y, float width, float height)
        {
            var image = Panel(parent, name, x, y, width, height, new Color32(34, 60, 78, 255));
            image.raycastTarget = true;
            var button = image.gameObject.AddComponent<Button>(); button.targetGraphic = image;
            var colors = button.colors; colors.highlightedColor = new Color(0.7f, 0.95f, 1); colors.pressedColor = new Color(0.45f, 0.8f, 0.9f); colors.disabledColor = new Color(0.43f, 0.47f, 0.52f); button.colors = colors;
            var label = Label(button.transform, "Label", caption, 7, 0, width - 14, height, 16);
            label.alignment = TextAnchor.MiddleCenter; return button;
        }
    }
}
