using System.Collections.Generic;
using UnityEngine;

namespace TimelinePlayer
{
    /// <summary>
    /// Root ScriptableObject that stores the full sequence exported from a Unity Timeline.
    /// At runtime, feed this to a SequencePlayer — no dependency on Unity.Timeline required.
    /// </summary>
    [CreateAssetMenu(menuName = "TimelineTool/Sequence Data", fileName = "NewSequenceData")]
    public class TimelineSequenceData : ScriptableObject
    {
        [Tooltip("Total length of the sequence in frames at 60fps.")]
        public int totalFrames;

        public List<TrackData> tracks = new();

        /// <summary>
        /// GUID of the source .playable asset. Used by the exporter to locate
        /// this asset after a rename (so the renamed file is updated in-place).
        /// </summary>
        [HideInInspector]
        public string sourceTimelineGuid;

        /// <summary>Total duration in seconds.</summary>
        public float TotalDuration => totalFrames / 60f;
    }
}
