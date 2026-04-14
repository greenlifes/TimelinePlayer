using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace TimelineTool.Editor
{
    /// <summary>
    /// PlayableAsset representing one clip on a TimelineActionTrack.
    /// Holds a reference to an AbstractActionData ScriptableObject
    /// which is exported to TimelineSequenceData via SequenceExporter.
    /// </summary>
    [System.Serializable]
    public class TimelineActionClip : PlayableAsset, ITimelineClipAsset
    {
        [Tooltip("The action ScriptableObject this clip represents.")]
        public AbstractActionData actionData;

        public ClipCaps clipCaps => ClipCaps.None;

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            var playable = ScriptPlayable<TimelineActionBehaviour>.Create(graph);
            playable.GetBehaviour().actionData = actionData;
            return playable;
        }
    }
}
