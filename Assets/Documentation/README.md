# Semiconductor Equipment Digital Twin

## Purpose

此 MVP 模擬半導體 Wafer Saw 設備工程師的工作：

**Monitor → Detect → Diagnose → Maintain → Verify → Recover**

它是 Equipment Engineering Simulation Demo，不是遊戲。機台由 Unity Primitives 組成；資料、故障、日誌時間和 KPI 都是合成示範。左側約 55% 為機台與操作流程，右側約 45% 為設備狀態、Sensor、Trend、KPI 和 Event Log。

新增內容位於目前的 `PackagingMachineSimulator`，命名空間為 `WaferSaw`。原本的 `PackagingSim` 封膠壓機場景保留。

## How to Run

來源專案版本維持 **Unity 2022.3.62f1**，Built-in Render Pipeline；原套件版本保持不變。新增功能使用 Unity uGUI Text、Input System 和 Standard shader，不需要 TMP Essential Resources、外部模型、第三方圖表或網路 API。

1. 用 Unity Hub 開啟 `PackagingMachineSimulator`，選用 **2022.3.62f1**。初次開啟等待現有套件解析及 C# 編譯。
2. 選 **Tools → Build Wafer Saw Digital Twin**。
3. 建立器產生並開啟 `Assets/Scenes/WaferSawDigitalTwin.unity`，自動建立 Camera、Light、Primitive Machine、Canvas、Buttons、Dropdown、PM Modal、Scroll View 和圖表，連接全部 references，並加入 Build Settings。
4. 按 **Play**，初始狀態為 **IDLE**；按 **START MACHINE** 開始。

隨附場景是版本相容的入口場景：直接開啟並 Play 也會由 `WaferSawBootstrap` 建立全部物件。使用 Tools 選單則會把完整物件存入場景，方便在 Editor 檢視。不需要手動 Drag & Drop reference。

**Tools 建立器會重建這個專用示範場景。** 若已自行修改其物件，請先另存場景，再執行建立器。

推薦 Game View 選 **1920×1080 或 1600×900（16:9）**。在左側機台區按住滑鼠左鍵拖曳可旋轉視角，滾輪縮放；相機有俯仰與距離限制。

## Demo Flow

手動完整示範約 1–3 分鐘：

1. **START MACHINE**：觀察 spindle、blade 與 XY stage 動作、綠色塔燈、平滑 Sensor 數值。
2. Fault Selector 選 **Spindle Vibration**，按 **INJECT FAULT**。
3. 觀察 vibration 曲線逐步上升，先 **WARNING**，再 **ALARM E203**。機台 spindle、stage 與 coolant 停止，塔燈紅色。
4. **DIAGNOSE**（步驟 1：Check Alarm History）。
5. 點 **2. View Sensor Trend**，檢視告警前的上升曲線。
6. 點 **3. Inspect Blade**。
7. **MAINTENANCE** 或 **4. Replace Blade**。狀態變橙色 MAINTENANCE，記錄模擬隔離與 blade replacement。
8. 點 **5. Calibrate / Check Setup**。
9. **VERIFY** 或 **6. Verification Run**。藍色 VERIFYING，執行 8 秒 test wafer；全部通道須連續至少 2.5 秒處於正常範圍。
10. 成功才變 **RUNNING**、塔燈綠色、Alarm cleared；觀察 Event Log、MTTR、Downtime、OEE 更新。

按 **DEMO MODE (AUTO)** 會開啟新 session，自動執行上述流程，約 **94 秒**結束。Fault 在第 12 秒注入，38 秒起進行排故，74 秒執行驗證，約 82 秒恢復，94 秒完成 KPI 展示。自動示範時手動排故與故障注入停用；**STOP** 可取消自動化。

**RESET DEMO** 是整個教育模擬 session 重開：清除 fault、alarm、PM、trend、KPI、event log，回到 Idle。它不是生產機台的 alarm reset。正常故障流程無單鍵清警報／直接 restart 捷徑；停止未排除的故障或中斷驗證也會鎖定 Alarm。

錯誤順序會顯示並記錄 **Diagnostic Step Incorrect**，不會前進或 crash。三種情境共用六步流程，但零件與維修動作隨故障改變。

### Fault scenarios

