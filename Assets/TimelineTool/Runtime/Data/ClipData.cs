using UnityEngine;

namespace TimelineTool
{
    /// <summary>
    /// Represents a single clip within a track, storing timing in frames (1 frame = 1/60 sec)
    /// and a reference to the action that should be executed.
    /// </summary>
    [System.Serializable]
    public class ClipData
    {
        [Tooltip("Start time in frames at 60fps (1 frame = 1/60 sec).")]
        public int startFrame;

        [Tooltip("Duration in frames at 60fps.")]
        public int durationFrames;

        [Tooltip("The action ScriptableObject to execute during this clip.")]
        public AbstractActionData actionData;

        public float StartTime    => startFrame / 60f;
        public float Duration     => durationFrames / 60f;
        public float EndTime      => StartTime + Duration;

        public float GetNormalizedTime(float elapsed)
        {
            if (Duration <= 0f) return 1f;
            return Mathf.Clamp01((elapsed - StartTime) / Duration);
        }
    }
}
