using UnityEngine.Playables;

namespace TimelinePlayer.Editor
{
    /// <summary>
    /// Mixer behaviour for TimelineActionTrack.
    /// Clips run independently, so no blending logic is needed here.
    /// </summary>
    public class TimelineActionMixerBehaviour : PlayableBehaviour { }
}
