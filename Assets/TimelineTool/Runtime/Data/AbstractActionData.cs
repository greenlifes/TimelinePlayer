namespace TimelineTool
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
    }
}
