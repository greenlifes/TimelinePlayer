using UnityEngine;

namespace TimelineTool.Actions
{
    /// <summary>
    /// Example action: activates/deactivates the bound GameObject on enter/exit.
    /// Create via: Assets > Create > TimelineTool > Actions > GameObject Active
    /// </summary>
    [CreateAssetMenu(menuName = "TimelineTool/Actions/GameObject Active", fileName = "GameObjectActiveAction")]
    public class GameObjectActiveAction : AbstractActionData
    {
        [Tooltip("SetActive value when the clip starts.")]
        public bool activeOnEnter = true;

        [Tooltip("SetActive value when the clip ends.")]
        public bool activeOnExit = false;

        public override void OnEnter(GameObject target)
        {
            target?.SetActive(activeOnEnter);
        }

        public override void OnUpdate(GameObject target, float normalizedTime)
        {
            // No per-frame logic required for a simple activation toggle.
        }

        public override void OnExit(GameObject target)
        {
            target?.SetActive(activeOnExit);
        }
    }
}
