using System.Collections.Generic;
namespace PocketArcade.UI
{
    public sealed class PanelFrame
    {
        public UIPanel Panel { get; }
        internal readonly List<UIPopup> MutablePopups = new List<UIPopup>();
        public IReadOnlyList<UIPopup> Popups => MutablePopups.AsReadOnly();
        internal PanelFrame(UIPanel panel) { Panel = panel; }
    }
}