| Fault | Warning | Alarm | Suggested inspection and simulated repair |
|---|---|---|---|
| Spindle Vibration / E203 | ≥ 2.5 mm/s | ≥ 4.5 mm/s | Inspect Blade → Replace Blade → Calibrate → Test Run |
| Vacuum Low / E117 | ≥ -65 kPa（真空絕對值變小） | ≥ -50 kPa | View Vacuum Trend → Inspect Chuck → Clean Chuck → Check Setup → Verify Vacuum |
| Cooling Flow Low / E305 | ≤ 1.2 L/min | ≤ 0.8 L/min | Check Flow → Inspect Cooling Line → Clean Filter → Check Setup → Verify Flow |

故障以約 18 秒逐步發展，Sensor 以 PerlinNoise 加指數低通平滑；UI 5 Hz 更新。Cooling 故障同時使溫度緩慢升高，維護後以熱慣性模型下降。Blade Wear 在更換後回到 0%，production 中逐步累積。

Alarm 顯示 code、description、**現在值**、**trip 當下值**、normal range 和 possible causes。停止後 vibration 會下降、coolant 為 0；故障原因仍保留，不能因此清除 Alarm。Possible causes 是診斷假設，inspection 後以 test wafer 驗證處置有效。

### Trend and safety semantics

- 保存最近 **120 samples × 0.2 s = 24 秒**。告警時凍結獨立快照，保留故障前惡化過程；開始 Verification 才切回 live trend。
- Vibration、Vacuum 圖的黃色／紅色虛線對應告警門檻。Temperature 的 45／55 °C 線是**觀察參考線**，沒有另外生成第四種溫度告警；Verification 要求 35–45 °C。
- 機台模型為剖面示意：運轉視為防護罩關閉。Maintenance 只在 stopped/zero-speed 狀態執行，進入 Verify 前記錄 guard closed。
- 這不是實際維修操作指導，沒有模擬真實 PLC、實體能源隔離、安全迴路、校正量測或 wafer 良率檢驗。幾何模型中的刀片旋轉速度刻意縮小以避免畫面 aliasing；實際模擬 RPM 由 Sensor 數字呈現。
- PM Checklist 只在 stopped 狀態可勾選；六項完成記錄 **PM COMPLETED**。PM 不會修復注入故障，也不會清 Alarm。
- Event Log 是可捲動的 Scroll View，白／黃／紅／橙／綠區分事件，新事件自動捲到底；最多保留 250 筆。14:03:00 為合成 session 起點，不是系統時鐘。

## Architecture

| File | Responsibility |
|---|---|
| `Scripts/Core/MachineState.cs` | Idle / Running / Warning / Alarm / Maintenance / Verifying 與日誌級別 |
| `Scripts/Core/MachineController.cs` | 狀態轉移、操作聯鎖、時間推進、trip 快照、自動示範與 reset |
| `Scripts/Sensors/SensorData.cs` | 七個 Sensor 欄位及 verification normal-range 判定 |
| `Scripts/Sensors/SensorSimulator.cs` | 平滑資料、故障影響、熱慣性、停止與換刀 |
| `Scripts/Faults/FaultScenario.cs` | 三種合成故障的 code、range、causes、component 和 action |
| `Scripts/Faults/FaultManager.cs` | 故障強度漸升與維修後的恢復目標 |
| `Scripts/Alarm/AlarmData.cs` | 保留 trip 時間和感測值 |
| `Scripts/Alarm/AlarmManager.cs` | Warning / Alarm 門檻及 latched alarm |
| `Scripts/Maintenance/TroubleshootingManager.cs` | 六步有序診斷，拒絕跳步 |
| `Scripts/Maintenance/PMManager.cs` | 六項 preventive-maintenance checklist |
| `Scripts/Data/EventLogger.cs` | 有上限的彩色事件資料與合成時間戳 |
| `Scripts/Data/KPIManager.cs` | Uptime / Downtime / Availability / OEE / MTTR / Alarms / Wafers |
| `Scripts/UI/DashboardUI.cs` | 自動建立 Dashboard、按鈕、Dropdown、PM Panel、事件綁定與 5 Hz 更新 |
| `Scripts/UI/TrendGraph.cs` | 用 uGUI VertexHelper 繪製歷史曲線與參考線，無外部 Chart Asset |
| `Scripts/UI/EventLogUI.cs` | Scroll View、顏色與 auto-scroll |
| `Scripts/UI/UiFactory.cs` | 小型共用 uGUI 建立方法與狀態色 |
| `Scripts/Runtime/MachineView.cs` | Primitive 設備、動畫、tower light、orbit camera |
| `Scripts/Runtime/WaferSawBootstrap.cs` | 入口場景在 Play 時建立全部物件，已建好的場景不重複建立 |
| `Editor/WaferSawSceneBuilder.cs` | Tools 選單、批次建立、材質保存、scene references 與 Build Settings |
| `Tests/EditMode/WaferSawTests.cs` | 故障、順序、驗證失敗、PM、reset、KPI、noise、demo-mode 邏輯測試 |
| `Tests/PlayMode/WaferSawPlayTests.cs` | 真實場景／按鈕／Input System pointer／動畫停止／UI clipping／bootstrap 測試及畫面擷取 |

