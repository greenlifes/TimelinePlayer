namespace TimelinePlayer
{
    /// <summary>
    /// Base class for all inline action instances stored directly in ClipData.
    /// Each clip holds its own independent [SerializeReference] instance —
    /// no shared ScriptableObject asset, no cross-clip state pollution.
    /// </summary>
    [System.Serializable]
    public abstract class AbstractActionData
    {
        public abstract void OnEnter(ReferenceHub hub);
        public abstract void OnUpdate(ReferenceHub hub, float normalizedTime);
        public abstract void OnExit(ReferenceHub hub);
        /// <summary>Called when playback is cancelled mid-clip. Revert state to before OnEnter.</summary>
        public abstract void OnCancel(ReferenceHub hub);
    }
}
