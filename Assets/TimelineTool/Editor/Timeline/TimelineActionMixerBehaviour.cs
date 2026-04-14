using UnityEngine.Playables;

namespace TimelineTool.Editor
{
    /// <summary>
    /// Mixer behaviour for TimelineActionTrack.
    /// Clips run independently, so no blending logic is needed here.
    /// </summary>
    public class TimelineActionMixerBehaviour : PlayableBehaviour { }
}
