# TimelinePlayer

一個將 Unity Timeline 轉換成可在 Runtime 序列化播放格式的工具。作者在 Editor 用熟悉的 Timeline 編輯動作時序，系統自動匯出成 `TimelineSequenceData` ScriptableObject，再由 `SequencePlayer` 於執行期驅動。

---

## 目錄結構

```
Assets/TimelinePlayer/
├── Player/          執行期播放引擎
│   └── SequencePlayer.cs
├── ReferenceHub/    繫結容器（依賴注入）
│   ├── ReferenceHub.cs
│   └── ReferenceEntry.cs
├── Timeline/        Timeline 整合層
│   ├── ActionClips/     動作邏輯基底與範例
│   ├── Data/            匯出後的資料模型
│   └── Track/           自訂 Track / Clip / Behaviour
└── Editor/          Editor 工具
    ├── Exporter/        TimelineAutoSync、SequenceExporter
    └── Inspector/       Inspector 擴充與 Drawer
```

---

## 資料流

```
.playable (Unity Timeline)
      │
      ▼  Editor：存檔時自動觸發
TimelineAutoSync  →  SequenceExporter.BuildSequenceData()
      │
      ▼
MyCutscene_SequenceData.asset  (TimelineSequenceData)
      │
      ▼  Runtime：SequencePlayer.Play()
OnEnter → OnUpdate(normalizedTime) → OnExit / OnCancel
```

- **命名對應**：`MyCutscene.playable` ↔ `MyCutscene_SequenceData.asset`（同目錄）
- **重複存檔**：用 `SourceTimelineGuid` 定位既有 SO，in-place 覆寫，不會打壞場景引用
- **自動綁定**：儲存時，所有使用該 timeline 的 `PlayableDirector` 會自動取得 / 新增 `SequencePlayer` 並填入 `_sequenceData`
- **過濾條件**：只處理包含至少一個 `TimelineActionTrack` 的 timeline

---

## 快速開始

1. 建立 `.playable` timeline，新增一條 `TimelineActionTrack`，放入 clip。
2. 在 clip 的 Inspector 用型別下拉選擇要用的 `ActionClip`（例如 `GameObjectActiveAction`），填寫欄位。
3. 儲存 timeline — `TimelineAutoSync` 會在旁邊產生 `*_SequenceData.asset`。
4. 場景上的 `PlayableDirector` GameObject 會自動掛上 `SequencePlayer` 並填好資料。
5. 指派 `ReferenceHub`（或用 `_overrideBindings` 逐 track 覆蓋），呼叫 `player.Play()`。

---

## ActionClip 擴展指引

這是最常見的擴充點 — 當你要加一個新動作（淡入、震動、觸發音效、呼叫某 System…），就寫一個新的 `ActionClip` 子類。

### Step 1 — 繼承 `ActionClip`

基底在 [ActionClip.cs](Assets/TimelinePlayer/Timeline/ActionClips/ActionClip.cs)：

```csharp
public abstract class ActionClip
{
    public abstract void OnEnter(ReferenceHub hub);
    public abstract void OnUpdate(ReferenceHub hub, float normalizedTime);
    public abstract void OnExit(ReferenceHub hub);
    public abstract void OnCancel(ReferenceHub hub);
}
```

| 生命週期 | 何時呼叫 | 典型用途 |
| --- | --- | --- |
| `OnEnter` | clip 起始幀（進入範圍時一次） | 讀取 hub 綁定、快取引用、記下原始狀態 |
| `OnUpdate` | 每幀（`normalizedTime` 0 → 1） | 補間、取樣曲線、驅動動態屬性 |
| `OnExit` | clip 自然結束 | 還原或收尾 |
| `OnCancel` | Play 中途被 `Stop()` | 回到 `OnEnter` 之前狀態（rollback） |

### Step 2 — 寫一個具體 Action

```csharp
using System;
using UnityEngine;

namespace TimelinePlayer.Actions
{
    [Serializable]
    public class MyFadeAction : ActionClip
    {
        [Tooltip("MonoBehaviourEntry Key，綁到場景上的 CanvasGroup 物件")]
        public string TargetKey;

        public float FromAlpha = 0f;
        public float ToAlpha   = 1f;

        [NonSerialized] private CanvasGroup _cg;
        [NonSerialized] private float       _originalAlpha;

        public override void OnEnter(ReferenceHub hub)
        {
            var entry = hub?.GetEntry<MonoBehaviourEntry>(TargetKey);
            _cg = entry?.Get<CanvasGroup>();
            if (_cg != null) _originalAlpha = _cg.alpha;
        }

        public override void OnUpdate(ReferenceHub hub, float normalizedTime)
        {
            if (_cg != null)
                _cg.alpha = Mathf.Lerp(FromAlpha, ToAlpha, normalizedTime);
        }

        public override void OnExit(ReferenceHub hub)
        {
            if (_cg != null) _cg.alpha = ToAlpha;
        }

        public override void OnCancel(ReferenceHub hub)
        {
            if (_cg != null) _cg.alpha = _originalAlpha;
        }
    }
}
```

