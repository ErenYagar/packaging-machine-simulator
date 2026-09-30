# Wafer Saw MVP — Verification

驗證日期：2026-09-30。此紀錄僅適用新增的 `WaferSaw` 模組。

| Check | Result |
|---|---|
| Unity C# compilation + full scene generation | PASS |
| WaferSaw EditMode | **12 / 12 PASS** |
| WaferSaw PlayMode | **4 / 4 PASS** |
| New WaferSaw C# compiler errors / warnings | **0 / 0** |
| Three faults: Warning → Alarm → ordered repair → Verification → Running | PASS |
| Wrong-order actions, start/stop bypass attempts, failed verification | PASS |
| Frozen pre-alarm trend retained during long diagnosis | PASS |
| PM safe-stop interlock, completion log, reset | PASS |
| Runtime model motion and stopping at Alarm | PASS |
| Synthetic Input System mouse through actual EventSystem / raycaster | PASS |
| Runtime bootstrap with no manually wired references | PASS |
| Auto Demo recovery and KPI completion within 94–96 simulated seconds | PASS |
| 1920×1080 actual Unity rendering, visible text height and graph mesh checks | PASS |
| Unity 2022.3.62f1 execution | **Not tested — Editor is not installed locally** |
| Standalone executable build | Not performed; deliverable is the Unity project |

## Environment and scope

來源專案仍為 **2022.3.62f1**，Input System **1.7.0**、uGUI **1.0.0**、TMP **3.0.9**、Test Framework **1.1.33**。`ProjectSettings/ProjectVersion.txt` 與 `Packages/manifest.json` 的 SHA-256 已核對未改。

實際測試使用本機 **Unity 6000.3.0f1**、Direct3D 11、隔離副本 `TestResults/WaferSawValidation`，套件沿用本機已有 Unity 6 驗證環境的 Input System **1.16.0**、uGUI **2.0.0**、Test Framework **1.6.0**。只在副本生成 Unity 6 完整序列化場景；沒有把該場景格式或升級設定寫回 2022 來源。

來源隨附的簡潔入口場景與完整 Editor-built 場景走相同 Build methods。PlayMode 分別測試已儲存場景與 runtime bootstrap。Unity 2022 下仍須由使用者開啟該版本，執行 Tools 建場景、Play 與 Test Runner；Unity 6 通過不能當作 2022 已驗證。

第一次完整編譯發現原有 `PackagingSim` 的 **4 處 CS0618**：`PackagingUI.cs` 的 `TMP_Text.enableWordWrapping`，以及原 `DemoTests.cs` 三處 `FindObjectOfType<T>()`。它們屬於既有示範在 Unity 6 的過時 API 警告，與新增 WaferSaw 無關；本次未更動原程式。

測試使用模擬時間加速檢查邏輯；正常 Play 使用 `Time.deltaTime`。真實滑鼠路徑測試透過合成 Input System 裝置，不移動桌面滑鼠；它驗證 EventSystem 點擊，不等於所有實體輸入裝置和解析度都已人工測試。

## Fixed during verification

- 自製 TrendGraph 明確要求 CanvasRenderer 並使用 VertexHelper mesh generation；新增 rendered-mesh assertion，確認曲線確實出現在畫面。
- Canvas 改採 Expand 縮放，非 16:9 Game View 也保留全部控制項；機台 camera viewport 跟隨置中 16:9 版面。
- 高解析離屏截圖重建文字 mesh，避免沿用小型測試視窗的字型取樣。
- 塔燈採 Unlit 材質保留可辨識狀態色。
- Verification 起始事件使用「requested」，通過後才記錄 PASS；步驟 6 在驗證中顯示 RUN。

## Saved evidence

- [EditMode XML](../../Documentation/WaferSaw/Results/EditMode.xml)
- [PlayMode XML](../../Documentation/WaferSaw/Results/PlayMode.xml)
- [Validation summary](../../Documentation/WaferSaw/Results/summary.json)
- [Idle screenshot](../../Documentation/WaferSaw/Previews/01-idle.png)
- [Warning screenshot](../../Documentation/WaferSaw/Previews/03-warning.png)
- [Alarm screenshot](../../Documentation/WaferSaw/Previews/04-alarm.png)
- [PM screenshot](../../Documentation/WaferSaw/Previews/06-pm.png)
- [Verifying screenshot](../../Documentation/WaferSaw/Previews/07-verifying.png)
- [Recovered screenshot](../../Documentation/WaferSaw/Previews/08-recovered.png)

這些圖片是實際 Unity Camera 與 uGUI 渲染，不是 AI 設計稿。
