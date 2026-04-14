using UnityEngine;

namespace TimelineTool
{
    /// <summary>
    /// Base class for all action data stored in ScriptableObjects.
    /// Inherit from this to define custom component behaviours
    /// that are triggered by the SequencePlayer at runtime.
    /// </summary>
    public abstract class AbstractActionData : ScriptableObject
    {
        /// <summary>
        /// Called once when the clip's start frame is reached.
        /// </summary>
        /// <param name="target">The GameObject bound to this track, or null if unbound.</param>
        public abstract void OnEnter(GameObject target);

        /// <summary>
        /// Called every Update frame while the clip is active.
        /// </summary>
        /// <param name="target">The GameObject bound to this track.</param>
        /// <param name="normalizedTime">Playback position within the clip, from 0 (start) to 1 (end).</param>
        public abstract void OnUpdate(GameObject target, float normalizedTime);

        /// <summary>
        /// Called once when the clip's end frame is passed.
        /// </summary>
        /// <param name="target">The GameObject bound to this track.</param>
        public abstract void OnExit(GameObject target);
    }
}
