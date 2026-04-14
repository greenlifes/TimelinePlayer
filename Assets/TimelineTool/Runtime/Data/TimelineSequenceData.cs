using System.Collections.Generic;
using UnityEngine;

namespace TimelineTool
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

        /// <summary>Total duration in seconds.</summary>
        public float TotalDuration => totalFrames / 60f;
    }
}
