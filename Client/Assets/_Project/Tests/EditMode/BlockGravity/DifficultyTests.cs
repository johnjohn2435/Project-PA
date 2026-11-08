using System;
using System.Collections.Generic;
using NUnit.Framework;
using PocketArcade.BlockGravity;

namespace PocketArcade.Tests.BlockGravity
{
    public sealed class DifficultyTests
    {
        [Test] public void PieceSizesIncreaseAtWholeTrayBoundaries()
        {
            var seen = new HashSet<string>();
            for (int seed = 0; seed < 64; seed++)
            {
                var source = new ProgressivePieceSource(seed);
                for (int draw = 0; draw < 90; draw++)
                {
                    var piece = source.Next();
                    int size = piece.Shape.Cells.Count;
                    Assert.That(size, Is.InRange(draw < 6 ? 1 : draw < 18 ? 3 : draw < 36 ? 4 : 5,
                        draw < 6 ? 4 : draw < 18 ? 6 : 9));
                    Assert.That(PlacementValidator.HasPlacement(new BoardState(8,8),piece.Shape), Is.True);
                    if (draw >= 36) seen.Add(piece.Id);
                }
            }
            Assert.That(seen, Is.EquivalentTo(new[] { "rect6_h", "rect6_v", "square9", "corner5" }));
        }

        [TestCase(0,1)] [TestCase(5,1)] [TestCase(6,2)] [TestCase(17,2)]
        [TestCase(18,3)] [TestCase(35,3)] [TestCase(36,4)] [TestCase(int.MaxValue,4)]
        public void HudDifficultyMatchesIncomingTray(int placements,int expected)
        { Assert.That(ProgressivePieceSource.LevelForPlacements(placements),Is.EqualTo(expected)); }

        [TestCase(0)] [TestCase(int.MaxValue)] [TestCase(int.MinValue)]
        public void RestartWithSameSeedReplaysEveryDifficultyStage(int seed)
        {
            var a = new ProgressivePieceSource(seed); var b = new ProgressivePieceSource(seed);
            for(int i=0;i<200;i++) Assert.That(a.Next().Id,Is.EqualTo(b.Next().Id));
            Assert.Throws<ArgumentOutOfRangeException>(()=>ProgressivePieceSource.LevelForPlacements(-1));
        }

        [Test] public void RejectedAndBusyInputCannotRushDifficultyOrRerollTray()
        {
            var a=new BlockGravitySession(new BlockGravityConfig(),new ProgressivePieceSource(42));
            var b=new BlockGravitySession(new BlockGravityConfig(),new ProgressivePieceSource(42));
            for(int i=0;i<18;i++)
            {
                Assert.That(a.TryPlace(0,new Cell(-1,0),out _),Is.Not.EqualTo(PlacementFailure.None));
                var choice=FirstMove(a);
                Assert.That(a.TryPlace(choice.slot,choice.cell,out _),Is.EqualTo(PlacementFailure.None));
                Assert.That(a.TryPlace(choice.slot,choice.cell,out _),Is.EqualTo(PlacementFailure.Busy));
                Assert.That(b.TryPlace(choice.slot,choice.cell,out _),Is.EqualTo(PlacementFailure.None));
                a.CompletePresentation();b.CompletePresentation();
                Assert.That(a.Score,Is.EqualTo(b.Score));
                for(int slot=0;slot<3;slot++)Assert.That(a.Tray[slot]?.Id,Is.EqualTo(b.Tray[slot]?.Id));
            }
        }

        private static (int slot, Cell cell) FirstMove(BlockGravitySession session)
        {
            var board=session.Board;var tray=session.Tray;
            for(int slot=0;slot<3;slot++)if(tray[slot]!=null)
                for(int y=0;y<8;y++)for(int x=0;x<8;x++)
                    if(PlacementValidator.CanPlace(board,tray[slot].Shape,new Cell(x,y)))return(slot,new Cell(x,y));
            Assert.Fail();return default;
        }

        [Test] public void RepeatedFirstFitActuallyLosesWithProgressivePieces()
        {
            for(int seed=0;seed<24;seed++)
            {
                var session=new BlockGravitySession(new BlockGravityConfig(),new ProgressivePieceSource(seed));
                while(session.State!=SessionState.GameOver&&session.Placements<500)
                {
                    var move=FirstMove(session);
                    Assert.That(session.TryPlace(move.slot,move.cell,out var result),Is.EqualTo(PlacementFailure.None));
                    Assert.That(result.IsGameOver,Is.EqualTo(!PlacementValidator.HasAnyPlacement(session.Board,session.Tray)));
                    Assert.That(session.State,Is.EqualTo(SessionState.AwaitingPresentation));
                    session.CompletePresentation();
                }
                Assert.That(session.State,Is.EqualTo(SessionState.GameOver));
                Assert.That(session.Placements,Is.GreaterThanOrEqualTo(6));
                Assert.That(session.TryClaimReward(out _),Is.True);
                Assert.That(session.TryClaimReward(out _),Is.False);
            }
        }
    }
}
