using UnityEngine;
namespace PocketArcade.UdadaDelivery.Unity
{
    public sealed class DeliveryHouseView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer door, landingZone, perfectZone;
        public void Present(DeliveryOutcome outcome, bool current)
        {
            bool success = outcome == DeliveryOutcome.Perfect || outcome == DeliveryOutcome.Delivered;
            door.color = outcome == DeliveryOutcome.Pending ? new Color(.2f,.35f,.3f) : success ? new Color(.25f,.7f,.45f) : new Color(.8f,.35f,.3f);
            landingZone.color = success ? new Color(.25f,.7f,.45f) : outcome != DeliveryOutcome.Pending ? new Color(.8f,.35f,.3f) : current ? new Color(.35f,.75f,.5f) : new Color(.7f,.75f,.69f);
            perfectZone.enabled = current && outcome == DeliveryOutcome.Pending;
        }
    }
}
