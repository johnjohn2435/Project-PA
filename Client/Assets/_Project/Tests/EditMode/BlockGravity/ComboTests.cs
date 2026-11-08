using System;
using System.Linq;
using NUnit.Framework;
using PocketArcade.BlockGravity;
namespace PocketArcade.Tests.BlockGravity
{
    public sealed class ComboTests
    {
        private sealed class Source : IPieceSource
        {
            public PieceDefinition Piece = new PieceDefinition("single",1,new Cell(0,0));
            public PieceDefinition Next() => Piece;
        }
        private static BlockGravitySession Start()
        { var b=new BoardState(2,8);b[0,0]=1;return new BlockGravitySession(new BlockGravityConfig(2,8,100),new Source(),b); }
        private static TurnResult Place(BlockGravitySession s,int slot,int x)
        { Assert.That(s.TryPlace(slot,new Cell(x,7),out var result),Is.EqualTo(PlacementFailure.None));s.CompletePresentation();return result; }
        [Test] public void ClearInEachFixedTrayKeepsComboAcrossFourMisses()
        {
            var s=Start();Place(s,0,1);Assert.That(s.Combo,Is.EqualTo(1));
            for(int t=1;t<=4;t++){Place(s,t%3,0);Assert.That(s.Combo,Is.EqualTo(1));}
            var result=Place(s,2,1);Assert.That(s.Combo,Is.EqualTo(2));Assert.That(result.ScoreDelta,Is.EqualTo(210));
            Assert.That(result.Frames.Single(f=>f.Stage==TurnStage.ClearingLines).Combo,Is.EqualTo(2));
        }
        [Test] public void EmptyTrayWithoutClearResetsOnlyOnThirdPiece()
        {
            var s=Start();Place(s,0,1);Place(s,1,0);Place(s,2,0);
            Place(s,0,0);Place(s,1,0);Assert.That(s.Combo,Is.EqualTo(1));
            var result=Place(s,2,0);Assert.That(s.Combo,Is.Zero);
            Assert.That(result.Frames.Single(f=>f.Stage==TurnStage.RefillTray).Combo,Is.Zero);
            Assert.That(Place(s,0,1).ScoreDelta,Is.EqualTo(110));Assert.That(s.Combo,Is.EqualTo(1));
        }
        [Test] public void ConsecutiveClearsMultiplyOnlyClearPointsAndSurviveRefill()
        {
            var source=new Source{Piece=new PieceDefinition("domino",1,new Cell(0,0),new Cell(1,0))};
            var s=new BlockGravitySession(new BlockGravityConfig(2,8,100),source);
            for(int i=1;i<=6;i++){Assert.That(Place(s,(i-1)%3,0).ScoreDelta,Is.EqualTo(20+100*i));Assert.That(s.Combo,Is.EqualTo(i));}
        }
        [Test] public void InvalidAndBusyPlacementsDoNotAdvanceTrayOrCombo()
        {
            var s=Start();Place(s,0,1);
            Assert.That(s.TryPlace(0,new Cell(0,7),out _),Is.EqualTo(PlacementFailure.UsedSlot));
            Assert.That(s.TryPlace(1,new Cell(-1,7),out _),Is.EqualTo(PlacementFailure.InvalidPosition));
            Assert.That(s.TryPlace(1,new Cell(0,7),out _),Is.EqualTo(PlacementFailure.None));
            Assert.That(s.TryPlace(2,new Cell(0,7),out _),Is.EqualTo(PlacementFailure.Busy));
            Assert.That(s.Combo,Is.EqualTo(1));Assert.That(s.Placements,Is.EqualTo(2));
        }
        [Test] public void MultipleLinesIncrementComboOnceAndFramesPreserveIt()
        {
            var b=new BoardState(2,2);b[0,0]=1;b[1,0]=2;b[0,1]=3;
            var s=new BlockGravitySession(new BlockGravityConfig(2,2,100),new Source(),b);
            Assert.That(s.TryPlace(0,new Cell(1,1),out var result),Is.EqualTo(PlacementFailure.None));
            Assert.That(s.Combo,Is.EqualTo(1));Assert.That(result.ScoreDelta,Is.EqualTo(1010));
            Assert.That(result.Frames.First().Combo,Is.Zero);Assert.That(result.Frames.Last().Combo,Is.EqualTo(1));
        }
        [Test] public void FreshRunHasNoCombo() { Assert.That(Start().Combo,Is.Zero); }
        [Test] public void ClearScoringValidatesMultiplierAndOverflow()
        {
            Assert.That(ScoreCalculator.Clear(2,3,4),Is.EqualTo(3600));
            Assert.Throws<ArgumentOutOfRangeException>(()=>ScoreCalculator.Clear(1,1,0));
            Assert.Throws<OverflowException>(()=>ScoreCalculator.Clear(int.MaxValue,int.MaxValue,int.MaxValue));
        }
    }
}
