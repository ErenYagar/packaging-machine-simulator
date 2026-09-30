using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace WaferSaw.Tests
{
    public sealed class WaferSawPlayTests
    {
        private MachineController machine;
        private DashboardUI ui;
        private void Tick(float seconds) { for (int i = 0; i < Mathf.CeilToInt(seconds / 0.1f); i++) machine.Advance(0.1f); ui.Refresh(); }
        private IEnumerator Load()
        {
            yield return SceneManager.LoadSceneAsync("WaferSawDigitalTwin"); yield return null;
            machine = GameObject.Find("Wafer Saw Digital Twin").GetComponent<MachineController>();
            ui = machine.GetComponent<DashboardUI>(); machine.AutomaticUpdates = false; ui.Refresh();
        }

        [UnityTest] public IEnumerator DashboardButtonsMotionAndRecovery()
        {
            yield return Load(); Assert.That(machine.State, Is.EqualTo(MachineState.Idle)); Capture("01-idle");
            ui.Controls[0].onClick.Invoke(); Tick(5); yield return null; Capture("02-running");
            var view = machine.GetComponent<MachineView>(); var position = view.Stage.localPosition;
            yield return new WaitForSeconds(0.15f); Assert.That(view.Stage.localPosition, Is.Not.EqualTo(position));
            ui.FaultSelector.value = 1; ui.Controls[2].onClick.Invoke(); Tick(10); Assert.That(machine.State, Is.EqualTo(MachineState.Warning)); Capture("03-warning"); Tick(13);
            Assert.That(machine.State, Is.EqualTo(MachineState.Alarm)); Capture("04-alarm");
            var rotor = view.Rotor.localRotation; position = view.Stage.localPosition;
            yield return new WaitForSeconds(0.15f); Assert.That(view.Rotor.localRotation, Is.EqualTo(rotor)); Assert.That(view.Stage.localPosition, Is.EqualTo(position));
            ui.Controls[5].onClick.Invoke(); Assert.That(machine.Notice, Is.EqualTo("Diagnostic Step Incorrect"));
            ui.Controls[3].onClick.Invoke(); ui.Steps[1].onClick.Invoke(); ui.Steps[2].onClick.Invoke(); ui.Controls[4].onClick.Invoke();
            ui.Refresh(); Capture("05-maintenance"); ui.Controls[6].onClick.Invoke();
            foreach (var toggle in ui.PmChecks) toggle.isOn = true;
            Assert.That(machine.PM.Completed, Is.True); Capture("06-pm"); ui.ClosePm.onClick.Invoke();
            ui.Steps[4].onClick.Invoke(); Tick(2); ui.Controls[5].onClick.Invoke(); Tick(3); Capture("07-verifying"); Tick(6);
            Assert.That(machine.State, Is.EqualTo(MachineState.Running)); Capture("08-recovered"); AssertTextFits();
            ui.Controls[7].onClick.Invoke(); Assert.That(machine.State, Is.EqualTo(MachineState.Idle)); Assert.That(machine.Kpi.TotalAlarms, Is.Zero);
            Assert.That(ui.Graphs[0].SampleCount, Is.Zero); Assert.That(machine.PM.Count, Is.Zero);
            ui.Controls[8].onClick.Invoke(); Tick(96); Assert.That(machine.State, Is.EqualTo(MachineState.Running)); Assert.That(machine.DemoActive, Is.False);
        }

        [UnityTest] public IEnumerator OtherFaultSelectorsRecoverAndReset()
        {
            yield return Load();
            for (int fault = 2; fault <= 3; fault++)
            {
                ui.Controls[7].onClick.Invoke(); ui.Controls[0].onClick.Invoke(); Tick(3); ui.FaultSelector.value = fault;
                ui.Controls[2].onClick.Invoke(); Tick(24); Assert.That(machine.State, Is.EqualTo(MachineState.Alarm));
                for (int i = 0; i < 6; i++) { ui.Steps[i].onClick.Invoke(); Tick(2); }
                Tick(8); Assert.That(machine.State, Is.EqualTo(MachineState.Running));
            }
        }

        [UnityTest] public IEnumerator ActualPointerCanStartMachine()
        {
            yield return Load();
            var prior = InputSystem.settings.backgroundBehavior;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
            var editorPrior = InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
            var mouse = InputSystem.AddDevice<Mouse>();
            try
            {
                var rect = (RectTransform)ui.Controls[0].transform;
                Canvas.ForceUpdateCanvases();
                Vector2 point = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center));
                Assert.That(point.x, Is.InRange(0, Screen.width)); Assert.That(point.y, Is.InRange(0, Screen.height));
                InputSystem.QueueStateEvent(mouse, new MouseState { position = point }); yield return null; yield return null;
                InputSystem.QueueStateEvent(mouse, new MouseState { position = point }.WithButton(MouseButton.Left)); yield return null; yield return null;
                InputSystem.QueueStateEvent(mouse, new MouseState { position = point }); yield return null; yield return null;
                Assert.That(machine.State, Is.EqualTo(MachineState.Running));
            }
            finally
            {
                InputSystem.RemoveDevice(mouse); InputSystem.settings.backgroundBehavior = prior;
#if UNITY_EDITOR
                InputSystem.settings.editorInputBehaviorInPlayMode = editorPrior;
#endif
            }
        }

        [UnityTest] public IEnumerator StarterSceneBootstrapCreatesAllReferencesWithoutDragging()
        {
            yield return Load(); Object.Destroy(machine.gameObject); yield return null;
            var root = new GameObject("Wafer Saw Digital Twin"); root.AddComponent<WaferSawBootstrap>(); yield return null;
            machine = root.GetComponent<MachineController>(); ui = root.GetComponent<DashboardUI>();
            Assert.That(machine.State, Is.EqualTo(MachineState.Idle)); Assert.That(ui.Controls.Length, Is.EqualTo(9));
            Assert.That(root.GetComponent<MachineView>().Stage, Is.Not.Null); ui.Controls[0].onClick.Invoke();
            Assert.That(machine.State, Is.EqualTo(MachineState.Running));
        }

        private void AssertTextFits()
        {
            foreach (var text in ui.Canvas.GetComponentsInChildren<Text>())
            {
                if (text == ui.EventLog.Text || !text.gameObject.activeInHierarchy) continue;
                Assert.That(text.preferredHeight, Is.LessThanOrEqualTo(text.rectTransform.rect.height + 1), "Text clipped: " + text.name + " = " + text.text);
            }
        }

        private void Capture(string name)
        {
            ui.Refresh(); machine.GetComponent<MachineView>().RefreshIndicators();
            var target = new RenderTexture(1920, 1080, 24); target.Create(); var previous = RenderTexture.active;
            var cameraObject = new GameObject("UI Capture", typeof(Camera)); var camera = cameraObject.GetComponent<Camera>(); camera.enabled = false;
            camera.clearFlags = CameraClearFlags.Depth; camera.cullingMask = 1 << 5; camera.targetTexture = target;
            ui.Canvas.renderMode = RenderMode.ScreenSpaceCamera; ui.Canvas.worldCamera = camera; ui.Canvas.planeDistance = 10;
            var scaler = ui.Canvas.GetComponent<CanvasScaler>(); scaler.enabled = false; ui.Canvas.scaleFactor = 1;
            foreach (var graphic in ui.Canvas.GetComponentsInChildren<Graphic>()) graphic.SetAllDirty();
            Canvas.ForceUpdateCanvases(); RenderTexture.active = target; GL.Clear(true, true, UiFactory.Background);
            foreach (var graph in ui.Graphs)
            {
                var mesh = graph.canvasRenderer.GetMesh();
                Assert.That(mesh, Is.Not.Null, "Graph mesh is missing: " + graph.name);
                Assert.That(mesh.vertexCount, Is.GreaterThan(16), "Graph has no rendered geometry: " + graph.name);
            }
            var modelCamera = machine.GetComponent<MachineView>().MachineCamera; Rect savedViewport = modelCamera.rect;
            Rect viewport = new Rect(0.014f, 0.442f, 0.527f, 0.38f);
            var modelTarget = new RenderTexture(Mathf.RoundToInt(1920 * viewport.width), Mathf.RoundToInt(1080 * viewport.height), 24); modelTarget.Create();
            modelCamera.targetTexture = modelTarget; modelCamera.rect = new Rect(0, 0, 1, 1); modelCamera.Render(); modelCamera.targetTexture = null; modelCamera.rect = viewport;
            RenderTexture.active = target; GL.PushMatrix(); GL.LoadPixelMatrix(0, 1920, 1080, 0);
            Graphics.DrawTexture(new Rect(viewport.x * 1920, (1 - viewport.yMax) * 1080, viewport.width * 1920, viewport.height * 1080), modelTarget); GL.PopMatrix(); camera.Render();
            RenderTexture.active = target; var pixels = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
            pixels.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0); pixels.Apply();
            string directory = Path.GetFullPath(Path.Combine(Application.dataPath, "../TestResults/WaferSawPreviews")); Directory.CreateDirectory(directory);
            File.WriteAllBytes(Path.Combine(directory, name + ".png"), pixels.EncodeToPNG());
            AssertTextFits();
            ui.Canvas.renderMode = RenderMode.ScreenSpaceOverlay; ui.Canvas.worldCamera = null; scaler.enabled = true; modelCamera.rect = savedViewport; RenderTexture.active = previous;
            Object.Destroy(pixels); Object.Destroy(cameraObject); target.Release(); Object.Destroy(target); modelTarget.Release(); Object.Destroy(modelTarget);
        }
    }
}
