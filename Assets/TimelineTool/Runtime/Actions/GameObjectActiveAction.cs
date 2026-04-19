using UnityEngine;

namespace TimelinePlayer.Actions
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

        [System.NonSerialized] private bool _originalActive;

        public override void OnEnter(ReferenceHub hub)
        {
            var go = hub?.GetEntry<GameObjectEntry>(targetKey)?.Value;
            if (go == null) return;
            _originalActive = go.activeSelf;
            go.SetActive(activeOnEnter);
        }

        public override void OnUpdate(ReferenceHub hub, float normalizedTime) { }

        public override void OnExit(ReferenceHub hub)
            => hub?.GetEntry<GameObjectEntry>(targetKey)?.Value.SetActive(activeOnExit);

        public override void OnCancel(ReferenceHub hub)
            => hub?.GetEntry<GameObjectEntry>(targetKey)?.Value.SetActive(_originalActive);
    }
}
