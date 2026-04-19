using UnityEngine;
using UnityEngine.Playables;

namespace TimelinePlayer.Editor
{
    /// <summary>
    /// PlayableBehaviour that drives AbstractActionData during Timeline preview in the Editor.
    /// The bound object is a ReferenceHub (TrackBindingType = ReferenceHub),
    /// passed directly as playerData into each lifecycle call.
    /// </summary>
    public class TimelineActionBehaviour : PlayableBehaviour
    {
        public AbstractActionData actionData;

        private bool         _hasEntered;
        private ReferenceHub _hub;

        public override void OnBehaviourPlay(Playable playable, FrameData info)
        {
            _hasEntered = false;
        }

        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            if (actionData == null) return;

            _hub = playerData as ReferenceHub;

            if (!_hasEntered)
            {
                _hasEntered = true;
                actionData.OnEnter(_hub);
            }

            double duration = playable.GetDuration();
            float normalizedTime = duration > 0.0
                ? Mathf.Clamp01((float)(playable.GetTime() / duration))
                : 1f;

            actionData.OnUpdate(_hub, normalizedTime);
        }

        public override void OnBehaviourPause(Playable playable, FrameData info)
        {
            if (!_hasEntered) return;
            _hasEntered = false;
            actionData?.OnExit(_hub);
            _hub = null;
        }
    }
}
