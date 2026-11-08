using UnityEngine;
namespace PocketArcade.UdadaDelivery.Unity
{
    [CreateAssetMenu(menuName = "Pocket Arcade/Udada Delivery/Tuning")]
    public sealed class DeliveryTuning : ScriptableObject
    {
        [SerializeField] private float duration = 45, runSpeed = 4, acceleration = 12, deceleration = 8;
        [SerializeField] private float parcelDrag = 2, minimumLaunchSpeed = 2.5f, doorTolerance = 1, perfectTolerance = .3f;
        [SerializeField] private float firstHouse = 9, houseSpacing = 14;
        [SerializeField] private int houseCount = 3;
        public DeliveryRules CreateRules() => new DeliveryRules { Duration = duration, RunSpeed = runSpeed, Acceleration = acceleration,
            Deceleration = deceleration, ParcelDrag = parcelDrag, MinimumLaunchSpeed = minimumLaunchSpeed,
            DoorTolerance = doorTolerance, PerfectTolerance = perfectTolerance, FirstHouse = firstHouse, HouseSpacing = houseSpacing, HouseCount = houseCount };
    }
}
