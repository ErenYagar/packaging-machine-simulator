using System.Collections;
using System.IO;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace PackagingSim.Tests
{
    public sealed class DemoTests
    {
        private InputSettings.BackgroundBehavior previousBackgroundBehavior;
#if UNITY_EDITOR
        private InputSettings.EditorInputBehaviorInPlayMode previousEditorBehavior;
#endif

        [SetUp]
        public void RouteSyntheticInputToBackgroundGame()
        {
            previousBackgroundBehavior = InputSystem.settings.backgroundBehavior;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
            previousEditorBehavior = InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
        }

        [TearDown]
        public void RestoreInputSettings()
        {
            InputSystem.settings.backgroundBehavior = previousBackgroundBehavior;
#if UNITY_EDITOR
            InputSystem.settings.editorInputBehaviorInPlayMode = previousEditorBehavior;
#endif
        }

        [UnityTest]
        public IEnumerator ButtonsChecklistAndAlarmRecovery()
        {
            yield return SceneManager.LoadSceneAsync("PackagingDemo");
            yield return null;
            var controller = Object.FindObjectOfType<MachineController>();
            var ui = controller.GetComponent<PackagingUI>();
            Assert.That(EventSystem.current.GetComponent<InputSystemUIInputModule>(), Is.Not.Null);
            Assert.That(ui.AlarmPanel.activeSelf, Is.False);
            Capture(ui, "idle");
            ui.StartButton.onClick.Invoke();
            Assert.That(controller.Machine.State, Is.EqualTo(MachineState.Running));
            yield return null;
            Capture(ui, "running");
            ui.LowPressureButton.onClick.Invoke();
            yield return new WaitForSeconds(1.5f);
            Assert.That(ui.AlarmPanel.activeSelf, Is.True);
            Assert.That(ui.ResetButton.interactable, Is.False);
            Capture(ui, "alarm");
            var checklist = controller.GetComponent<MaintenanceChecklist>();
            foreach (var toggle in checklist.Toggles) toggle.isOn = true;
            Assert.That(checklist.CompletedCount, Is.EqualTo(4));
            Assert.That(controller.Machine.State, Is.EqualTo(MachineState.Alarm));
            foreach (var label in ui.GetComponentsInChildren<TMP_Text>())
            {
                label.ForceMeshUpdate();
                Assert.That(label.isTextOverflowing, Is.False, "Text exceeds its panel: " + label.text);
            }
            ui.RepairButton.onClick.Invoke();
            yield return new WaitForSeconds(2);
            Assert.That(controller.Machine.State, Is.EqualTo(MachineState.Alarm));
            Assert.That(ui.ResetButton.interactable, Is.True);
            Capture(ui, "recovered-awaiting-reset");
            ui.ResetButton.onClick.Invoke();
            yield return null;
            Assert.That(controller.Machine.State, Is.EqualTo(MachineState.Idle));
            Assert.That(ui.AlarmPanel.activeSelf, Is.False);
        }

        [UnityTest]
        public IEnumerator InputSystemKeyboardControlsMachine()
        {
            yield return SceneManager.LoadSceneAsync("PackagingDemo");
            yield return null;
            var keyboard = InputSystem.AddDevice<Keyboard>();
            var controller = Object.FindObjectOfType<MachineController>();
            try
            {
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.S));
                yield return null;
                yield return null;
                Assert.That(controller.Machine.State, Is.EqualTo(MachineState.Running));
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.X));
                yield return null;
                yield return null;
                Assert.That(controller.Machine.State, Is.EqualTo(MachineState.Idle));
            }
            finally { InputSystem.RemoveDevice(keyboard); }
        }

        [UnityTest]
        public IEnumerator InputSystemPointerClicksStartButton()
        {
            yield return SceneManager.LoadSceneAsync("PackagingDemo");
            yield return null;
            var mouse = InputSystem.AddDevice<Mouse>();
            try
            {
                var controller = Object.FindObjectOfType<MachineController>();
                var ui = controller.GetComponent<PackagingUI>();
                var rect = (RectTransform)ui.StartButton.transform;
                Vector2 point = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center));
                InputSystem.QueueStateEvent(mouse, new MouseState { position = point });
                yield return null;
                yield return null;
                InputSystem.QueueStateEvent(mouse, new MouseState { position = point }.WithButton(MouseButton.Left));
                yield return null;
                yield return null;
                InputSystem.QueueStateEvent(mouse, new MouseState { position = point });
                yield return null;
                yield return null;
                Assert.That(controller.Machine.State, Is.EqualTo(MachineState.Running));
            }
            finally { InputSystem.RemoveDevice(mouse); }
        }

        private static void Capture(PackagingUI ui, string name)
        {
            // Render the actual scene and TMP/uGUI canvas; no desktop window capture needed.
            var target = new RenderTexture(1600, 900, 24);
            target.Create();
            var previousTarget = RenderTexture.active;
            var uiCameraObject = new GameObject("Capture UI Camera", typeof(Camera));
            var uiCamera = uiCameraObject.GetComponent<Camera>();
            uiCamera.enabled = false;
            uiCamera.clearFlags = CameraClearFlags.Depth;
            uiCamera.cullingMask = 1 << 5;
            uiCamera.targetTexture = target;
            ui.Canvas.renderMode = RenderMode.ScreenSpaceCamera;
            ui.Canvas.worldCamera = uiCamera;
            ui.Canvas.planeDistance = 10;
            // Canvas root and generated children are on UI layer for the capture camera.
            foreach (var child in ui.Canvas.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = 5;
            Canvas.ForceUpdateCanvases();
            RenderTexture.active = target;
            GL.Clear(true, true, new Color(0.025f, 0.047f, 0.075f));
            var machineCamera = GameObject.Find("Machine Camera").GetComponent<Camera>();
            machineCamera.cullingMask = ~(1 << 5);
            Rect viewport = machineCamera.rect;
            var machineTarget = new RenderTexture(Mathf.RoundToInt(1600 * viewport.width), Mathf.RoundToInt(900 * viewport.height), 24);
            machineTarget.Create();
            machineCamera.targetTexture = machineTarget;
            machineCamera.rect = new Rect(0, 0, 1, 1);
            machineCamera.Render();
            machineCamera.targetTexture = null;
            machineCamera.rect = viewport;
            RenderTexture.active = target;
            GL.PushMatrix();
            GL.LoadPixelMatrix(0, 1600, 900, 0);
            Graphics.DrawTexture(new Rect(viewport.x * 1600, (1 - viewport.yMax) * 900,
                viewport.width * 1600, viewport.height * 900), machineTarget);
            GL.PopMatrix();
            uiCamera.Render();
            var pixels = new Texture2D(1600, 900, TextureFormat.RGB24, false);
            pixels.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0);
            pixels.Apply();
            string directory = Path.GetFullPath(Path.Combine(Application.dataPath, "../TestResults/Previews"));
            Directory.CreateDirectory(directory);
            File.WriteAllBytes(Path.Combine(directory, name + ".png"), pixels.EncodeToPNG());
            ui.Canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            ui.Canvas.worldCamera = null;
            machineCamera.targetTexture = null;
            RenderTexture.active = previousTarget;
            Object.Destroy(pixels);
            Object.Destroy(uiCameraObject);
            target.Release();
            Object.Destroy(target);
            machineTarget.Release();
            Object.Destroy(machineTarget);
        }
    }
}
