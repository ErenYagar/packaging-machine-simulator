using UnityEngine;
using UnityEngine.InputSystem;

namespace WaferSaw
{
    public sealed class MachineView : MonoBehaviour
    {
        public MachineController Machine;
        public Transform Stage, Rotor, CameraPivot;
        public Camera MachineCamera;
        public Renderer[] Lamps;
        public Material[] Materials;
        public float Travel { get; private set; }
        private Material[] lampInstances;
        private float yaw = -37, pitch = 18, distance = 8.6f;
        private bool dragging;
        private static readonly Vector3 ViewTarget = new Vector3(0, 1.58f, 0);

        public void Build(MachineController machine)
        {
            Machine = machine;
            Color[] colors = { new Color32(207, 219, 222, 255), new Color32(72, 103, 124, 255), new Color32(123, 142, 154, 255), new Color32(99, 157, 185, 255), new Color32(49, 94, 155, 255), new Color32(25, 42, 54, 255), new Color32(69, 109, 137, 255) };
            Materials = new Material[colors.Length];
            for (int i = 0; i < colors.Length; i++)
            {
                Materials[i] = new Material(Shader.Find(i == 6 ? "Unlit/Color" : "Standard")) { name = "WS Surface " + i, color = colors[i] };
                if (i != 6) { Materials[i].SetFloat("_Metallic", i == 2 || i == 3 ? 0.72f : 0.18f); Materials[i].SetFloat("_Glossiness", 0.35f); }
            }
            Part("Machine Cabinet", PrimitiveType.Cube, transform, new Vector3(0, 0.72f, 0), new Vector3(3.25f, 1.4f, 2), 0);
            Part("Front Service Panel", PrimitiveType.Cube, transform, new Vector3(0, 0.69f, -1.015f), new Vector3(2.96f, 0.87f, 0.025f), 2);
            for (int i = -1; i <= 1; i += 2)
            {
                Part("Panel Handle", PrimitiveType.Cube, transform, new Vector3(i * 0.69f, 0.89f, -1.052f), new Vector3(0.045f, 0.2f, 0.055f), 5);
                Part("Frame Post", PrimitiveType.Cube, transform, new Vector3(i * 1.53f, 2.12f, -0.91f), new Vector3(0.13f, 1.44f, 0.13f), 1);
                Part("Rear Post", PrimitiveType.Cube, transform, new Vector3(i * 1.53f, 2.12f, 0.91f), new Vector3(0.13f, 1.44f, 0.13f), 1);
                Part("Foot", PrimitiveType.Cylinder, transform, new Vector3(i * 1.27f, 0.03f, -0.71f), new Vector3(0.3f, 0.055f, 0.3f), 5);
                Part("Foot", PrimitiveType.Cylinder, transform, new Vector3(i * 1.27f, 0.03f, 0.71f), new Vector3(0.3f, 0.055f, 0.3f), 5);
                Part("X Guide Rail", PrimitiveType.Cube, transform, new Vector3(0, 1.53f, i * 0.53f), new Vector3(2.75f, 0.08f, 0.09f), 2);
            }
            Part("Top Beam", PrimitiveType.Cube, transform, new Vector3(0, 2.83f, 0), new Vector3(3.26f, 0.17f, 2), 1);
            Part("Rear Enclosure", PrimitiveType.Cube, transform, new Vector3(0, 2.05f, 0.975f), new Vector3(2.97f, 1.34f, 0.06f), 0);
            Part("Front Guard Lower Edge", PrimitiveType.Cube, transform, new Vector3(0, 1.44f, -0.96f), new Vector3(3.14f, 0.07f, 0.06f), 1);
            Part("Guard Handle", PrimitiveType.Cube, transform, new Vector3(1.24f, 2.06f, -0.97f), new Vector3(0.055f, 0.38f, 0.08f), 2);
            Stage = new GameObject("XY Stage Assembly").transform; Stage.SetParent(transform, false);
            Stage.localPosition = new Vector3(0, 1.64f, 0);
            Part("XY Stage", PrimitiveType.Cube, Stage, Vector3.zero, new Vector3(1.95f, 0.14f, 1.34f), 2);
            Part("Vacuum Chuck", PrimitiveType.Cylinder, Stage, new Vector3(0, 0.13f, 0), new Vector3(1.54f, 0.07f, 1.54f), 5);
            Part("Wafer Frame", PrimitiveType.Cylinder, Stage, new Vector3(0, 0.215f, 0), new Vector3(1.69f, 0.015f, 1.69f), 2);
            Part("Dicing Tape", PrimitiveType.Cylinder, Stage, new Vector3(0, 0.235f, 0), new Vector3(1.57f, 0.008f, 1.57f), 4);
            Part("Silicon Wafer", PrimitiveType.Cylinder, Stage, new Vector3(0, 0.251f, 0), new Vector3(1.37f, 0.008f, 1.37f), 3);
            for (int i = -5; i <= 5; i++)
            {
                float offset = i * 0.11f, length = 2 * Mathf.Sqrt(0.66f * 0.66f - offset * offset);
                Part("Dicing Lane X", PrimitiveType.Cube, Stage, new Vector3(0, 0.262f, offset), new Vector3(length, 0.001f, 0.004f), 5);
                Part("Dicing Lane Z", PrimitiveType.Cube, Stage, new Vector3(offset, 0.262f, 0), new Vector3(0.004f, 0.001f, length), 5);
            }
            Part("Spindle Support", PrimitiveType.Cube, transform, new Vector3(0.45f, 2.49f, 0.67f), new Vector3(0.93f, 0.55f, 0.53f), 2);
            Part("Spindle Housing", PrimitiveType.Cylinder, transform, new Vector3(0.45f, 2.22f, 0.1f), new Vector3(0.42f, 0.33f, 0.42f), 1).localRotation = Quaternion.Euler(90, 0, 0);
            Rotor = new GameObject("Spindle Rotor").transform; Rotor.SetParent(transform, false); Rotor.localPosition = new Vector3(0.45f, 2.22f, -0.23f);
            Part("Diamond Blade", PrimitiveType.Cylinder, Rotor, Vector3.zero, new Vector3(0.66f, 0.008f, 0.66f), 2).localRotation = Quaternion.Euler(90, 0, 0);
            Part("Blade Hub", PrimitiveType.Cylinder, Rotor, new Vector3(0, 0, -0.018f), new Vector3(0.2f, 0.022f, 0.2f), 5).localRotation = Quaternion.Euler(90, 0, 0);
            Part("Blade Rotation Indicator", PrimitiveType.Cube, Rotor, new Vector3(0.12f, 0, -0.027f), new Vector3(0.13f, 0.018f, 0.005f), 3);
            Part("Vision Camera", PrimitiveType.Cube, transform, new Vector3(-0.6f, 2.46f, -0.2f), new Vector3(0.28f, 0.37f, 0.27f), 5);
            Part("Vision Lens", PrimitiveType.Cylinder, transform, new Vector3(-0.6f, 2.22f, -0.2f), new Vector3(0.13f, 0.07f, 0.13f), 3);
            Part("Cooling Nozzle", PrimitiveType.Cylinder, transform, new Vector3(0.76f, 2.15f, -0.24f), new Vector3(0.065f, 0.17f, 0.065f), 4).localRotation = Quaternion.Euler(0, 0, -37);
            Part("Tower Pole", PrimitiveType.Cylinder, transform, new Vector3(1.24f, 3.01f, 0.52f), new Vector3(0.06f, 0.16f, 0.06f), 5);
            Lamps = new Renderer[3];
            for (int i = 0; i < Lamps.Length; i++) Lamps[i] = Part("Tower Light " + i, PrimitiveType.Cylinder, transform, new Vector3(1.24f, 3.2f + i * 0.13f, 0.52f), new Vector3(0.18f, 0.058f, 0.18f), 6).GetComponent<Renderer>();
            Part("HMI Arm", PrimitiveType.Cube, transform, new Vector3(1.94f, 1.78f, -0.25f), new Vector3(0.6f, 0.075f, 0.1f), 2);
            Part("Generic HMI", PrimitiveType.Cube, transform, new Vector3(2.19f, 2.16f, -0.25f), new Vector3(0.64f, 0.75f, 0.12f), 0);
            Part("HMI Screen", PrimitiveType.Cube, transform, new Vector3(2.19f, 2.18f, -0.32f), new Vector3(0.53f, 0.59f, 0.01f), 4);
            Part("Wafer Cassette Module", PrimitiveType.Cube, transform, new Vector3(-1.99f, 1.1f, -0.16f), new Vector3(0.61f, 1.2f, 1.1f), 0);
            for (int i = 0; i < 7; i++) Part("Cassette Shelf", PrimitiveType.Cube, transform, new Vector3(-1.99f, 1.0f + i * 0.07f, -0.73f), new Vector3(0.48f, 0.024f, 0.06f), 5);
            Part("Equipment Platform", PrimitiveType.Cube, transform, new Vector3(0, -0.1f, 0), new Vector3(6.5f, 0.1f, 4.5f), 5);

            var background = new GameObject("Background Camera", typeof(Camera)); background.transform.SetParent(transform, false);
            var backgroundCamera = background.GetComponent<Camera>(); backgroundCamera.clearFlags = CameraClearFlags.SolidColor; backgroundCamera.backgroundColor = UiFactory.Background; backgroundCamera.cullingMask = 0; backgroundCamera.depth = -10;
            CameraPivot = new GameObject("Machine Camera").transform; CameraPivot.SetParent(transform, false);
            MachineCamera = CameraPivot.gameObject.AddComponent<Camera>(); MachineCamera.clearFlags = CameraClearFlags.SolidColor; MachineCamera.backgroundColor = UiFactory.Background; MachineCamera.cullingMask = ~(1 << 5);
            MachineCamera.rect = new Rect(0.014f, 0.442f, 0.527f, 0.38f); MachineCamera.fieldOfView = 31; MachineCamera.nearClipPlane = 0.1f; MachineCamera.farClipPlane = 50;
            PositionCamera();
            var lightObject = new GameObject("Equipment Key Light", typeof(Light)); lightObject.transform.SetParent(transform, false); lightObject.transform.rotation = Quaternion.Euler(40, -35, 0);
            var light = lightObject.GetComponent<Light>(); light.type = LightType.Directional; light.intensity = 1.3f; light.color = new Color(0.91f, 0.96f, 1);
            var fill = new GameObject("Equipment Fill Light", typeof(Light)); fill.transform.SetParent(transform, false); fill.transform.rotation = Quaternion.Euler(25, 130, 0);
            var fillLight = fill.GetComponent<Light>(); fillLight.type = LightType.Directional; fillLight.intensity = 0.65f; fillLight.color = new Color(0.63f, 0.81f, 1);
        }