## Engineering Concepts

**State Machine / Alarm Management:** Idle → Running → Warning → Alarm → Maintenance → Verifying → Running；Verification 失敗回 Alarm 並重新要求診斷。Alarm 不由單次好轉讀值自動解除。

**Sensor Monitoring / Fault Injection:** 合成參數以平滑模型惡化；只有 injection selector 的 Random Fault 在按下時選一次隨機情境，沒有每 frame `Random.Range` 跳值。

**Troubleshooting / Preventive Maintenance:** corrective troubleshooting 與 PM checklist 分開，故障修好仍須驗證；單純勾 PM 不代表復歸。

**OEE / MTTR / Downtime:**

- Planned Production Time：第一次 Start 到目前的模擬秒數，含 stop 與故障時間；初始 Idle 不計。
- Uptime：Running + Warning；Warning 仍有生產，因此計 operating time。
- Downtime：Planned Time − Uptime，含中途 Idle、Alarm、Maintenance、Verifying。
- Availability = Uptime / Planned Production Time。
- Demo OEE = Availability × **Performance 0.95** × **Quality 0.99**。兩個固定係數在 Dashboard 標示。
- MTTR = 已完成故障事件（首次 Alarm 至成功 Recovery）的平均秒數。驗證失敗不結案、不重開事件計時；Total Alarms 則計每次重新進入 Alarm。
- 尚無完成 repair 時 MTTR 顯示 `--`。
- Wafer 每 10 秒生產時間累積一片；Verification 使用 test wafer，不計入 production wafers。

**此 KPI 為教育展示用途的簡化模型。** 它不代表工廠 MES、真實生產績效或公司內部資料。

## Verification

本機只有 Unity **6000.3.0f1**，所以實際編譯與 Play Mode 在 `TestResults/WaferSawValidation` 隔離副本執行。來源專案的 `ProjectVersion.txt` 和 `Packages/manifest.json` 保留原值；沒有將 Unity 6 場景格式或升級後的套件設定覆寫回来源專案。

測試結果與限制記錄於 [Verification.md](Verification.md)。**Unity 6 的通過結果不能視為 Unity 2022.3.62f1 已實測通過。** 用目標版本開啟後，仍請執行 Tools 建場景與 Play，或透過 Test Runner 跑 WaferSaw 的 EditMode／PlayMode assemblies。

重現測試使用專案 `Tools/Verify-WaferSaw.ps1`。請傳入對應 Editor 路徑；不同版本驗證使用 `-UseIsolatedCopy`，不升級來源專案。

## Disclaimer

This project is an educational simulation.

It does not reproduce any proprietary PTI, DISCO, or semiconductor manufacturer's machine design, software, process recipe, alarm code, or confidential production parameter.

All machine parameters and alarm scenarios are synthetic demonstration data.

## Technical references

- [Unity uGUI Graphic implementation](https://github.com/Unity-Technologies/uGUI/blob/main/com.unity.ugui/Runtime/UGUI/UI/Core/Graphic.cs): custom mesh-based UI rendering.
- [Unity command-line test execution](https://docs.unity.com/en-us/engine/6000.6/manual/scripting/test-framework-introduction/running-tests/run-tests-from-command-line): batch test runner parameters; verification here uses the locally installed Editor version stated above.
