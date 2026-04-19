using UnityEngine;

namespace TimelinePlayer.Actions
{
    /// <summary>
    /// Activates / deactivates a GameObject from the bound ReferenceHub on enter and exit.
    /// Each ClipData stores its own independent instance of this class.
    /// </summary>
    [System.Serializable]
    public class GameObjectMoveAction : AbstractActionData
    {
        [Tooltip("Key in the bound ReferenceHub pointing to the target GameObject.")]
        public string targetKey;
        public bool returnOrigin;
        public float MoveDistance;
        public Vector3 MoveDirection;

        [System.NonSerialized] private TransformEntry target;
        [System.NonSerialized] private Vector3 originalPosition;

        public override void OnEnter(ReferenceHub hub)
        {
            target = hub.GetEntry<TransformEntry>(targetKey);
            originalPosition = target.Value.position;
        }

        public override void OnUpdate(ReferenceHub hub, float normalizedTime)
        {
            target.Value.position = originalPosition + (normalizedTime * MoveDistance * target.Value.TransformDirection(MoveDirection.normalized));
        }

        public override void OnExit(ReferenceHub hub)
        {
            if (returnOrigin) { target.Value.position = originalPosition; }
        }

        public override void OnCancel(ReferenceHub hub)
        {
            if (target?.Value != null) target.Value.position = originalPosition;
        }
    }
}
