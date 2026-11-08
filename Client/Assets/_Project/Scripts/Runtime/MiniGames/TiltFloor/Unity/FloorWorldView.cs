using UnityEngine;
namespace PocketArcade.TiltFloor.Unity
{
    public sealed class FloorWorldView:MonoBehaviour
    {
        [SerializeField] private Camera playCamera;
        [SerializeField] private MeshFilter surface;
        [SerializeField] private SpriteRenderer player,pulse;
        [SerializeField] private SpriteRenderer[] bodies,warnings,borders,grid,legs,arrow;
        private PocketArcade.Characters.CharacterRigController character;
        private Mesh mesh;
        private Vector3[] vertices;
        private FloorSession presentedSession;
        private int presentedLandings;
        private float impactAge=1,landingPress;
        public Camera PlayCamera=>playCamera;
        public void AttachCharacter(ResourceManager resources,string id)=>character=PocketArcade.Characters.CharacterRigController.SpawnForWorld(resources,id,transform,player,.38f);
        public void ReleaseCharacter(ResourceManager resources){if(character!=null)resources.Release(character.gameObject);character=null;}
        public void SetCharacterPaused(bool paused){if(character!=null)character.SetPaused(paused);}
        private Vector3 Project(double x,double y,FloorVector tilt)=>new Vector3((float)x,(float)(y*.66-(tilt.X*x+tilt.Y*y)*.012-landingPress),0);
        private static void Line(SpriteRenderer r,Vector3 a,Vector3 b,float width)
        {var d=b-a;r.transform.position=(a+b)*.5f;r.transform.localScale=new Vector3(d.magnitude,width,1);r.transform.rotation=Quaternion.Euler(0,0,Mathf.Atan2(d.y,d.x)*Mathf.Rad2Deg);}
        public void Present(FloorSession s,Vector2 movement=default)
        {
            if(!ReferenceEquals(presentedSession,s))
            {presentedSession=s;presentedLandings=0;impactAge=1;}
            if(s.Landed!=presentedLandings)
            {presentedLandings=s.Landed;impactAge=Mathf.Min(impactAge,(float)(s.Elapsed-s.LastLandingTime));}
            else impactAge+=Mathf.Min(Time.unscaledDeltaTime,.1f);
            float envelope=Mathf.Clamp01(1-impactAge/.32f);
            landingPress=.14f*envelope*envelope*Mathf.Abs(Mathf.Sin(impactAge*32));
            var shake=new Vector3(Mathf.Sin(impactAge*91)*.045f,Mathf.Cos(impactAge*73)*.07f,0)*envelope*envelope;
            playCamera.orthographicSize=Mathf.Max(5f,Mathf.Max(4.65f,(float)s.HalfSize*1.31f)/Mathf.Max(.1f,playCamera.aspect));playCamera.transform.position=new Vector3(0,-playCamera.orthographicSize*.10f,-10)+shake;
            if(mesh==null){mesh=surface.mesh;mesh.MarkDynamic();vertices=new Vector3[borders.Length+1];}
            vertices[0]=new Vector3(0,-landingPress,.1f);double radius=s.HalfSize;
            var edgeColor=Color.Lerp(new Color(.3f,.47f,.4f),new Color(.88f,.3f,.23f),Mathf.InverseLerp(.65f,1,(float)s.Danger));
            for(int i=0;i<borders.Length;i++)
            {
                double a=i*System.Math.PI*2/borders.Length,b=(i+1)*System.Math.PI*2/borders.Length;
                var from=Project(System.Math.Cos(a)*radius,System.Math.Sin(a)*radius,s.Tilt);var to=Project(System.Math.Cos(b)*radius,System.Math.Sin(b)*radius,s.Tilt);
                vertices[i+1]=from+Vector3.forward*.1f;Line(borders[i],from,to,.08f);borders[i].color=edgeColor;
            }
            mesh.vertices=vertices;mesh.RecalculateBounds();
            for(int i=0;i<legs.Length;i++){double a=(i+.5)*System.Math.PI*.5;var basePoint=Project(System.Math.Cos(a)*radius,System.Math.Sin(a)*radius,new FloorVector(0,0))+Vector3.down*(.35f-landingPress);Line(legs[i],basePoint,Project(System.Math.Cos(a)*radius,System.Math.Sin(a)*radius,s.Tilt),.07f);}
            for(int i=0;i<grid.Length;i++){double p=-radius+(i%5+1)*radius/3,span=System.Math.Sqrt(radius*radius-p*p);Line(grid[i],i<5?Project(p,-span,s.Tilt):Project(-span,p,s.Tilt),i<5?Project(p,span,s.Tilt):Project(span,p,s.Tilt),.025f);}
            player.transform.position=Project(s.Player.X,s.Player.Y,s.Tilt)+Vector3.up*.12f;
            if(character!=null){character.transform.position=Project(s.Player.X,s.Player.Y,s.Tilt);character.transform.rotation=Quaternion.Euler(0,0,Mathf.Clamp((float)s.Tilt.X*.25f,-8,8));character.SetMovement(s.State==FloorState.Playing?movement:Vector2.zero);if(s.State==FloorState.Fell||s.State==FloorState.Hit)character.SetPaused(true);}
            for(int i=0;i<bodies.Length;i++)
            {
                var body=s.BodyAt(i);var ground=Project(body.Position.X,body.Position.Y,s.Tilt);bool warning=body.IsEnemy&&(body.Phase==DropPhase.Warning||body.Phase==DropPhase.Falling);
                if(i<warnings.Length){warnings[i].enabled=warning;warnings[i].transform.position=ground;warnings[i].transform.localScale=Vector3.one*(body.Phase==DropPhase.Warning?.4f+(float)body.Progress*.4f:.8f);warnings[i].color=new Color(.85f,.22f,.18f,.35f);}
                bodies[i].enabled=body.Phase==DropPhase.Alive||body.Phase==DropPhase.Falling||body.Phase==DropPhase.Ejected;
                float height=body.Phase==DropPhase.Falling?(1-(float)body.Progress)*3.5f:body.Phase==DropPhase.Ejected?-(float)body.Progress*2:0;
                float size=body.IsEnemy?.48f:.3f;if(body.Phase==DropPhase.Ejected)size*=1-(float)body.Progress*.8f;
                bodies[i].transform.position=ground+Vector3.up*(.08f+height);bodies[i].transform.localScale=Vector3.one*size;bodies[i].transform.rotation=Quaternion.Euler(0,0,body.IsEnemy?0:45);
                bodies[i].color=body.IsEnemy?new Color(.8f,.29f,.24f,body.Phase==DropPhase.Ejected?1-(float)body.Progress:1):new Color(.98f,.72f,.13f);
            }
            float age=(float)(s.Elapsed-s.LastCollectedTime);pulse.enabled=age<.3f;pulse.transform.position=Project(s.LastCollectedAt.X,s.LastCollectedAt.Y,s.Tilt);pulse.transform.localScale=Vector3.one*(.3f+age*2);pulse.color=new Color(.4f,.85f,.55f,Mathf.Clamp01(1-age/.3f));
            var direction=new Vector3((float)s.Tilt.X,(float)s.Tilt.Y*.66f,0).normalized;var start=new Vector3(0,2.7f,0);var end=start+direction*(float)s.Danger*.8f;Line(arrow[0],start,end,.05f);Line(arrow[1],end,end-Quaternion.Euler(0,0,30)*direction*.23f,.05f);Line(arrow[2],end,end-Quaternion.Euler(0,0,-30)*direction*.23f,.05f);foreach(var part in arrow){part.enabled=s.Danger>.08;part.color=edgeColor;}
        }
        private void OnDestroy(){if(mesh!=null)Destroy(mesh);}
    }
}
