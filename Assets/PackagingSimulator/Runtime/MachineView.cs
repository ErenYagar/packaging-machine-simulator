using System.Collections.Generic;
using UnityEngine;

namespace PackagingSim
{
    [RequireComponent(typeof(MachineController))]
    public sealed class MachineView : MonoBehaviour
    {
        public Material surfaceMaterial;
        private MachineController controller;
        private Transform press;
        private Renderer beacon;
        private float cycle;
        private readonly List<Material> materials = new List<Material>();

        private void Start()
        {
            controller = GetComponent<MachineController>();
            var background = new GameObject("Background Camera", typeof(Camera));
            background.transform.SetParent(transform, false);
            var backgroundCamera = background.GetComponent<Camera>();
            backgroundCamera.depth = -1;
            backgroundCamera.cullingMask = 0;
            backgroundCamera.clearFlags = CameraClearFlags.SolidColor;
            backgroundCamera.backgroundColor = new Color(0.025f, 0.047f, 0.075f);
            var cameraObject = new GameObject("Machine Camera", typeof(Camera));
            cameraObject.transform.SetParent(transform, false);
            var camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.025f, 0.047f, 0.075f);
            camera.rect = new Rect(0.025f, 0.405f, 0.445f, 0.365f);
            camera.transform.position = new Vector3(5, 3.6f, -6);
            camera.transform.LookAt(new Vector3(0, 1.6f, 0));
            camera.orthographic = true;
            camera.orthographicSize = 2.1f;
            var light = new GameObject("Key Light", typeof(Light));
            light.transform.SetParent(transform, false);
            light.transform.rotation = Quaternion.Euler(45, -35, 0);
            light.GetComponent<Light>().type = LightType.Directional;
            light.GetComponent<Light>().intensity = 1.5f;
            RenderSettings.ambientLight = new Color(0.5f, 0.55f, 0.65f);
            Color body = new Color(0.62f, 0.70f, 0.75f);
            Color steel = new Color(0.18f, 0.27f, 0.34f);
            Part("Base", new Vector3(0, 0.55f, 0), new Vector3(3, 1.1f, 2), steel);
            Part("Work Table", new Vector3(0, 1.2f, 0), new Vector3(3.4f, 0.2f, 2.25f), body);
            Part("Left Column", new Vector3(-1.25f, 2, 0.4f), new Vector3(0.25f, 1.8f, 0.3f), body);
            Part("Right Column", new Vector3(1.25f, 2, 0.4f), new Vector3(0.25f, 1.8f, 0.3f), body);
            Part("Upper Beam", new Vector3(0, 2.85f, 0.4f), new Vector3(3, 0.4f, 0.9f), steel);
            press = Part("Press Head", new Vector3(0, 2.1f, 0), new Vector3(1.8f, 0.38f, 1.4f), body).transform;
            Part("Mold", new Vector3(0, 1.4f, 0), new Vector3(1.7f, 0.2f, 1.3f), new Color(0.14f, 0.6f, 0.54f));
            beacon = Part("Status Light", new Vector3(1.15f, 3.22f, 0.4f), new Vector3(0.2f, 0.32f, 0.2f), Color.green).GetComponent<Renderer>();
        }

        private GameObject Part(string name, Vector3 position, Vector3 scale, Color color)
        {
            var part = GameObject.CreatePrimitive(PrimitiveType.Cube);
            part.name = name;
            part.transform.SetParent(transform, false);
            part.transform.localPosition = position;
            part.transform.localScale = scale;
            var material = new Material(surfaceMaterial);
            material.color = color;
            materials.Add(material);
            part.GetComponent<Renderer>().sharedMaterial = material;
            return part;
        }

        private void Update()
        {
            if (press == null) return;
            var state = controller.Machine.State;
            if (state == MachineState.Running) cycle += Time.deltaTime * 2.8f;
            press.localPosition = new Vector3(0, 2.1f - 0.28f * (1f - Mathf.Cos(cycle)), 0);
            beacon.sharedMaterial.color = state == MachineState.Alarm ? Color.red :
                state == MachineState.Running ? Color.green : new Color(1f, 0.7f, 0.1f);
        }

        private void OnDestroy() { foreach (var material in materials) Destroy(material); }
    }
}
