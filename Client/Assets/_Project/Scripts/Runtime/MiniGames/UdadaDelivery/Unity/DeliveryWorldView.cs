using UnityEngine;
namespace PocketArcade.UdadaDelivery.Unity
{
    public sealed class DeliveryWorldView : MonoBehaviour
    {
        [SerializeField] private Camera playCamera;
        [SerializeField] private Transform playerVisual, parcelVisual;
        [SerializeField] private SpriteRenderer player, parcel, landingPreview;
        [SerializeField] private SpriteRenderer[] carriedBoxes, roadMarks;
        [SerializeField] private DeliveryHouseView[] houses;
        private PocketArcade.Characters.CharacterRigController character;
        public void AttachCharacter(ResourceManager resources, string characterId)
        {
            character = PocketArcade.Characters.CharacterRigController.SpawnForWorld(resources, characterId, playerVisual, player, .43f);
            character.transform.localPosition = Vector3.down * .52f;
        }
        public void ReleaseCharacter(ResourceManager resources)
        {
            if (character != null) resources.Release(character.gameObject);
            character = null;
        }
        public void SetCharacterPaused(bool paused)
        {
            if (character != null) character.SetPaused(paused);
        }
        public Camera PlayCamera => playCamera;
        public void Present(DeliverySession session)
        {
            float distance = (float)session.Distance;
            playCamera.orthographicSize = Mathf.Max(4.6f, 7f / Mathf.Max(.1f, playCamera.aspect));
            float halfWidth = playCamera.orthographicSize * playCamera.aspect;
            playCamera.transform.position = new Vector3(distance + halfWidth * .5f, playCamera.orthographicSize * .04f, -10);
            playerVisual.position = new Vector3(distance, 0, 0);
            float bounce = session.State == DeliveryState.Running ? Mathf.Sin((float)session.Elapsed * 18) * (float)session.Speed * .012f : 0;
            if (character != null) character.SetMovement(session.State == DeliveryState.Running && session.Speed > .02 ? Vector2.right : Vector2.zero);
            int carried = session.Load - (session.ParcelInFlight ? 1 : 0);
            for (int i=0;i<carriedBoxes.Length;i++)
            {
                carriedBoxes[i].enabled = i < carried;
                carriedBoxes[i].transform.position = new Vector3(distance-.06f, .76f+i*.31f+bounce, 0);
                carriedBoxes[i].transform.localRotation = Quaternion.Euler(0,0,-bounce*40*(i+1));
            }
            parcel.enabled = session.ParcelInFlight || session.ResolvedCount > 0 && session.LastOutcome != DeliveryOutcome.Skipped;
            parcelVisual.position = new Vector3((float)(session.ParcelInFlight ? session.ParcelX : session.LastLandingX),(float)session.ParcelHeight,0);
            parcelVisual.localRotation = Quaternion.Euler(0,0, session.ParcelInFlight ? -(float)session.FlightProgress*180 : 0);
            foreach(var renderer in parcelVisual.GetComponentsInChildren<SpriteRenderer>()) renderer.enabled=parcel.enabled;
            for(int i=0;i<houses.Length;i++)
            {
                houses[i].transform.position = new Vector3((float)session.HouseAt(i),0,0);
                houses[i].Present(session.OutcomeAt(i),i==session.ResolvedCount);
            }
            landingPreview.enabled = session.State == DeliveryState.Running && !session.ParcelInFlight && session.Speed >= session.MinimumLaunchSpeed;
            landingPreview.transform.position = new Vector3((float)session.PredictedLanding,-.52f,0);
            bool linedUp = session.ResolvedCount < session.HouseCount && System.Math.Abs(session.PredictedLanding-session.HouseAt(session.ResolvedCount)) <= session.DoorTolerance;
            landingPreview.color = linedUp ? new Color(.2f,.75f,.45f) : new Color(.9f,.58f,.22f);
            int firstMark=Mathf.FloorToInt(distance/2)-8;
            for(int i=0;i<roadMarks.Length;i++)roadMarks[i].transform.position = new Vector3((firstMark+i)*2,-.72f,0);
        }
    }
}
