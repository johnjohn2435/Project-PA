using System;
using NUnit.Framework;
using PocketArcade.EchoRoom;
namespace PocketArcade.Tests.EchoRoom
{
    public sealed class EchoTests
    {
        private static EchoSession Playing(EchoRules rules=null){var s=new EchoSession(rules??new EchoRules{Target=99});s.Start();return s;}
        private static void MoveTo(EchoSession s,EchoPoint target)
        {int count=0;while((s.ActorAt(0).Position-target).Length>.04&&count++<1000&&s.State==EchoState.Playing)s.Tick(1.0/120,(target-s.ActorAt(0).Position).Limited(1));Assert.That(count,Is.LessThan(1000));}
        [Test] public void ReadyDoesNotRecordOrAdvance(){var s=new EchoSession();s.Tick(20,new EchoPoint(1,0));Assert.That(s.Elapsed,Is.Zero);Assert.That(s.GhostCount,Is.Zero);}
        [Test] public void TenSecondsAddsOneGhostAndResetsPlayer(){var s=Playing();s.Tick(9.99,new EchoPoint(0,0));Assert.That(s.GhostCount,Is.Zero);s.Tick(.02,new EchoPoint(0,0));Assert.That(s.GhostCount,Is.EqualTo(1));Assert.That(s.Loop,Is.EqualTo(2));Assert.That(s.ActorAt(0).Position.Length,Is.Zero);}
        [Test] public void GhostReplaysExactRecordedPositions()
        {
            var s=Playing(new EchoRules{LoopSeconds=1,Target=99});var expected=new EchoPoint[31];
            for(int i=1;i<=30;i++){s.Tick(1.0/120,new EchoPoint(.4,.7));expected[i]=s.ActorAt(0).Position;}
            s.Tick(.75,new EchoPoint(0,0));
            for(int i=1;i<=30;i++){s.Tick(1.0/120,new EchoPoint(0,0));Assert.That(s.ActorAt(1).Position.X,Is.EqualTo(expected[i].X).Within(1e-9));Assert.That(s.ActorAt(1).Position.Y,Is.EqualTo(expected[i].Y).Within(1e-9));}
        }
        [Test] public void ReplayedRouteAutomaticallyDeliversInNextLoop()
        {
            var s=Playing(new EchoRules{Target=99,Parcels=new[]{new EchoPoint(0,1.2)}});s.Tick(.35,new EchoPoint(0,1));s.Tick(.35,new EchoPoint(0,-1));Assert.That(s.Delivered,Is.EqualTo(1));
            s.Tick(9.3,new EchoPoint(0,0));s.Tick(.7,new EchoPoint(0,0));Assert.That(s.Delivered,Is.EqualTo(2));Assert.That(s.GhostDeliveries,Is.EqualTo(1));
        }
        [Test] public void SharedParcelCannotBeDeliveredTwiceByTwoActors()
        {
            var s=Playing(new EchoRules{Target=99,Parcels=new[]{new EchoPoint(0,1.2)}});s.Tick(.35,new EchoPoint(0,1));s.Tick(.35,new EchoPoint(0,-1));s.Tick(9.3,new EchoPoint(0,0));s.Tick(.35,new EchoPoint(0,1));s.Tick(.35,new EchoPoint(0,-1));Assert.That(s.Delivered,Is.EqualTo(2));
        }
        [Test] public void CanCarryOnlyOneParcel(){var s=Playing(new EchoRules{Target=99,Parcels=new[]{new EchoPoint(0,1.2),new EchoPoint(0,1.21)}});s.Tick(.35,new EchoPoint(0,1));Assert.That(s.ActorAt(0).Carrying,Is.True);Assert.That(s.ParcelAvailable(0),Is.False);Assert.That(s.ParcelAvailable(1),Is.True);}
        [Test] public void UnreturnedCargoIsNotCountedAtReset(){var s=Playing(new EchoRules{Target=99,Parcels=new[]{new EchoPoint(0,2.65)}});s.Tick(.7,new EchoPoint(0,1));Assert.That(s.ActorAt(0).Carrying,Is.True);s.Tick(9.3,new EchoPoint(0,0));Assert.That(s.Delivered,Is.Zero);Assert.That(s.ActorAt(0).Carrying,Is.False);Assert.That(s.ParcelAvailable(0),Is.True);}
        [Test] public void ClosedDoorBlocksPassage(){var s=Playing();s.Tick(.7,new EchoPoint(1,0));Assert.That(s.ActorAt(0).Position.X,Is.LessThan(1.1));}
        [Test] public void PlateOpensCorrectDoorAndExpiresAfterLeaving(){var s=Playing();MoveTo(s,s.PlateAt(0));Assert.That(s.GateOpen(0),Is.True);Assert.That(s.GateOpen(1),Is.False);MoveTo(s,new EchoPoint(0,0));s.Tick(4.2,new EchoPoint(0,0));Assert.That(s.GateOpen(0),Is.False);}
        [Test] public void PlateAllowsActualPassageThroughSideGate(){var s=Playing();MoveTo(s,s.PlateAt(1));MoveTo(s,new EchoPoint(0,0));MoveTo(s,new EchoPoint(2.2,0));Assert.That(s.ActorAt(0).Position.X,Is.GreaterThan(2));}
        [Test] public void GhostCanHoldPlateForCurrentPlayer(){var s=Playing();MoveTo(s,s.PlateAt(0));s.Tick(10-s.LoopElapsed,new EchoPoint(0,0));s.Tick(.8,new EchoPoint(0,0));Assert.That(s.GateOpen(0),Is.True);Assert.That(s.ActorAt(0).Position.Length,Is.Zero);}
        [Test] public void GhostCollisionDoesNotModifyRecordedPath(){var s=Playing(new EchoRules{LoopSeconds=1,Speed=1.5,Target=99});s.Tick(1,new EchoPoint(0,1));s.Tick(.8,new EchoPoint(0,1));Assert.That(s.ActorAt(1).Position.Y,Is.EqualTo(1.2).Within(1e-8));Assert.That((s.ActorAt(0).Position-s.ActorAt(1).Position).Length,Is.GreaterThanOrEqualTo(.36));}
        [Test] public void SafeHomeCannotBeBlockedByOverlappingGhosts(){var s=Playing();s.Tick(50,new EchoPoint(0,0));Assert.That(s.GhostCount,Is.EqualTo(5));Assert.That(s.ActorAt(0).Position.Length,Is.Zero);}
        [Test] public void FinalRoundEndsWithoutCreatingUnboundedGhosts(){var s=Playing();s.Tick(1000,new EchoPoint(0,0));Assert.That(s.State,Is.EqualTo(EchoState.TimeUp));Assert.That(s.GhostCount,Is.EqualTo(5));Assert.That(s.Elapsed,Is.EqualTo(60));Assert.That(s.Remaining,Is.Zero);}
        [Test] public void GoalFinishesEarlyAndFreezesRecording(){var s=Playing(new EchoRules{Target=1,Parcels=new[]{new EchoPoint(0,1.2)}});s.Tick(.35,new EchoPoint(0,1));s.Tick(.35,new EchoPoint(0,-1));Assert.That(s.State,Is.EqualTo(EchoState.Complete));double time=s.Elapsed;s.Tick(100,new EchoPoint(1,0));Assert.That(s.Elapsed,Is.EqualTo(time));}
        [Test] public void LargeDeltaMatchesFixedSteps(){var a=Playing();var b=Playing();a.Tick(12,new EchoPoint(0,0));for(int i=0;i<1440;i++)b.Tick(1.0/120,new EchoPoint(0,0));Assert.That(a.GhostCount,Is.EqualTo(b.GhostCount));Assert.That(a.LoopElapsed,Is.EqualTo(b.LoopElapsed));}
        [Test] public void RulesAndParcelPositionsAreCopied(){var rules=new EchoRules();var s=new EchoSession(rules);rules.Parcels[0]=new EchoPoint(0,0);Assert.That(s.ParcelAt(0).Length,Is.GreaterThan(1));}
        [Test] public void InvalidRulesAndInputsAreRejected(){Assert.Throws<ArgumentOutOfRangeException>(()=>new EchoSession(new EchoRules{Rounds=100}));Assert.Throws<ArgumentOutOfRangeException>(()=>Playing().Tick(.1,new EchoPoint(double.NaN,0)));}
        [Test] public void DifferentFirstTwoRoutesEnableGhostCooperationToFinish()
        {
            var s=new EchoSession();s.Start();
            void Go(double x,double y)
            {
                int guard=0;var target=new EchoPoint(x,y);
                while((target-s.ActorAt(0).Position).Length>.08&&guard++<1200&&s.State==EchoState.Playing){var d=target-s.ActorAt(0).Position;s.Tick(1.0/120,d*(1/d.Length));}
                Assert.That(guard,Is.LessThan(1200));
            }
            void Side(int sign)
            {
                for(int side=0;side<2;side++){Go(sign>0?-.65:.65,sign>0?1.1:-1.1);Go(0,0);Go(sign*2.6,0);Go(sign*2.6,side==0?2:-2);Go(sign*2.6,0);Go(0,0);}
                Go(0,sign>0?2.65:-2.65);Go(0,0);
            }
            Side(1);s.Tick(10-s.LoopElapsed,new EchoPoint(0,0));Side(-1);s.Tick(60,new EchoPoint(0,0));
            Assert.That(s.State,Is.EqualTo(EchoState.Complete));Assert.That(s.Delivered,Is.GreaterThanOrEqualTo(18));Assert.That(s.GhostDeliveries,Is.GreaterThanOrEqualTo(12));Assert.That(s.Elapsed,Is.LessThan(60));
        }
    }
}