**重點慣例：**

- 用 `[NonSerialized]` 保留執行期快取（不要被序列化）。
- `OnEnter` 取 hub entry 後做 null 檢查；Runtime 可能沒綁。
- 狀態還原（`OnCancel`）與自然結束（`OnExit`）要分開 — cancel 要能回到原樣，exit 是終止姿勢。

### Step 3 — Editor 中自動出現

寫完後**不需註冊**。[ActionClipDrawer.cs](Assets/TimelinePlayer/Editor/Inspector/ActionClipDrawer.cs) 會用 reflection 掃所有非 abstract 的 `ActionClip` 子類，在 clip Inspector 的型別下拉自動列出 `MyFadeAction`。

### 範例參考

- [GameObjectActiveAction.cs](Assets/TimelinePlayer/Timeline/ActionClips/GameObjectActiveAction.cs) — 最簡單：開關 GameObject，示範原始狀態保存
- [TransformTweenAction.cs](Assets/TimelinePlayer/Timeline/ActionClips/TransformTweenAction.cs) — `OnUpdate` + easing 補間

---

## ReferenceHub：繫結模型

`ActionClip` **不直接引用場景物件** — 場景物件由 [ReferenceHub.cs](Assets/TimelinePlayer/ReferenceHub/ReferenceHub.cs) 以 `key → value` 提供，clip 存 key、runtime 再查。

這樣做的好處：

- 同一條 timeline 可以套不同角色 / 物件（換個 hub 就好）
- clip 序列化的是字串 key，改場景結構不會打壞 asset
- Editor 預覽和 Runtime 播放用一致的機制

### 可用的 Entry 型別

定義於 [ReferenceEntry.cs](Assets/TimelinePlayer/ReferenceHub/ReferenceEntry.cs)：

| Entry | Value 型別 |
| --- | --- |
| `GameObjectEntry` | `GameObject` |
| `MonoBehaviourEntry` | `MonoBehaviour`（用 `.Get<T>()` 轉型） |
| `IntEntry` / `FloatEntry` / `BoolEntry` / `StringEntry` | 對應基本型別 |
| `Vector2Entry` / `Vector3Entry` / `ColorEntry` | 對應結構 |

### 查詢 API

```csharp
// 會噴 warning 如果找不到
var go = hub.GetEntry<GameObjectEntry>("Player")?.Value;

// 安全版
if (hub.TryGetEntry<MonoBehaviourEntry>("Animator", out var entry))
{
    var anim = entry.Get<Animator>();
}
```

---

## Override Bindings

[SequencePlayer.cs](Assets/TimelinePlayer/Player/SequencePlayer.cs) 預設用 `_hub` 解析所有 track 的綁定，但可以透過 `_overrideBindings` 讓某些 track 指向不同的 hub：

```csharp
[SerializeField] private ReferenceHub _hub;
[SerializeField] private List<OverrideBinding> _overrideBindings;

[Serializable]
public class OverrideBinding
{
    public string TrackName;        // 對應 TrackData.TrackName
    public ReferenceHub OverrideHub;
}
```

在 Inspector 可用 [SequencePlayerEditor.cs](Assets/TimelinePlayer/Editor/Inspector/SequencePlayerEditor.cs) 的 **Extract Track Names from SequenceData** 一鍵填入所有 track 名稱，再選擇性指派 override。

**使用情境**：同一段演出套到兩個角色 — 主 hub 放 A，override 其中幾條 track 指向 B。

---

## 檔案命名慣例

| 類型 | 副檔名 / 後綴 |
| --- | --- |
| Timeline asset | `*.playable` |
| 匯出資料 | `*_SequenceData.asset` |

[TimelineAutoSync.cs](Assets/TimelinePlayer/Editor/Exporter/TimelineAutoSync.cs) 的 `GetPairedPath()` 強制這個命名對應。首次存檔自動建立，之後的存檔以 `SourceTimelineGuid` 定位，即使 `.asset` 被改名或搬家也能正確 in-place 更新。

---

## 依賴

- Unity Timeline
- [UniTask](https://github.com/Cysharp/UniTask) — `SequencePlayer.PlayAsync` 的 async loop
- `Midou.Utility` — 部分 Tween 範例使用
