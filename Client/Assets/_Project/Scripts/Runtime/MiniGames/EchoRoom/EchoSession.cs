using System;
using System.Collections.Generic;
namespace PocketArcade.EchoRoom
{
    public readonly struct EchoPoint
    {
        public readonly double X,Y;
        public EchoPoint(double x,double y){X=x;Y=y;}
        public double Length=>Math.Sqrt(X*X+Y*Y);
        public EchoPoint Limited(double max)=>Length>max?this*(max/Length):this;
        public static EchoPoint operator +(EchoPoint a,EchoPoint b)=>new EchoPoint(a.X+b.X,a.Y+b.Y);
        public static EchoPoint operator -(EchoPoint a,EchoPoint b)=>new EchoPoint(a.X-b.X,a.Y-b.Y);
        public static EchoPoint operator *(EchoPoint a,double v)=>new EchoPoint(a.X*v,a.Y*v);
    }
    public enum EchoState{Ready,Playing,Complete,TimeUp}
    public sealed class EchoRules
    {
        public double LoopSeconds=10,Speed=3.5,DoorGrace=4;
        public int Rounds=6,Target=18;
        public EchoPoint[] Parcels={new EchoPoint(-2.6,2),new EchoPoint(-2.6,-2),new EchoPoint(2.6,2),new EchoPoint(2.6,-2),new EchoPoint(0,2.65),new EchoPoint(0,-2.65)};
        public EchoRules Copy(){var r=(EchoRules)MemberwiseClone();r.Parcels=(EchoPoint[])Parcels?.Clone();return r;}
        public void Validate()
        {
            foreach(double v in new[]{LoopSeconds,Speed,DoorGrace})if(double.IsNaN(v)||double.IsInfinity(v)||v<=0)throw new ArgumentOutOfRangeException(nameof(v));
            if(LoopSeconds<.5||LoopSeconds>30||Rounds<2||Rounds>8||Target<1||Parcels==null||Parcels.Length<1||Parcels.Length>20)throw new ArgumentOutOfRangeException(nameof(EchoRules));
            foreach(var p in Parcels)if(double.IsNaN(p.X)||double.IsNaN(p.Y)||double.IsInfinity(p.X)||double.IsInfinity(p.Y)||Math.Abs(p.X)>3.1||Math.Abs(p.Y)>2.9||p.Length<.9)throw new ArgumentOutOfRangeException(nameof(p));
        }
    }
    public readonly struct EchoActor
    {
        public readonly EchoPoint Position;
        public readonly bool Carrying;
        public EchoActor(EchoPoint p,bool carrying){Position=p;Carrying=carrying;}
    }
    // Position recording is immutable. Replayed actors interact with the current parcels and plates.
    public sealed class EchoSession
    {
        public const string GameId="echo_room";
        public const int StepsPerSecond=120;
        private readonly EchoRules rules;
        private readonly int loopFrames;
        private readonly List<EchoPoint[]> traces=new List<EchoPoint[]>();
        private EchoPoint[] recording;
        private readonly EchoPoint[] actors;
        private readonly int[] cargo;
        private readonly bool[] parcels;
        private readonly double[] gateTimers=new double[2];
        private int frame,totalSteps;
        private double accumulator;
        public EchoState State{get;private set;}=EchoState.Ready;
        public int Delivered{get;private set;}
        public int GhostDeliveries{get;private set;}
        public int Loop=>traces.Count+1;
        public int GhostCount=>traces.Count;
        public int ActorCount=>GhostCount+1;
        public int ParcelCount=>parcels.Length;
        public int Target=>rules.Target;
        public int Rounds=>rules.Rounds;
        public int FramesPerLoop=>loopFrames;
        public double LoopElapsed=>(double)frame/StepsPerSecond;
        public double LoopRemaining=>(double)(loopFrames-frame)/StepsPerSecond;
        public double Remaining=>(double)(loopFrames*rules.Rounds-totalSteps)/StepsPerSecond;
        public double Elapsed=>(double)totalSteps/StepsPerSecond;
        public double LoopProgress=>(double)frame/loopFrames;
        public EchoSession(EchoRules rules=null)
        {
            this.rules=(rules??new EchoRules()).Copy();this.rules.Validate();loopFrames=(int)Math.Round(this.rules.LoopSeconds*StepsPerSecond);
            actors=new EchoPoint[this.rules.Rounds];cargo=new int[actors.Length];parcels=new bool[this.rules.Parcels.Length];recording=new EchoPoint[loopFrames+1];ResetRoom();
        }
        public EchoActor ActorAt(int index)=>new EchoActor(actors[index],cargo[index]>=0);
        public EchoPoint ParcelAt(int index)=>rules.Parcels[index];
        public bool ParcelAvailable(int index)=>parcels[index];
        public bool GateOpen(int index)=>gateTimers[index]>0;
        public EchoPoint PlateAt(int index)=>index==0?new EchoPoint(.65,-1.1):new EchoPoint(-.65,1.1);
        public EchoPoint TracePoint(int ghost,int index)=>traces[ghost][Math.Min(loopFrames,index)];
        public void Start(){if(State==EchoState.Ready)State=EchoState.Playing;}
        public void Tick(double seconds,EchoPoint input)
        {
            if(double.IsNaN(seconds)||double.IsInfinity(seconds)||seconds<0||double.IsNaN(input.X)||double.IsNaN(input.Y)||double.IsInfinity(input.X)||double.IsInfinity(input.Y))throw new ArgumentOutOfRangeException(nameof(seconds));
            if(State!=EchoState.Playing)return;accumulator+=seconds;input=input.Limited(1);const double dt=1.0/StepsPerSecond;
            while(accumulator+1e-10>=dt&&State==EchoState.Playing){accumulator-=dt;Step(input,dt);}
        }
        private void ResetRoom()
        {
            for(int i=0;i<actors.Length;i++){actors[i]=new EchoPoint(0,0);cargo[i]=-1;}
            for(int i=0;i<parcels.Length;i++)parcels[i]=true;
            gateTimers[0]=gateTimers[1]=0;frame=0;recording[0]=actors[0];
        }
        private void Plates(double dt)
        {
            for(int g=0;g<2;g++)
            {
                gateTimers[g]=Math.Max(0,gateTimers[g]-dt);
                for(int a=0;a<ActorCount;a++)if((actors[a]-PlateAt(g)).Length<.42){gateTimers[g]=rules.DoorGrace;break;}
            }
        }
        private static EchoPoint Box(EchoPoint p,double cx,double cy,double width,double height)
        {
            double dx=p.X-cx,dy=p.Y-cy,hx=width*.5+.18,hy=height*.5+.18;
            if(Math.Abs(dx)>=hx||Math.Abs(dy)>=hy)return p;
            return hx-Math.Abs(dx)<hy-Math.Abs(dy)?new EchoPoint(cx+(dx<0?-hx:hx),p.Y):new EchoPoint(p.X,cy+(dy<0?-hy:hy));
        }
        private EchoPoint Constrain(EchoPoint p)
        {
            p=new EchoPoint(Math.Max(-3.22,Math.Min(3.22,p.X)),Math.Max(-2.92,Math.Min(2.92,p.Y)));
            for(int side=0;side<2;side++)
            {
                double x=side==0?-1.25:1.25;
                p=Box(p,x,-1.9,.16,2.6);p=Box(p,x,1.9,.16,2.6);
                if(!GateOpen(side))p=Box(p,x,0,.16,1.2);
            }
            return p;
        }
        private void Step(EchoPoint input,double dt)
        {
            for(int i=1;i<ActorCount;i++)actors[i]=traces[i-1][frame+1];
            Plates(dt);var p=actors[0];p=Constrain(new EchoPoint(p.X+input.X*rules.Speed*dt,p.Y));p=Constrain(new EchoPoint(p.X,p.Y+input.Y*rules.Speed*dt));
            // The central delivery bay is safe; recordings cannot trap the player at the loop spawn.
            if(p.Length>.85)for(int i=1;i<ActorCount;i++)
            {
                var delta=p-actors[i];double distance=delta.Length;
                if(distance<.37){var normal=distance>.001?delta*(1/distance):new EchoPoint(0,1);p=Constrain(actors[i]+normal*.37);}
            }
            actors[0]=p;Plates(0);
            for(int a=0;a<ActorCount;a++)
            {
                if(cargo[a]>=0&&actors[a].Length<.65){cargo[a]=-1;Delivered++;if(a>0)GhostDeliveries++;}
                if(cargo[a]<0)for(int item=0;item<parcels.Length;item++)if(parcels[item]&&(actors[a]-rules.Parcels[item]).Length<.4){parcels[item]=false;cargo[a]=item;break;}
            }
            recording[++frame]=actors[0];totalSteps++;
            if(Delivered>=rules.Target){State=EchoState.Complete;return;}
            if(frame==loopFrames)
            {
                if(Loop==rules.Rounds){State=EchoState.TimeUp;return;}
                traces.Add(recording);recording=new EchoPoint[loopFrames+1];ResetRoom();
            }
        }
    }
}
