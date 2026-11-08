using NUnit.Framework;
using PocketArcade.BlockGravity;
namespace PocketArcade.Tests
{
    public sealed class SessionSimulationTests
    {
        [TestCase(1)][TestCase(2)][TestCase(3)][TestCase(4)][TestCase(5)]
        [TestCase(6)][TestCase(7)][TestCase(8)][TestCase(9)][TestCase(10)]
        public void DeterministicRunsCanAlwaysResolveOrReachGameOver(int seed)
        {
            var session=new BlockGravitySession(new BlockGravityConfig(),new PieceBag(DefaultPieceSet.Create(),seed));
            var random=new System.Random(seed*97);
            for(int turn=0;turn<500 && session.State!=SessionState.GameOver;turn++)
            {
                Assert.That(session.State,Is.EqualTo(SessionState.WaitingForInput));bool placed=false;
                var candidates=new int[192];for(int i=0;i<candidates.Length;i++)candidates[i]=i;
                for(int i=candidates.Length-1;i>0;i--){int j=random.Next(i+1);int value=candidates[i];candidates[i]=candidates[j];candidates[j]=value;}
                foreach(int value in candidates)
                {if(session.TryPlace(value/64,new Cell(value%8,(value%64)/8),out _)==PlacementFailure.None){placed=true;break;}}
                Assert.That(placed,Is.True,"Waiting state must expose a legal move.");session.CompletePresentation();
                Assert.That(session.Score,Is.GreaterThanOrEqualTo(0));
            }
            Assert.That(session.State,Is.EqualTo(SessionState.GameOver).Or.EqualTo(SessionState.WaitingForInput));
            Assert.That(session.Placements,Is.InRange(1,500));
            TestContext.WriteLine(seed+","+session.Placements+","+session.Score+","+session.State);
        }
    }
}
