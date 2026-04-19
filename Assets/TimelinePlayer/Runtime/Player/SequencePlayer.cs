using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace TimelinePlayer
{
    /// <summary>
    /// Time-based linear playback engine.
    /// Reads a TimelineSequenceData ScriptableObject and drives AbstractActionData
    /// clips via UniTask. Each track binding maps a key to a ReferenceHub,
    /// which is passed directly into OnEnter / OnUpdate / OnExit.
    /// </summary>
    public class SequencePlayer : MonoBehaviour
    {
        [SerializeField] private TimelineSequenceData sequenceData;
        [SerializeField] private ReferenceHub referenceHub;

        [Serializable]
        public class OverrideBinding
        {
            [Tooltip("Must match the bindingKey in TrackData (= Timeline track name).")]
            public string bindingKey;
            public ReferenceHub OverrideHub;
        }

        [SerializeField] private List<OverrideBinding> bindings = new();

        // -----------------------------------------------------------------------

        private CancellationTokenSource _cts;
        private Dictionary<string, ReferenceHub> _bindingMap;
        private bool _isPaused;

        public bool IsPlaying { get; private set; }
        public bool IsPaused  => _isPaused;

        // -----------------------------------------------------------------------
        // Public API

        public void Play()
        {
            if (IsPlaying) Stop();
            _isPaused = false;
            RebuildBindingMap();
            _cts = new CancellationTokenSource();
            PlayAsync(_cts.Token).Forget();
        }

        public void Pause()  => _isPaused = true;
        public void Resume() => _isPaused = false;

        public void Stop()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
            _isPaused = false;
            IsPlaying = false;
        }

        // -----------------------------------------------------------------------

        private void Awake()      => RebuildBindingMap();
        private void OnDestroy()  => Stop();

        public void RebuildBindingMap()
        {
            _bindingMap = new Dictionary<string, ReferenceHub>(StringComparer.Ordinal);
            foreach (var b in bindings)
            {
                if (b.OverrideHub != null) { _bindingMap[b.bindingKey] = b.OverrideHub; }
            }
        }

        // -----------------------------------------------------------------------

        private async UniTaskVoid PlayAsync(CancellationToken ct)
        {
            if (sequenceData == null || sequenceData.tracks == null)
            {
                Debug.LogWarning("[SequencePlayer] No SequenceData assigned.", this);
                return;
            }

            IsPlaying = true;

            var allClips = BuildResolvedClipList();
            var entered  = new HashSet<ClipData>(ReferenceEqualityComparer.Instance);
            var exited   = new HashSet<ClipData>(ReferenceEqualityComparer.Instance);

            float elapsed   = 0f;
            float totalTime = sequenceData.TotalDuration;

            // ---- Main update loop ----------------------------------------
            while (elapsed <= totalTime)
            {
                if (ct.IsCancellationRequested) break;

                if (!_isPaused)
                {
                    TickFrame(allClips, entered, exited, elapsed);
                }

                await UniTask.Yield(PlayerLoopTiming.Update, ct);

                if (!_isPaused)
                    elapsed += Time.deltaTime;
            }

            // ---- Cleanup: exit any clips still active at sequence end -----
            if (!ct.IsCancellationRequested)
            {
                foreach (var (clip, hub) in allClips)
                {
                    if (entered.Contains(clip) && !exited.Contains(clip))
                    {
                        exited.Add(clip);
                        clip.actionData?.OnExit(hub);
                    }
                }
            }
            else
            {
                // Cancelled mid-play: revert any active clips to their pre-enter state
                foreach (var (clip, hub) in allClips)
                {
                    if (entered.Contains(clip) && !exited.Contains(clip))
                    {
                        exited.Add(clip);
                        clip.actionData?.OnCancel(hub);
                    }
                }
            }

            IsPlaying = false;
        }

        private static void TickFrame(
            List<(ClipData clip, ReferenceHub hub)> allClips,
            HashSet<ClipData> entered,
            HashSet<ClipData> exited,
            float elapsed)
        {
            foreach (var (clip, hub) in allClips)
            {
                if (clip.actionData == null) continue;

                bool inRange    = elapsed >= clip.StartTime && elapsed < clip.EndTime;
                bool hasEntered = entered.Contains(clip);
                bool hasExited  = exited.Contains(clip);

                if (hasExited) continue;

                if (inRange && !hasEntered)
                {
                    entered.Add(clip);
                    clip.actionData.OnEnter(hub);
                }

                if (inRange && hasEntered)
                    clip.actionData.OnUpdate(hub, clip.GetNormalizedTime(elapsed));

                if (!inRange && hasEntered && elapsed >= clip.EndTime)
                {
                    exited.Add(clip);
                    clip.actionData.OnExit(hub);
                }
            }
        }

        private List<(ClipData clip, ReferenceHub hub)> BuildResolvedClipList()
        {
            var list = new List<(ClipData clip, ReferenceHub hub)>();
            foreach (var track in sequenceData.tracks)
            {
                var overrided = _bindingMap.TryGetValue(track.bindingKey, out var hub);
                foreach (var clip in track.clips)
                {
                    list.Add((clip, overrided? hub : referenceHub));
                }
            }
            list.Sort((a, b) => a.clip.startFrame.CompareTo(b.clip.startFrame));
            return list;
        }

        // -----------------------------------------------------------------------

        private sealed class ReferenceEqualityComparer : IEqualityComparer<ClipData>
        {
            public static readonly ReferenceEqualityComparer Instance = new();
            public bool Equals(ClipData x, ClipData y)  => ReferenceEquals(x, y);
            public int  GetHashCode(ClipData obj) =>
                System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
        }
    }
}
