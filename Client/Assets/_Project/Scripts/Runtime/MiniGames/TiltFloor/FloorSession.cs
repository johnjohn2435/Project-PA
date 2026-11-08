using System;
namespace PocketArcade.TiltFloor
{
    public readonly struct FloorVector
    {
        public readonly double X,Y;
        public FloorVector(double x,double y){X=x;Y=y;}
        public double Length=>Math.Sqrt(X*X+Y*Y);
        public FloorVector Limited(double max)=>Length>max?this*(max/Length):this;
        public static FloorVector operator +(FloorVector a,FloorVector b)=>new FloorVector(a.X+b.X,a.Y+b.Y);
        public static FloorVector operator -(FloorVector a,FloorVector b)=>new FloorVector(a.X-b.X,a.Y-b.Y);
        public static FloorVector operator *(FloorVector a,double scale)=>new FloorVector(a.X*scale,a.Y*scale);
    }
    public enum FloorState{Ready,Playing,Fell,Hit}
    public enum DropPhase{Inactive,Warning,Falling,Alive,Ejected}
    public sealed class FloorRules
    {
        public double HalfSize=4,RunSpeed=3.8,TiltPerMeter=9.4,MaxTilt=34;
        public double FirstDrop=2,SpawnInterval=3.2,MinSpawnInterval=.18,RampSeconds=14,WarningSeconds=1,DropSeconds=.5;
        public double EnemySpeed=.65,EnemySlope=.3,SpawnRadiusRatio=.82;
        public double FirstBonus=6,BonusInterval=7,BonusLifetime=9,BonusRadiusRatio=.72;
        public int MaxEnemies=64,MaxItems=3,TimePointsPerSecond=10,ItemPoints=250;
        public int Capacity=>MaxEnemies+MaxItems;
        public FloorRules Copy()=>(FloorRules)MemberwiseClone();
        public void Validate()
        {
            foreach(double v in new[]{HalfSize,RunSpeed,TiltPerMeter,MaxTilt,FirstDrop,SpawnInterval,MinSpawnInterval,RampSeconds,WarningSeconds,DropSeconds,EnemySpeed,EnemySlope,FirstBonus,BonusInterval,BonusLifetime})
                if(double.IsNaN(v)||double.IsInfinity(v)||v<=0)throw new ArgumentOutOfRangeException(nameof(v));
            foreach(double v in new[]{SpawnRadiusRatio,BonusRadiusRatio})if(double.IsNaN(v)||double.IsInfinity(v)||v<0||v>1)throw new ArgumentOutOfRangeException(nameof(v));
            if(HalfSize<2||MaxEnemies<1||MaxEnemies>128||MaxItems<1||MaxItems>8||TimePointsPerSecond<1||ItemPoints<1||MinSpawnInterval>SpawnInterval)throw new ArgumentOutOfRangeException(nameof(FloorRules));
        }
    }
    public readonly struct FloorBody
    {
        public readonly FloorVector Position;
        public readonly bool IsEnemy;
        public readonly DropPhase Phase;
        public readonly double Progress,Age;
        public bool Active=>Phase!=DropPhase.Inactive;
        public FloorBody(FloorVector p,bool enemy,DropPhase phase,double progress,double age){Position=p;IsEnemy=enemy;Phase=phase;Progress=progress;Age=age;}
    }
    public sealed class FloorSession
    {
        public const string GameId="tilt_floor";
        private sealed class Body{public FloorVector Position,Velocity;public DropPhase Phase;public double Age,WarningDuration,DropDuration;public bool Enemy;}
        private readonly FloorRules rules;
        private readonly Body[] bodies;
        private FloorVector velocity,tiltVelocity;
        private uint random;
        private double accumulator,nextDrop,nextBonus;
        private int spawnSerial;
        public FloorState State{get;private set;}=FloorState.Ready;
        public FloorVector Player{get;private set;}
        public FloorVector Tilt{get;private set;}
        public FloorVector LastCollectedAt{get;private set;}
        public double LastCollectedTime{get;private set;}=-10;
        public double Elapsed{get;private set;}
        public int Landed{get;private set;}
        public double LastLandingTime{get;private set;}=-10;
        public double HalfSize=>rules.HalfSize;
        public int Collected{get;private set;}
        public int Defeated{get;private set;}
        public int Spawned{get;private set;}
        public int BodyCount=>bodies.Length;
        public int Stage=>1+(int)(Elapsed/15);
        public int WaveSize=>Math.Min(12,1+(int)(Elapsed/20));
        public double CurrentSpawnInterval=>Math.Max(rules.MinSpawnInterval,rules.SpawnInterval/(1+Elapsed/rules.RampSeconds));
        public double CurrentWarningSeconds=>Math.Max(.28,rules.WarningSeconds/(1+Elapsed/50));
        public double CurrentDropSeconds=>Math.Max(.22,rules.DropSeconds/(1+Elapsed/100));
        public double CurrentEnemySpeed=>rules.EnemySpeed+Math.Min(2.5,Elapsed*.009);
        public double Danger=>Math.Min(1,Tilt.Length/rules.MaxTilt);
        public long SurvivalScore=>(long)Math.Min(long.MaxValue/2,Math.Floor(Elapsed*rules.TimePointsPerSecond+1e-7));
        public long BonusScore=>(long)Collected*rules.ItemPoints;
        public long Score=>SurvivalScore+BonusScore;
        public int ItemPoints=>rules.ItemPoints;
        public int EnemyCount{get{int n=0;foreach(var b in bodies)if(b.Enemy&&b.Phase!=DropPhase.Inactive&&b.Phase!=DropPhase.Ejected)n++;return n;}}
        public FloorSession(FloorRules rules=null,int seed=1)
        {
            this.rules=(rules??new FloorRules()).Copy();this.rules.Validate();random=(uint)seed;if(random==0)random=1;
            nextDrop=this.rules.FirstDrop;nextBonus=this.rules.FirstBonus;bodies=new Body[this.rules.Capacity];
            for(int i=0;i<bodies.Length;i++)bodies[i]=new Body{Enemy=i<this.rules.MaxEnemies};
        }
        public FloorBody BodyAt(int i)
        {
            var b=bodies[i];double progress=b.Phase==DropPhase.Warning?b.Age/b.WarningDuration:b.Phase==DropPhase.Falling?b.Age/b.DropDuration:b.Phase==DropPhase.Ejected?b.Age/.65:0;
            return new FloorBody(b.Position,b.Enemy,b.Phase,Math.Min(1,progress),b.Age);
        }
        private double Next(){random^=random<<13;random^=random>>17;random^=random<<5;return random/(double)uint.MaxValue;}
        private FloorVector RandomPoint(double ratio){double a=Next()*Math.PI*2,r=Math.Sqrt(Next())*rules.HalfSize*ratio;return new FloorVector(Math.Cos(a)*r,Math.Sin(a)*r);}
        public void Start(){if(State==FloorState.Ready)State=FloorState.Playing;}
        public void Tick(double seconds,FloorVector input)
        {
            if(double.IsNaN(seconds)||double.IsInfinity(seconds)||seconds<0||double.IsNaN(input.X)||double.IsNaN(input.Y)||double.IsInfinity(input.X)||double.IsInfinity(input.Y))throw new ArgumentOutOfRangeException(nameof(seconds));
            if(State!=FloorState.Playing)return;
            accumulator+=seconds;input=input.Limited(1);const double dt=1.0/120;
            while(accumulator+1e-10>=dt&&State==FloorState.Playing){accumulator-=dt;Advance(dt,input);}
        }
        private void SpawnWave()
        {
            for(int n=0;n<WaveSize;n++)
            {
                Body slot=null;for(int i=0;i<rules.MaxEnemies;i++)if(bodies[i].Phase==DropPhase.Inactive){slot=bodies[i];break;}
                if(slot==null)break;
                var point=RandomPoint(rules.SpawnRadiusRatio);
                if(++spawnSerial%3==0&&rules.SpawnRadiusRatio>0)point=(Player+velocity*(CurrentWarningSeconds+CurrentDropSeconds)+RandomPoint(.10)).Limited(rules.HalfSize*rules.SpawnRadiusRatio);
                slot.WarningDuration=CurrentWarningSeconds;slot.DropDuration=CurrentDropSeconds;slot.Position=point;slot.Velocity=new FloorVector(0,0);slot.Age=0;slot.Phase=DropPhase.Warning;Spawned++;
            }
        }
        private void SpawnBonus()
        {
            for(int i=rules.MaxEnemies;i<bodies.Length;i++)if(bodies[i].Phase==DropPhase.Inactive)
            {var b=bodies[i];b.Position=RandomPoint(rules.BonusRadiusRatio);b.Velocity=new FloorVector(0,0);b.Age=0;b.Phase=DropPhase.Alive;break;}
        }
        private void Advance(double dt,FloorVector input)
        {
            Elapsed+=dt;
            if(Elapsed+1e-9>=nextDrop){SpawnWave();nextDrop=Elapsed+CurrentSpawnInterval;}
            if(Elapsed+1e-9>=nextBonus){SpawnBonus();nextBonus=Elapsed+rules.BonusInterval;}
            var torque=Player*rules.TiltPerMeter;
            foreach(var b in bodies)if(b.Enemy&&b.Phase==DropPhase.Alive)torque+=b.Position*.7;
            torque=torque.Limited(rules.MaxTilt);tiltVelocity+=(torque-Tilt)*(18*dt)-tiltVelocity*(6*dt);Tilt=(Tilt+tiltVelocity*dt).Limited(rules.MaxTilt);
            var acceleration=input.Length>.05?(input*rules.RunSpeed-velocity)*7:velocity*(-2);
            velocity=(velocity+(acceleration+Tilt*.13)*dt).Limited(rules.RunSpeed+2);Player+=velocity*dt;
            if(Player.Length>rules.HalfSize+.12){State=FloorState.Fell;return;}
            foreach(var b in bodies)
            {
                if(b.Phase==DropPhase.Inactive)continue;b.Age+=dt;
                if(b.Phase==DropPhase.Warning){if(b.Age>=b.WarningDuration){b.Phase=DropPhase.Falling;b.Age=0;}continue;}
                if(b.Phase==DropPhase.Falling){if(b.Age>=b.DropDuration){b.Phase=DropPhase.Alive;b.Age=0;
                    if(b.Enemy)
                    {
                        // Apply once at touchdown; radius supplies the lever arm.
                        tiltVelocity=(tiltVelocity+b.Position*8).Limited(45);
                        Landed++;LastLandingTime=Elapsed;
                    }
                }else continue;}
                if(b.Phase==DropPhase.Ejected){if(b.Age>=.65)b.Phase=DropPhase.Inactive;continue;}
                if(b.Enemy)
                {
                    var toward=Player-b.Position;var desired=toward.Length>.001?toward*(CurrentEnemySpeed/toward.Length):new FloorVector(0,0);
                    b.Velocity=(b.Velocity+((desired-b.Velocity)*.9+Tilt*rules.EnemySlope)*dt).Limited(8);
                }
                else b.Velocity=(b.Velocity+(Tilt*.09-b.Velocity*1.2)*dt).Limited(2);
                b.Position+=b.Velocity*dt;
                if(b.Position.Length>rules.HalfSize+.15)
                {if(b.Enemy){b.Phase=DropPhase.Ejected;b.Age=0;Defeated++;}else b.Phase=DropPhase.Inactive;continue;}
                var delta=Player-b.Position;
                if(!b.Enemy)
                {
                    if(delta.Length<.48){b.Phase=DropPhase.Inactive;Collected++;LastCollectedAt=b.Position;LastCollectedTime=Elapsed;}
                    else if(b.Age>rules.BonusLifetime)b.Phase=DropPhase.Inactive;
                }
                else if(delta.Length<.57)
                {
                    State=FloorState.Hit;
                    return;
                }
            }
            if(Player.Length>rules.HalfSize+.12)State=FloorState.Fell;
        }
    }
}
