using PocketArcade.Characters;
using UnityEngine;
namespace PocketArcade.EchoRoom.Unity
{
    public sealed class EchoWorldView:MonoBehaviour
    {
        [SerializeField] private Camera playCamera;
        [SerializeField] private SpriteRenderer[] actors,cargo,parcels,gates,plates;
        [SerializeField] private LineRenderer[] trails;
        private CharacterRigController[] rigs;
        private Renderer[][] parts;
        private bool[][] authoredVisibility;
        private Vector3[] previous;
        private int shownGhosts=-1;
        private bool paused;
        public Camera PlayCamera=>playCamera;
        private static readonly Color[] Colors={Color.white,new Color(.3f,.8f,1),new Color(.8f,.5f,1),new Color(1,.65f,.3f),new Color(.5f,.9f,.6f),new Color(1,.5f,.75f),new Color(.7f,.7f,1),new Color(.8f,1,.4f)};
        public static Vector3 Project(EchoPoint p)=>new Vector3((float)p.X,(float)p.Y*.8f,0);
        public void Attach(ResourceManager resources,string id)
        {
            rigs=new CharacterRigController[actors.Length];parts=new Renderer[actors.Length][];authoredVisibility=new bool[actors.Length][];previous=new Vector3[actors.Length];
            for(int i=0;i<actors.Length;i++)
            {
                rigs[i]=CharacterRigController.SpawnForWorld(resources,id,transform,actors[i],.33f);parts[i]=rigs[i].GetComponentsInChildren<Renderer>(true);authoredVisibility[i]=new bool[parts[i].Length];for(int n=0;n<parts[i].Length;n++)authoredVisibility[i][n]=parts[i][n].enabled;
                if(i>0)foreach(var part in parts[i])TintGhost(part,Colors[i%Colors.Length]);
            }
        }
        private static void TintGhost(Renderer renderer,Color tint)
        {
            tint.a=.48f;
            if(renderer is SpriteRenderer sprite)
            {var c=sprite.color;sprite.color=new Color(c.r*tint.r,c.g*tint.g,c.b*tint.b,c.a*tint.a);return;}
            // Mesh ghosts use per-instance properties; never alter shared skin materials.
            var materials=renderer.sharedMaterials;
            for(int index=0;index<materials.Length;index++)
            {
                var material=materials[index];if(material==null)continue;
                string property=material.HasProperty("_BaseColor")?"_BaseColor":material.HasProperty("_Color")?"_Color":null;
                if(property==null)continue;
                var color=material.GetColor(property);var block=new MaterialPropertyBlock();renderer.GetPropertyBlock(block,index);
                block.SetColor(property,new Color(color.r*tint.r,color.g*tint.g,color.b*tint.b,color.a*tint.a));renderer.SetPropertyBlock(block,index);
            }
        }
        public void Release(ResourceManager resources){if(rigs!=null)foreach(var rig in rigs)if(rig!=null)resources.Release(rig.gameObject);rigs=null;parts=null;}
        public void SetPaused(bool value){paused=value;if(rigs!=null)foreach(var rig in rigs)if(rig!=null)rig.SetPaused(value);}
        public void Present(EchoSession s,Vector2 movement=default)
        {
            playCamera.orthographicSize=Mathf.Max(4.8f,4.0f/Mathf.Max(.1f,playCamera.aspect));playCamera.transform.position=new Vector3(0,-.6f,-10);
            for(int i=0;i<actors.Length;i++)
            {
                bool active=i<s.ActorCount;var a=active?s.ActorAt(i):default;var p=active?Project(a.Position):Vector3.zero;actors[i].enabled=active&&rigs==null;actors[i].transform.position=p;
                cargo[i].enabled=active&&a.Carrying;cargo[i].transform.position=p+Vector3.up*.82f;
                if(rigs!=null){for(int n=0;n<parts[i].Length;n++)parts[i][n].enabled=active&&authoredVisibility[i][n];rigs[i].SetPaused(paused||!active||s.State!=EchoState.Playing);rigs[i].transform.position=p;var delta=p-previous[i];rigs[i].SetMovement(!active||s.State!=EchoState.Playing?Vector2.zero:i==0?movement:new Vector2(delta.x,delta.y).normalized);}
                previous[i]=p;
            }
            for(int i=0;i<parcels.Length;i++){parcels[i].enabled=i<s.ParcelCount&&s.ParcelAvailable(i);if(i<s.ParcelCount)parcels[i].transform.position=Project(s.ParcelAt(i));}
            for(int i=0;i<gates.Length;i++){var color=i==0?new Color(.3f,.62f,.9f):new Color(.7f,.44f,.8f);gates[i].color=new Color(color.r,color.g,color.b,s.GateOpen(i)?.16f:1);plates[i].color=s.GateOpen(i)?color:new Color(color.r*.7f,color.g*.7f,color.b*.7f);}
            if(shownGhosts!=s.GhostCount)
            {
                shownGhosts=s.GhostCount;
                for(int i=0;i<trails.Length;i++)
                {
                    trails[i].enabled=i<s.GhostCount;if(i>=s.GhostCount)continue;
                    int count=s.FramesPerLoop/12+1;var points=new Vector3[count];for(int n=0;n<count;n++)points[n]=Project(s.TracePoint(i,n*12))+Vector3.forward*.04f;
                    trails[i].positionCount=count;trails[i].SetPositions(points);var c=Colors[(i+1)%Colors.Length];c.a=.23f;trails[i].startColor=trails[i].endColor=c;
                }
            }
        }
    }
}
