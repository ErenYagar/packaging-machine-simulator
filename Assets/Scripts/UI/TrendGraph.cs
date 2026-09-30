using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace WaferSaw
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class TrendGraph : MaskableGraphic
    {
        public TrendGraph() { useLegacyMeshGeneration = false; }
        public int Channel;
        public float Minimum, Maximum, WarningThreshold, AlarmThreshold;
        private readonly List<float> values = new List<float>();
        public int SampleCount => values.Count;

        public void SetSamples(IReadOnlyList<SensorData> samples)
        {
            values.Clear();
            for (int i = 0; i < samples.Count; i++)
                values.Add(Channel == 0 ? samples[i].Vibration : Channel == 1 ? samples[i].Vacuum : samples[i].Temperature);
            SetVerticesDirty();
        }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear(); Rect rect = rectTransform.rect;
            for (int i = 0; i <= 4; i++)
            {
                float y = rect.yMin + rect.height * i / 4;
                Line(vh, new Vector2(rect.xMin, y), new Vector2(rect.xMax, y), 1, new Color(0.25f, 0.38f, 0.46f, 0.5f));
            }
            Threshold(vh, rect, WarningThreshold, UiFactory.Yellow);
            Threshold(vh, rect, AlarmThreshold, UiFactory.Red);
            for (int i = 1; i < values.Count; i++)
                Line(vh, Point(rect, i - 1), Point(rect, i), 2.5f, color);
        }
        private Vector2 Point(Rect rect, int index)
        { return new Vector2(rect.xMax - (values.Count - 1 - index) / 119f * rect.width, Y(rect, values[index])); }
        private float Y(Rect rect, float value) { return rect.yMin + Mathf.InverseLerp(Minimum, Maximum, value) * rect.height; }
        private void Threshold(VertexHelper vh, Rect rect, float value, Color tint)
        {
            for (float x = rect.xMin; x < rect.xMax; x += 14)
                Line(vh, new Vector2(x, Y(rect, value)), new Vector2(Mathf.Min(x + 7, rect.xMax), Y(rect, value)), 1, new Color(tint.r, tint.g, tint.b, 0.5f));
        }
        private static void Line(VertexHelper vh, Vector2 a, Vector2 b, float width, Color tint)
        {
            Vector2 direction = b - a;
            Vector2 normal = new Vector2(-direction.y, direction.x).normalized * width * 0.5f;
            int start = vh.currentVertCount;
            vh.AddVert(a - normal, tint, Vector2.zero); vh.AddVert(a + normal, tint, Vector2.zero);
            vh.AddVert(b + normal, tint, Vector2.zero); vh.AddVert(b - normal, tint, Vector2.zero);
            vh.AddTriangle(start, start + 1, start + 2); vh.AddTriangle(start, start + 2, start + 3);
        }
    }
}
