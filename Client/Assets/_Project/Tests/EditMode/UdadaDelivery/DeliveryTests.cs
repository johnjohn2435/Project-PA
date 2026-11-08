using System;
using NUnit.Framework;
using PocketArcade.UdadaDelivery;
namespace PocketArcade.Tests.UdadaDelivery
{
    public sealed class DeliveryTests
    {
        private static DeliverySession Started(DeliveryRules rules=null) {var s=new DeliverySession(rules);s.Start();return s;}
        private static void RunUntilAim(DeliverySession s,double offset=0)
        {
            int steps=0;
            while(s.PredictedLanding<s.HouseAt(s.ResolvedCount)+offset&&s.State==DeliveryState.Running&&steps++<10000)s.Tick(1.0/120,false);
            Assert.That(s.State,Is.EqualTo(DeliveryState.Running));Assert.That(steps,Is.LessThan(10000));
        }
        private static void Settle(DeliverySession s,bool braking=true)
        {int steps=0;while(s.ParcelInFlight&&steps++<2000)s.Tick(1.0/120,braking);Assert.That(s.ParcelInFlight,Is.False);}
        [Test] public void ReadyDoesNotAdvanceBeforeStart()
        {var s=new DeliverySession();s.Tick(2,false);Assert.That(s.Distance,Is.Zero);s.Start();s.Tick(.5,false);Assert.That(s.Speed,Is.GreaterThan(0));}
        [Test] public void BrakingLaunchesParcelAtCurrentMomentum()
        {var s=Started();s.Tick(1,false);double expected=s.PredictedLanding;s.Tick(1.0/120,true);Assert.That(s.ParcelInFlight,Is.True);Assert.That(s.Shots,Is.EqualTo(1));Settle(s);Assert.That(s.LastLandingX,Is.EqualTo(expected).Within(1e-7));}
        [Test] public void HeldBrakeCannotRepeatLaunch()
        {var s=Started();RunUntilAim(s);s.Tick(1.0/120,true);s.Tick(10,true);Assert.That(s.Shots,Is.EqualTo(1));Assert.That(s.ResolvedCount,Is.EqualTo(1));}
        [Test] public void PressWithoutSpeedDoesNotConsumeParcelOrHouse()
        {var s=Started();s.Tick(1,true);Assert.That(s.Shots,Is.Zero);Assert.That(s.ResolvedCount,Is.Zero);Assert.That(s.Distance,Is.Zero);}
        [Test] public void ReleaseAcceleratesAgain()
        {var s=Started();s.Tick(1,false);s.Tick(1,true);Assert.That(s.Speed,Is.Zero);s.Tick(.5,false);Assert.That(s.Speed,Is.GreaterThan(0));}
        [Test] public void CenteredLandingIsPerfectAndPaysOnce()
        {var s=Started();RunUntilAim(s);s.Tick(1.0/120,true);Settle(s);Assert.That(s.LastOutcome,Is.EqualTo(DeliveryOutcome.Perfect));Assert.That(s.Deliveries,Is.EqualTo(1));Assert.That(s.Score,Is.EqualTo(300));s.Tick(.1,true);Assert.That(s.Score,Is.EqualTo(300));}
        [Test] public void LandingInsideDoorOutsideCenterIsNormalDelivery()
        {var s=Started();RunUntilAim(s,.65);s.Tick(1.0/120,true);Settle(s);Assert.That(s.LastOutcome,Is.EqualTo(DeliveryOutcome.Delivered));Assert.That(s.Score,Is.EqualTo(150));}
        [TestCase(-2,DeliveryOutcome.TooShort)][TestCase(2,DeliveryOutcome.TooFar)]
        public void TimingErrorReportsWhichDirectionToAdjust(double offset,DeliveryOutcome expected)
        {var s=Started();RunUntilAim(s,offset);s.Tick(1.0/120,true);Settle(s);Assert.That(s.LastOutcome,Is.EqualTo(expected));Assert.That(s.Score,Is.Zero);Assert.That(s.Misses,Is.EqualTo(1));}
        [Test] public void MovingRunnerDoesNotChangeAirborneParcelsLanding()
        {var s=Started();RunUntilAim(s);double expected=s.PredictedLanding;s.Tick(1.0/120,true);Settle(s,false);Assert.That(s.LastLandingX,Is.EqualTo(expected).Within(1e-7));Assert.That(s.LastOutcome,Is.EqualTo(DeliveryOutcome.Perfect));}
        [Test] public void BrakingDuringFlightDoesNotLaunchIntoNextHouse()
        {var s=Started();RunUntilAim(s);s.Tick(1.0/120,true);s.Tick(.2,false);s.Tick(.1,true);Assert.That(s.Shots,Is.EqualTo(1));Assert.That(s.ParcelInFlight,Is.True);}
        [Test] public void HeavyLoadChangesSlideDistanceAndRunnerBraking()
        {
            var s=Started();RunUntilAim(s);s.Tick(1.0/120,true);Settle(s);Assert.That(s.Load,Is.EqualTo(2));s.Tick(1,false);
            double heavyRange=s.PredictedLanding-s.Distance;
            var light=Started();light.Tick(1,false);double lightRange=light.PredictedLanding-light.Distance;
            Assert.That(heavyRange,Is.GreaterThan(lightRange));s.Tick(.1,true);light.Tick(.1,true);Assert.That(s.Speed,Is.GreaterThan(light.Speed));
        }
        [Test] public void ThreePerfectLandingsFinishCourseWithStreakBonus()
        {
            var s=Started();for(int i=0;i<3;i++){RunUntilAim(s);s.Tick(1.0/120,true);Settle(s);}
            Assert.That(s.State,Is.EqualTo(DeliveryState.Finished));Assert.That(s.PerfectDeliveries,Is.EqualTo(3));Assert.That(s.Score,Is.EqualTo(1050));Assert.That(s.Coins,Is.EqualTo(10));
        }
        [Test] public void MissResetsLoad()
        {var s=Started();RunUntilAim(s);s.Tick(1.0/120,true);Settle(s);Assert.That(s.Load,Is.EqualTo(2));RunUntilAim(s,-2);s.Tick(1.0/120,true);Settle(s);Assert.That(s.Load,Is.EqualTo(1));}
        [Test] public void NoInputMissesExactlyThreeHousesAndFinishes()
        {var s=Started();s.Tick(100,false);Assert.That(s.ResolvedCount,Is.EqualTo(3));Assert.That(s.Misses,Is.EqualTo(3));Assert.That(s.State,Is.EqualTo(DeliveryState.Finished));Assert.That(s.Score,Is.Zero);}
        [Test] public void TimerAlsoEndsStationaryRun()
        {var s=Started(new DeliveryRules{Duration=1});s.Tick(2,true);Assert.That(s.Remaining,Is.Zero);Assert.That(s.State,Is.EqualTo(DeliveryState.Finished));Assert.That(s.Misses,Is.EqualTo(3));}
        [Test] public void FinishedRunCannotAddScoreOrShots()
        {var s=Started();s.Tick(100,false);double distance=s.Distance;s.Tick(10,true);s.Start();Assert.That(s.Distance,Is.EqualTo(distance));Assert.That(s.Shots,Is.Zero);}
        [Test] public void LargeDeltaMatchesFixedStepLanding()
        {
            var a=Started();var b=Started();a.Tick(1,false);b.Tick(1,false);a.Tick(4,true);for(int i=0;i<480;i++)b.Tick(1.0/120,true);
            Assert.That(a.LastLandingX,Is.EqualTo(b.LastLandingX).Within(1e-7));Assert.That(a.Score,Is.EqualTo(b.Score));Assert.That(a.LastOutcome,Is.EqualTo(b.LastOutcome));
        }
        [Test] public void SessionOwnsRulesSnapshot()
        {var rules=new DeliveryRules();var s=new DeliverySession(rules);rules.FirstHouse=999;Assert.That(s.HouseAt(0),Is.EqualTo(9));}
        [Test] public void InvalidRulesAndElapsedAreRejected()
        {
            Assert.Throws<ArgumentOutOfRangeException>(()=>new DeliverySession(new DeliveryRules{ParcelDrag=0}));
            Assert.Throws<ArgumentOutOfRangeException>(()=>new DeliverySession(new DeliveryRules{HouseCount=0}));
            Assert.Throws<ArgumentOutOfRangeException>(()=>new DeliverySession().Tick(double.NaN,false));
        }
    }
}
