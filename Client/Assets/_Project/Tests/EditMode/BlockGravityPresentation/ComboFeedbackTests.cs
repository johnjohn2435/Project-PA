using NUnit.Framework;
using PocketArcade.BlockGravity.Unity;
namespace PocketArcade.Tests.BlockGravityPresentation
{
    public sealed class ComboFeedbackTests
    {
        [Test] public void OnlyContinuedCombosPulseAndSettingsAreReadEveryTime()
        {
            bool enabled=true;int pulses=0;var feedback=new ComboFeedback(()=>enabled,()=>pulses++);
            feedback.OnClear(0);feedback.OnClear(1);Assert.That(pulses,Is.Zero);
            feedback.OnClear(2);Assert.That(pulses,Is.EqualTo(1));
            enabled=false;feedback.OnClear(3);Assert.That(pulses,Is.EqualTo(1));
            enabled=true;feedback.OnClear(4);Assert.That(pulses,Is.EqualTo(2));
        }
    }
}
