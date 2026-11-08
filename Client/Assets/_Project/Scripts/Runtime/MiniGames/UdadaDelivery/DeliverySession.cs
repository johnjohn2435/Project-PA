using System;

namespace PocketArcade.UdadaDelivery
{
    public enum DeliveryState { Ready, Running, Finished }
    public enum DeliveryOutcome { Pending, Perfect, Delivered, TooShort, TooFar, Skipped }
    public sealed class DeliveryRules
    {
        public double Duration = 45, RunSpeed = 4, Acceleration = 12, Deceleration = 8;
        public double ParcelDrag = 2, MinimumLaunchSpeed = 2.5, DoorTolerance = 1, PerfectTolerance = .3;
        public double FirstHouse = 9, HouseSpacing = 14;
        public int HouseCount = 3;
        public void Validate()
        {
            foreach (double value in new[] { Duration, RunSpeed, Acceleration, Deceleration, ParcelDrag, MinimumLaunchSpeed,
                DoorTolerance, PerfectTolerance, FirstHouse, HouseSpacing })
                if (double.IsNaN(value) || double.IsInfinity(value) || value <= 0) throw new ArgumentOutOfRangeException(nameof(value));
            if (MinimumLaunchSpeed >= RunSpeed || PerfectTolerance > DoorTolerance || HouseCount < 1 || HouseCount > 20 ||
                HouseSpacing < DoorTolerance * 3) throw new ArgumentOutOfRangeException(nameof(DeliveryRules));
        }
        internal DeliveryRules Copy() => (DeliveryRules)MemberwiseClone();
    }
    // Fixed-step rules. Rendering, prefab art and Unity physics never determine a delivery.
    public sealed class DeliverySession
    {
        // A new score key prevents old obstacle-run scores from competing with three-house scores.
        public const string GameId = "udada_delivery", ScoreId = "udada_brake.v1";
        private readonly DeliveryRules rules;
        private readonly DeliveryOutcome[] outcomes;
        private double accumulator, parcelStart, parcelInitialSpeed, parcelDrag, parcelFlightTime, parcelAge;
        private bool wasBraking;
        private int parcelHouse, shotLoad;
        public DeliveryState State { get; private set; } = DeliveryState.Ready;
        public double Elapsed { get; private set; }
        public double Remaining => Math.Max(0, rules.Duration - Elapsed);
        public double Distance { get; private set; }
        public double Speed { get; private set; }
        public int HouseCount => outcomes.Length;
        public int ResolvedCount { get; private set; }
        public int Deliveries { get; private set; }
        public int PerfectDeliveries { get; private set; }
        public int Misses => ResolvedCount - Deliveries;
        public int Streak { get; private set; }
        public int Load => Math.Min(3, 1 + Streak);
        public int Shots { get; private set; }
        public long Score { get; private set; }
        public int Coins => Math.Min(100, (int)(Score / 100));
        public bool ParcelInFlight { get; private set; }
        public double ParcelX { get; private set; }
        public double ParcelHeight => ParcelInFlight ? .55 * (1 - FlightProgress) + .7 * Math.Sin(Math.PI * FlightProgress) : 0;
        public double FlightProgress => ParcelInFlight ? Math.Min(1, parcelAge / parcelFlightTime) : 1;
        public double LastLandingX { get; private set; }
        public DeliveryOutcome LastOutcome { get; private set; }
        public double MinimumLaunchSpeed => rules.MinimumLaunchSpeed;
        public double DoorTolerance => rules.DoorTolerance;
        public double PerfectTolerance => rules.PerfectTolerance;
        public double PredictedLanding => Distance + Speed * Speed / (2 * DragForLoad(Load));
        public double TargetDistance => ResolvedCount < HouseCount ? HouseAt(ResolvedCount) - Distance : 0;
        public double SegmentProgress => (double)ResolvedCount / HouseCount;
        public DeliverySession(DeliveryRules rules = null)
        {
            this.rules = (rules ?? new DeliveryRules()).Copy(); this.rules.Validate();
            outcomes = new DeliveryOutcome[this.rules.HouseCount];
        }
        public double HouseAt(int index)
        { if (index < 0 || index >= HouseCount) throw new ArgumentOutOfRangeException(nameof(index)); return rules.FirstHouse + index * rules.HouseSpacing; }
        public DeliveryOutcome OutcomeAt(int index) => outcomes[index];
        public void Start() { if (State == DeliveryState.Ready) State = DeliveryState.Running; }
        public void ResetBrakeLatch() => wasBraking = false;
        private double DragForLoad(int load) => rules.ParcelDrag / (1 + (load - 1) * .2);
        public void Tick(double seconds, bool braking)
        {
            if (double.IsNaN(seconds) || double.IsInfinity(seconds) || seconds < 0) throw new ArgumentOutOfRangeException(nameof(seconds));
            if (State != DeliveryState.Running) return;
            accumulator += seconds;
            const double step = 1.0 / 120;
            while (accumulator + 1e-10 >= step && State == DeliveryState.Running)
            { accumulator -= step; Advance(step, braking); }
        }
        private void Advance(double dt, bool braking)
        {
            Elapsed = Math.Min(rules.Duration, Elapsed + dt);
            // A press creates one launch. Continuing to hold or braking during flight cannot launch again.
            if (braking && !wasBraking && !ParcelInFlight && ResolvedCount < HouseCount && Speed >= rules.MinimumLaunchSpeed)
            {
                shotLoad = Load; parcelHouse = ResolvedCount; parcelStart = Distance; parcelInitialSpeed = Speed;
                parcelDrag = DragForLoad(shotLoad); parcelFlightTime = parcelInitialSpeed / parcelDrag; parcelAge = 0;
                ParcelX = Distance; ParcelInFlight = true; Shots++;
            }
            wasBraking = braking;
            double target = braking ? 0 : rules.RunSpeed;
            double change = (braking ? rules.Deceleration / (1 + (Load - 1) * .25) : rules.Acceleration) * dt;
            Speed = Speed < target ? Math.Min(target, Speed + change) : Math.Max(target, Speed - change);
            Distance += Speed * dt;
            if (ParcelInFlight)
            {
                parcelAge = Math.Min(parcelFlightTime, parcelAge + dt);
                ParcelX = parcelStart + parcelInitialSpeed * parcelAge - .5 * parcelDrag * parcelAge * parcelAge;
                if (parcelAge >= parcelFlightTime - 1e-9)
                {
                    ParcelInFlight = false; LastLandingX = ParcelX;
                    double error = ParcelX - HouseAt(parcelHouse);
                    var outcome = Math.Abs(error) <= rules.PerfectTolerance ? DeliveryOutcome.Perfect :
                        Math.Abs(error) <= rules.DoorTolerance ? DeliveryOutcome.Delivered : error < 0 ? DeliveryOutcome.TooShort : DeliveryOutcome.TooFar;
                    Resolve(outcome);
                }
            }
            // Passing a house without launching consumes that house, but an in-flight parcel gets to finish.
            while (!ParcelInFlight && ResolvedCount < HouseCount && Distance > HouseAt(ResolvedCount) + rules.DoorTolerance + 1)
                Resolve(DeliveryOutcome.Skipped);
            if (Remaining <= 1e-9)
            {
                Elapsed = rules.Duration; ParcelInFlight = false;
                while (ResolvedCount < HouseCount) Resolve(DeliveryOutcome.Skipped);
            }
            if (ResolvedCount == HouseCount) State = DeliveryState.Finished;
        }
        private void Resolve(DeliveryOutcome outcome)
        {
            outcomes[ResolvedCount++] = outcome; LastOutcome = outcome;
            if (outcome == DeliveryOutcome.Perfect || outcome == DeliveryOutcome.Delivered)
            {
                Deliveries++; Streak++; if (outcome == DeliveryOutcome.Perfect) PerfectDeliveries++;
                Score += (outcome == DeliveryOutcome.Perfect ? 300 : 150) + Math.Min(2, Streak - 1) * 50;
            }
            else Streak = 0;
        }
    }
}
