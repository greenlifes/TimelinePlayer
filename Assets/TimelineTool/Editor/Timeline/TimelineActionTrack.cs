using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace TimelineTool.Editor
{
    /// <summary>
    /// Custom Timeline track that holds TimelineActionClips.
    /// Each track can be bound to a GameObject in the Timeline's Bindings panel.
    /// Right-click the Timeline header and add "TimelineTool > Action Track".
    /// </summary>
    [TrackColor(0.18f, 0.78f, 0.42f)]
    [TrackClipType(typeof(TimelineActionClip))]
    [TrackBindingType(typeof(ReferenceHub))]
    public class TimelineActionTrack : TrackAsset
    {
        public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
        {
            // Sync clip display names from their action data
            foreach (var clip in GetClips())
            {
                if (clip.asset is TimelineActionClip actionClip && actionClip.actionData != null)
                    clip.displayName = actionClip.actionData.GetType().Name;
            }

            return ScriptPlayable<TimelineActionMixerBehaviour>.Create(graph, inputCount);
        }
    }
}
