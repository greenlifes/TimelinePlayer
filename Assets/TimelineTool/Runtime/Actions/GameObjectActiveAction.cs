using UnityEngine;

namespace TimelineTool.Actions
{
    /// <summary>
    /// Activates / deactivates a GameObject from the bound ReferenceHub on enter and exit.
    /// Each ClipData stores its own independent instance of this class.
    /// </summary>
    [System.Serializable]
    public class GameObjectActiveAction : AbstractActionData
    {
        [Tooltip("Key in the bound ReferenceHub pointing to the target GameObject.")]
        public string targetKey;

        [Tooltip("SetActive value when the clip starts.")]
        public bool activeOnEnter = true;

        [Tooltip("SetActive value when the clip ends.")]
        public bool activeOnExit = false;

        public override void OnEnter(ReferenceHub hub)
            => hub?.Get<GameObjectEntry>(targetKey)?.Value.SetActive(activeOnEnter);

        public override void OnUpdate(ReferenceHub hub, float normalizedTime) { }

        public override void OnExit(ReferenceHub hub)
            => hub?.Get<GameObjectEntry>(targetKey)?.Value.SetActive(activeOnExit);
    }
}
