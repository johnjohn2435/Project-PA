using System;
using System.IO;
using NUnit.Framework;
using PocketArcade.BlockGravity.Unity;
using PocketArcade.Core.Save;

namespace PocketArcade.Tests.BlockGravityPresentation
{
    public sealed class ResultControllerTests
    {
        private static GameResultReceipt Receipt(GameResultRequest r)=>new GameResultReceipt
        {resultId=r.ResultId,gameId=r.GameId,score=r.Score,earnedCoins=r.EarnedCoins,creditedCoins=r.EarnedCoins,highScore=r.Score};
        [Test] public void SaveFailureCanRetryWithTheExactSameRequestAndSavedStateIsCached()
        {
            int attempts=0;var request=new GameResultRequest("run","block_gravity",1000,4);
            var controller=new BlockGravityResultController(request,r=>{Assert.That(r,Is.SameAs(request));if(++attempts==1)throw new IOException();return Receipt(r);});
            Assert.That(controller.TrySave(),Is.False);Assert.That(controller.SaveError,Is.TypeOf<IOException>());Assert.That(controller.IsSaved,Is.False);
            Assert.That(controller.TrySave(),Is.True);Assert.That(controller.TrySave(),Is.True);Assert.That(attempts,Is.EqualTo(2));Assert.That(controller.SaveError,Is.Null);
        }
        [Test] public void ReentrantSaveDoesNotStartAnotherPersistenceOperation()
        {
            BlockGravityResultController controller=null;int writes=0;
            controller=new BlockGravityResultController(new GameResultRequest("run","block_gravity",1000,4),r=>{writes++;Assert.That(controller.TrySave(),Is.False);return Receipt(r);});
            Assert.That(controller.TrySave(),Is.True);Assert.That(writes,Is.EqualTo(1));Assert.That(controller.IsSaving,Is.False);
        }
        [Test] public void MismatchedReceiptIsNotAcceptedAndReceiptCopiesAreIndependent()
        {
            var request=new GameResultRequest("run","block_gravity",1000,4);bool valid=false;
            var controller=new BlockGravityResultController(request,r=>valid?Receipt(r):Receipt(new GameResultRequest("wrong","block_gravity",1000,4)));
            Assert.That(controller.TrySave(),Is.False);valid=true;Assert.That(controller.TrySave(),Is.True);
            controller.Receipt.creditedCoins=99;Assert.That(controller.Receipt.creditedCoins,Is.EqualTo(4));
        }
    }
}