        private Transform Part(string name, PrimitiveType primitive, Transform parent, Vector3 position, Vector3 scale, int material)
        {
            var part = GameObject.CreatePrimitive(primitive); part.name = name; part.transform.SetParent(parent, false);
            part.transform.localPosition = position; part.transform.localScale = scale; part.GetComponent<Renderer>().sharedMaterial = Materials[material];
            var collider = part.GetComponent<Collider>(); collider.enabled = false;
            return part.transform;
        }
        private void Start()
        {
            lampInstances = new Material[Lamps.Length];
            for (int i = 0; i < Lamps.Length; i++) lampInstances[i] = Lamps[i].material;
        }
        private void Update()
        {
            if (Machine == null || Stage == null) return;
            if (Machine.MotionEnabled)
            {
                Travel += Time.deltaTime;
                Stage.localPosition = new Vector3(Mathf.Sin(Travel * 0.42f) * 0.33f, 1.64f, Mathf.Sin(Travel * 0.1f) * 0.12f);
                // Display rotation is deliberately slowed to avoid aliasing; RPM is shown numerically.
                Rotor.Rotate(Vector3.forward, Mathf.Min(Machine.Sensors.Data.Rpm, 30000) / 30000 * 680 * Time.deltaTime, Space.Self);
            }
            else if (Machine.State == MachineState.Idle && Machine.Elapsed < 0.3f)
            { Travel = 0; Stage.localPosition = new Vector3(0, 1.64f, 0); Rotor.localRotation = Quaternion.identity; yaw = -37; pitch = 18; distance = 8.6f; }
            RefreshIndicators();
            float scale = Mathf.Min(Screen.width / 1920f, Screen.height / 1080f);
            float fitWidth = 1920 * scale / Screen.width, fitHeight = 1080 * scale / Screen.height;
            MachineCamera.rect = new Rect((1 - fitWidth) * 0.5f + 0.014f * fitWidth, (1 - fitHeight) * 0.5f + 0.442f * fitHeight, 0.527f * fitWidth, 0.38f * fitHeight);
            var mouse = Mouse.current;
            if (mouse != null)
            {
                Vector2 point = mouse.position.ReadValue(); bool overModel = MachineCamera.pixelRect.Contains(point);
                if (mouse.leftButton.wasPressedThisFrame) dragging = overModel && !Machine.GetComponent<DashboardUI>().PmPanel.activeSelf;
                if (!mouse.leftButton.isPressed) dragging = false;
                if (dragging) { Vector2 delta = mouse.delta.ReadValue(); yaw += delta.x * 0.22f; pitch = Mathf.Clamp(pitch - delta.y * 0.18f, 7, 42); }
                if (overModel) distance = Mathf.Clamp(distance - mouse.scroll.ReadValue().y * 0.004f, 6.4f, 10.5f);
            }
            PositionCamera();
        }

        public void RefreshIndicators()
        {
            if (lampInstances != null)
            {
                int active = Machine.State == MachineState.Alarm ? 2 : Machine.State == MachineState.Warning || Machine.State == MachineState.Maintenance ? 1 : 0;
                for (int i = 0; i < Lamps.Length; i++)
                {
                    Color tint = i == active ? UiFactory.StateColor(Machine.State) : new Color(0.06f, 0.1f, 0.13f);
                    lampInstances[i].color = tint;
                }
            }
        }
        private void PositionCamera()
        {
            CameraPivot.localPosition = ViewTarget + Quaternion.Euler(pitch, yaw, 0) * new Vector3(0, 0, -distance);
            CameraPivot.LookAt(transform.TransformPoint(ViewTarget));
        }
        private void OnDestroy() { if (lampInstances != null) foreach (var material in lampInstances) Destroy(material); }
    }
}
