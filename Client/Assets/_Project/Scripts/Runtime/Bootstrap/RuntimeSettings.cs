using PocketArcade.Core;
using UnityEngine;

namespace PocketArcade
{
    [DisallowMultipleComponent]
    public sealed class RuntimeSettings : MonoBehaviour
    {
        private void Awake()
        {
            Application.targetFrameRate = ProjectDefaults.TargetFrameRate;
            QualitySettings.vSyncCount = 0;
            Screen.orientation = ScreenOrientation.Portrait;
        }
    }
}
