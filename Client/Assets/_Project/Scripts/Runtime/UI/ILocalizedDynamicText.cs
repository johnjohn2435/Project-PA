using System.Collections.Generic;
using TMPro;
namespace PocketArcade.UI
{
    public interface ILocalizedDynamicText
    {
        IEnumerable<TMP_Text> DynamicTextLabels { get; }
    }
}
