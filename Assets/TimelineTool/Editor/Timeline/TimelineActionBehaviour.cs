using UnityEngine;
using UnityEngine.Playables;

namespace TimelineTool.Editor
{
    /// <summary>
    /// PlayableBehaviour that drives AbstractActionData during Timeline preview in the Editor.
    /// This class is Editor-only; at runtime the SequencePlayer handles playback instead.
    /// </summary>
    public class TimelineActionBehaviour : PlayableBehaviour
    {
        public AbstractActionData actionData;

        private bool _hasEntered;

        public override void OnBehaviourPlay(Playable playable, FrameData info)
        {
            _hasEntered = false;
        }

        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            if (actionData == null) return;

            var target = playerData as GameObject;

            if (!_hasEntered)
            {
                _hasEntered = true;
                actionData.OnEnter(target);
            }

            double duration = playable.GetDuration();
            float normalizedTime = duration > 0.0
                ? Mathf.Clamp01((float)(playable.GetTime() / duration))
                : 1f;

            actionData.OnUpdate(target, normalizedTime);
        }

        public override void OnBehaviourPause(Playable playable, FrameData info)
        {
            if (!_hasEntered) return;

            _hasEntered = false;

            var target = info.output.GetUserData() as GameObject;
            actionData?.OnExit(target);
        }
    }
}
