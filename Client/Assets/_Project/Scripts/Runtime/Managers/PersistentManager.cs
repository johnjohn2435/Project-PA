using System;
using UnityEngine;
namespace PocketArcade
{
    // Each concrete manager owns its lifetime. This is not a manager container or service locator.
    public abstract class PersistentManager : MonoBehaviour
    {
        public void ValidateIndependentRoot()
        {
            if (!gameObject.activeSelf || !enabled || transform.parent != null || GetComponentsInChildren<PersistentManager>(true).Length != 1 || GetComponent<GameBootstrap>() != null)
                throw new InvalidOperationException(GetType().Name + " must be an independent root with exactly one manager.");
        }
        protected void Persist()
        {
            if (!Application.isPlaying) return;
            ValidateIndependentRoot();
            DontDestroyOnLoad(gameObject);
        }
    }
}
