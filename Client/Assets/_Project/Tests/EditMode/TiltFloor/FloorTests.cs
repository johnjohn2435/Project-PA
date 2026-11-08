using System;
using NUnit.Framework;
using PocketArcade.TiltFloor;
namespace PocketArcade.Tests.TiltFloor
{
    public sealed class FloorTests
    {
        private static FloorRules Quiet()=>new FloorRules{FirstDrop=10000,FirstBonus=10000};
        private static FloorSession Playing(FloorRules rules=null,int seed=1){var s=new FloorSession(rules??Quiet(),seed);s.Start();return s;}
        [Test] public void ReadyDoesNotAdvanceOrScore(){var s=new FloorSession();s.Tick(10,new FloorVector(1,0));Assert.That(s.Elapsed,Is.Zero);Assert.That(s.Score,Is.Zero);Assert.That(s.Spawned,Is.Zero);}
        [Test] public void SurvivalTimeContinuouslyAddsScore(){var s=Playing();s.Tick(5,new FloorVector(0,0));Assert.That(s.Score,Is.EqualTo(50));Assert.That(s.BonusScore,Is.Zero);}
        [Test] public void MovingRightLoadsAndTiltsRight(){var s=Playing();s.Tick(.7,new FloorVector(1,0));Assert.That(s.Player.X,Is.GreaterThan(1));Assert.That(s.Tilt.X,Is.GreaterThan(1));}
        [Test] public void CrossingCenterCanReverseTilt(){var s=Playing();s.Tick(.5,new FloorVector(1,0));s.Tick(1,new FloorVector(-1,0));s.Tick(.3,new FloorVector(0,0));Assert.That(s.Player.X,Is.LessThan(0));Assert.That(s.Tilt.X,Is.LessThan(0));}
        [Test] public void CircularEdgeRejectsCornersThatUsedToBeSafe(){var s=Playing();s.Tick(2,new FloorVector(1,1));Assert.That(s.State,Is.EqualTo(FloorState.Fell));Assert.That(s.Player.Length,Is.GreaterThan(s.HalfSize));Assert.That(Math.Abs(s.Player.X),Is.LessThan(s.HalfSize));}
        [Test] public void NoAutomaticWinWhenNoItemsRemain(){var s=Playing();s.Tick(20,new FloorVector(0,0));Assert.That(s.State,Is.EqualTo(FloorState.Playing));}
        [Test] public void WarningAlwaysPrecedesFallingAndLanding()
        {
            var s=Playing(new FloorRules{FirstDrop=.05,SpawnInterval=100,WarningSeconds=.4,DropSeconds=.4,SpawnRadiusRatio=0,FirstBonus=100});
            s.Tick(.1,new FloorVector(0,0));Assert.That(s.BodyAt(0).Phase,Is.EqualTo(DropPhase.Warning));s.Tick(.4,new FloorVector(0,0));Assert.That(s.BodyAt(0).Phase,Is.EqualTo(DropPhase.Falling));s.Tick(.4,new FloorVector(0,0));Assert.That(s.BodyAt(0).Phase,Is.EqualTo(DropPhase.Alive));
        }
        [Test] public void AirborneEnemyDoesNotPushPlayer(){var s=Playing(new FloorRules{FirstDrop=.05,SpawnInterval=100,WarningSeconds=1,DropSeconds=1,SpawnRadiusRatio=0,FirstBonus=100});s.Tick(1.5,new FloorVector(0,0));Assert.That(s.Player.Length,Is.Zero);}
        [Test] public void BonusAddsItsOwnScoreExactlyOnce()
        {var s=Playing(new FloorRules{FirstDrop=100,FirstBonus=.05,BonusInterval=100,BonusRadiusRatio=0});s.Tick(.1,new FloorVector(0,0));Assert.That(s.Collected,Is.EqualTo(1));Assert.That(s.BonusScore,Is.EqualTo(250));s.Tick(1,new FloorVector(0,0));Assert.That(s.BonusScore,Is.EqualTo(250));Assert.That(s.Score,Is.EqualTo(s.SurvivalScore+250));}
        [Test] public void DifficultyAddsMoreEnemiesAtShorterIntervals(){var s=Playing();double start=s.CurrentSpawnInterval;double speed=s.CurrentEnemySpeed;s.Tick(100,new FloorVector(0,0));Assert.That(s.CurrentSpawnInterval,Is.LessThan(start/4));Assert.That(s.WaveSize,Is.GreaterThan(4));Assert.That(s.CurrentEnemySpeed,Is.GreaterThan(speed));}
        [Test] public void EnemyPoolHasAHardBound(){var rules=new FloorRules{FirstDrop=.01,SpawnInterval=.02,MinSpawnInterval=.01,WarningSeconds=100,MaxEnemies=4,FirstBonus=100};var s=Playing(rules);s.Tick(5,new FloorVector(0,0));Assert.That(s.EnemyCount,Is.EqualTo(4));Assert.That(s.Spawned,Is.EqualTo(4));Assert.That(s.BodyCount,Is.EqualTo(7));}
        [Test] public void DeathFreezesTimeScoreAndSpawns(){var s=Playing(new FloorRules());s.Tick(5,new FloorVector(1,0));Assert.That(s.State,Is.EqualTo(FloorState.Fell));long score=s.Score;double elapsed=s.Elapsed;int spawned=s.Spawned;s.Tick(10,new FloorVector(0,0));s.Start();Assert.That(s.Score,Is.EqualTo(score));Assert.That(s.Elapsed,Is.EqualTo(elapsed));Assert.That(s.Spawned,Is.EqualTo(spawned));}
        [Test] public void LargeDeltaMatchesFixedSteps(){var a=Playing(new FloorRules(),3);var b=Playing(new FloorRules(),3);a.Tick(4,new FloorVector(0,0));for(int i=0;i<480;i++)b.Tick(1.0/120,new FloorVector(0,0));Assert.That(a.Score,Is.EqualTo(b.Score));Assert.That(a.Spawned,Is.EqualTo(b.Spawned));Assert.That(a.BodyAt(0).Position.X,Is.EqualTo(b.BodyAt(0).Position.X).Within(1e-8));}
        [Test] public void DiagonalInputDoesNotMoveFaster(){var a=Playing();var b=Playing();a.Tick(.25,new FloorVector(1,1));b.Tick(.25,new FloorVector(1,0));Assert.That(a.Player.Length,Is.EqualTo(b.Player.Length).Within(.001));}
        [Test] public void RulesAreCopied(){var rules=Quiet();var s=new FloorSession(rules);rules.HalfSize=999;Assert.That(s.HalfSize,Is.EqualTo(4));}
        [Test] public void SpawnSeedIsRepeatable(){var a=Playing(new FloorRules(),78);var b=Playing(new FloorRules(),78);a.Tick(3,new FloorVector(0,0));b.Tick(3,new FloorVector(0,0));Assert.That(a.BodyAt(0).Position.X,Is.EqualTo(b.BodyAt(0).Position.X));Assert.That(a.BodyAt(0).Position.Y,Is.EqualTo(b.BodyAt(0).Position.Y));}
        [Test] public void InvalidRulesAndInputsAreRejected(){Assert.Throws<ArgumentOutOfRangeException>(()=>new FloorSession(new FloorRules{MaxEnemies=0}));Assert.Throws<ArgumentOutOfRangeException>(()=>Playing().Tick(.1,new FloorVector(double.NaN,0)));}
        [Test] public void TiltingCanEjectEnemiesAndRecycleSpawnSlots()
        {
            var s=Playing(new FloorRules{MaxEnemies=4,FirstDrop=.1,SpawnInterval=.2,MinSpawnInterval=.1,WarningSeconds=.3,DropSeconds=.1,EnemySpeed=.2},1);
            for(int i=0;i<7200&&s.State==FloorState.Playing;i++)
            {
                double angle=s.Elapsed*.7;
                var target=new FloorVector(Math.Cos(angle)*1.6,Math.Sin(angle)*1.6);
                s.Tick(1.0/120,(target-s.Player)*2-s.Tilt*.02);
            }
            Assert.That(s.Defeated,Is.GreaterThan(0));Assert.That(s.Spawned,Is.GreaterThan(4));Assert.That(s.BodyCount,Is.EqualTo(7));Assert.That(s.Score,Is.EqualTo(s.SurvivalScore+s.BonusScore));
        }
        [Test] public void LaterWavesShortenWarningsWithoutRemovingThem()
        {
            var s=Playing();double earlyWarning=s.CurrentWarningSeconds,earlyDrop=s.CurrentDropSeconds;
            s.Tick(200,new FloorVector(0,0));Assert.That(s.CurrentWarningSeconds,Is.LessThan(earlyWarning));Assert.That(s.CurrentWarningSeconds,Is.GreaterThanOrEqualTo(.28));Assert.That(s.CurrentDropSeconds,Is.LessThan(earlyDrop));
        }
        [Test] public void LandingAppliesDirectionalImpulseExactlyOnce()
        {
            var s=Playing(new FloorRules{FirstDrop=.05,SpawnInterval=100,WarningSeconds=.4,DropSeconds=.4,FirstBonus=100},78);
            while(s.BodyAt(0).Phase!=DropPhase.Alive&&s.Elapsed<2)s.Tick(1.0/120,new FloorVector(0,0));
            Assert.That(s.State,Is.EqualTo(FloorState.Playing));
            Assert.That(s.Landed,Is.EqualTo(1));Assert.That(s.LastLandingTime,Is.EqualTo(s.Elapsed));
            var point=s.BodyAt(0).Position;
            s.Tick(1.0/120,new FloorVector(0,0));
            Assert.That(s.Tilt.X*point.X+s.Tilt.Y*point.Y,Is.GreaterThan(.01));
            s.Tick(.1,new FloorVector(0,0));Assert.That(s.Landed,Is.EqualTo(1));
        }
        [Test] public void AirborneEnemyHasNoLandingImpactAndRetryStartsClean()
        {
            var rules=new FloorRules{FirstDrop=.05,SpawnInterval=100,WarningSeconds=.4,DropSeconds=.4,FirstBonus=100};
            var s=Playing(rules);s.Tick(.5,new FloorVector(0,0));
            Assert.That(s.Landed,Is.Zero);Assert.That(s.Tilt.Length,Is.Zero);
            s.Tick(.5,new FloorVector(0,0));Assert.That(s.Landed,Is.EqualTo(1));
            var retry=Playing(rules);Assert.That(retry.Landed,Is.Zero);Assert.That(retry.LastLandingTime,Is.LessThan(0));
        }
        [Test] public void LandedEnemyContactImmediatelyEndsRunAndFreezesScore()
        {
            var s=Playing(new FloorRules{FirstDrop=.05,SpawnInterval=100,WarningSeconds=.4,DropSeconds=.4,SpawnRadiusRatio=0,FirstBonus=100});
            s.Tick(.3,new FloorVector(0,0));Assert.That(s.State,Is.EqualTo(FloorState.Playing));
            s.Tick(.8,new FloorVector(0,0));Assert.That(s.State,Is.EqualTo(FloorState.Hit));
            long score=s.Score;double elapsed=s.Elapsed;s.Tick(10,new FloorVector(1,0));Assert.That(s.Score,Is.EqualTo(score));Assert.That(s.Elapsed,Is.EqualTo(elapsed));
        }
    }
}
