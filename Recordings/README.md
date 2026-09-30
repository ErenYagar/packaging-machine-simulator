# Wafer Saw Digital Twin — Demo Recording

[觀看／下載 MP4](https://github.com/ErenYagar/packaging-machine-simulator/raw/refs/heads/main/Recordings/WaferSaw-DigitalTwin-Demo-1080p.mp4)

這是 Unity **6000.3.0f1** 隔離錄影副本的實際模擬器畫面。錄影包含 3 秒 Idle、約 94 秒自動展示及 5 秒復原畫面，沒有音軌。

| 項目 | 規格 |
|---|---|
| 長度 | 102 秒（1 分 42 秒） |
| 解析度 | 1920×1080 |
| 幀率 | 30 fps |
| 編碼 | H.264 / yuv420p / MP4 |
| 解碼驗證 | 3,060 幀完整通過 |

展示流程：正常運轉 → 注入主軸振動故障 → Warning → Alarm E203 → 停機 → 檢查告警與趨勢 → 檢查並更換刀片 → 校正 → Test wafer → 恢復 Running。

[事件紀錄](event-log.txt) 直接取自這次模擬 session；14:03:00 是合成起始時間。畫面 KPI 由該次模擬累計，定義見 [專案說明](../Assets/Documentation/README.md)。

重現展示：在 Unity 開啟專案，選 **Tools → Build Wafer Saw Digital Twin**，按 **Play**，再按 **DEMO MODE (AUTO)**。錄影用程式、快取與本機診斷日誌不包含在公開專案內。
