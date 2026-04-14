using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace TimelineTool
{
    /// <summary>
    /// Time-based linear playback engine that reads a TimelineSequenceData ScriptableObject
    /// and drives AbstractActionData clips using UniTask. No Unity.Timeline dependency.
    /// </summary>
    public class SequencePlayer : MonoBehaviour
    {
        [SerializeField] private TimelineSequenceData sequenceData;

        [Serializable]
        public class TrackBinding
        {
            [Tooltip("Must match the bindingKey in TrackData.")]
            public string bindingKey;
            public GameObject target;
        }

        [SerializeField] private List<TrackBinding> bindings = new();

        // -----------------------------------------------------------------------

        private CancellationTokenSource _cts;
        private Dictionary<string, GameObject> _bindingMap;

        public bool IsPlaying { get; private set; }

        // -----------------------------------------------------------------------
        // Public API

        public void Play()
        {
            if (IsPlaying) Stop();

            _cts = new CancellationTokenSource();
            PlayAsync(_cts.Token).Forget();
        }

        public void Stop()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
            IsPlaying = false;
        }

        // -----------------------------------------------------------------------

        private void Awake() => RebuildBindingMap();

        private void OnDestroy() => Stop();

        public void RebuildBindingMap()
        {
            _bindingMap = new Dictionary<string, GameObject>(StringComparer.Ordinal);
            foreach (var b in bindings)
                if (b.target != null)
                    _bindingMap[b.bindingKey] = b.target;
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

            // Resolve bindings and flatten all clips into a single list
            var resolvedClips = BuildResolvedClipList();

            // Per-clip state flags
            var entered = new HashSet<ClipData>(ReferenceEqualityComparer.Instance);
            var exited  = new HashSet<ClipData>(ReferenceEqualityComparer.Instance);

            float elapsed   = 0f;
            float totalTime = sequenceData.TotalDuration;

            // ---- Main update loop ----------------------------------------
            while (elapsed <= totalTime)
            {
                if (ct.IsCancellationRequested) break;

                TickFrame(resolvedClips, entered, exited, elapsed);

                await UniTask.Yield(PlayerLoopTiming.Update, ct);
                elapsed += Time.deltaTime;
            }

            // ---- Cleanup: exit any clips still active at sequence end -----
            if (!ct.IsCancellationRequested)
            {
                foreach (var (clip, target) in resolvedClips)
                {
                    if (entered.Contains(clip) && !exited.Contains(clip))
                    {
                        exited.Add(clip);
                        clip.actionData?.OnExit(target);
                    }
                }
            }

            IsPlaying = false;
        }

        private void TickFrame(
            List<(ClipData clip, GameObject target)> resolvedClips,
            HashSet<ClipData> entered,
            HashSet<ClipData> exited,
            float elapsed)
        {
            foreach (var (clip, target) in resolvedClips)
            {
                if (clip.actionData == null) continue;

                bool inRange   = elapsed >= clip.StartTime && elapsed < clip.EndTime;
                bool hasEntered = entered.Contains(clip);
                bool hasExited  = exited.Contains(clip);

                if (hasExited) continue;

                // --- OnEnter ---
                if (inRange && !hasEntered)
                {
                    entered.Add(clip);
                    clip.actionData.OnEnter(target);
                }

                // --- OnUpdate ---
                if (inRange && entered.Contains(clip))
                    clip.actionData.OnUpdate(target, clip.GetNormalizedTime(elapsed));

                // --- OnExit: clip passed its end frame ---
                if (!inRange && hasEntered && elapsed >= clip.EndTime)
                {
                    exited.Add(clip);
                    clip.actionData.OnExit(target);
                }
            }
        }

        private List<(ClipData clip, GameObject target)> BuildResolvedClipList()
        {
            var list = new List<(ClipData, GameObject)>();
            foreach (var track in sequenceData.tracks)
            {
                _bindingMap.TryGetValue(track.bindingKey, out var target);
                foreach (var clip in track.clips)
                    list.Add((clip, target));
            }
            // Sort by start frame so OnEnter fires in chronological order
            list.Sort((a, b) => a.clip.startFrame.CompareTo(b.clip.startFrame));
            return list;
        }

        // -----------------------------------------------------------------------
        // Equality comparer by reference so HashSet works even if ClipData is a class

        private sealed class ReferenceEqualityComparer : IEqualityComparer<ClipData>
        {
            public static readonly ReferenceEqualityComparer Instance = new();
            public bool Equals(ClipData x, ClipData y) => ReferenceEquals(x, y);
            public int GetHashCode(ClipData obj) => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
        }
    }
}
