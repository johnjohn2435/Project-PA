using System;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using PocketArcade.Editor;
using PocketArcade.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;
namespace PocketArcade.Tests
{
    public sealed class StringTableTests
    {
        private const string Valid = "{\"version\":1,\"rows\":[{\"key\":\"hello\",\"ko\":\"안녕 {0}\",\"en\":\"Hello {0}\"}]}";
        [TearDown] public void Cleanup() => L10n.Reset();
        [Test] public void TableUsesStringKeysAndBothLanguages()
        {
            var table = StringTable.Parse(Valid);
            Assert.That(table.Format("hello",GameLanguage.Korean,"친구"),Is.EqualTo("안녕 친구"));
            Assert.That(table.Format("hello",GameLanguage.English,"friend"),Is.EqualTo("Hello friend"));
            Assert.That(table.Get("missing",GameLanguage.Korean),Is.EqualTo("[missing]"));
        }
        [Test] public void DuplicateKeysAreRejected()
        {
            var row = "{\"key\":\"a\",\"ko\":\"가\",\"en\":\"A\"}";
            Assert.Throws<FormatException>(()=>StringTable.Parse("{\"version\":1,\"rows\":["+row+","+row+"]}"));
        }
        [TestCase("{\"version\":1,\"rows\":[]}")]
        [TestCase("{\"version\":2,\"rows\":[]}")]
        [TestCase("{\"version\":1,\"rows\":[{\"key\":\"a\",\"ko\":\"가\"}]}")]
        public void InvalidTablesAreRejected(string json) => Assert.Throws<FormatException>(()=>StringTable.Parse(json));
        [Test] public void PlaceholderMismatchAndMalformedBracesAreRejected()
        {
            Assert.Throws<FormatException>(()=>StringTable.Parse(Valid.Replace("Hello {0}","Hello {1}")));
            Assert.Throws<FormatException>(()=>StringTable.Parse(Valid.Replace("Hello {0}","Hello {")));
        }
        [Test] public void FailedReplacementPreservesCurrentTable()
        {
            L10n.Initialize(Valid);
            Assert.Throws<FormatException>(()=>L10n.Initialize("{}"));
            Assert.That(L10n.Format("hello","친구"),Is.EqualTo("안녕 친구"));
        }
        [Test] public void LocalizedLabelRefreshesOnLanguageChangeAndReenable()
        {
            L10n.Initialize(Valid);
            var root = new GameObject("Label",typeof(RectTransform),typeof(TextMeshProUGUI));
            try
            {
                var text=root.GetComponent<TMP_Text>(); var binding=root.AddComponent<LocalizedText>(); binding.SetKey("hello");
                Assert.That(text.text,Is.EqualTo("안녕 {0}"));
                L10n.SetLanguage(GameLanguage.English); Assert.That(text.text,Is.EqualTo("Hello {0}"));
                root.SetActive(false); L10n.SetLanguage(GameLanguage.Korean);
                root.SetActive(true); Assert.That(text.text,Is.EqualTo("안녕 {0}"));
            }
            finally { Object.DestroyImmediate(root); }
        }
        [Test] public void EveryLiteralLookupKeyExistsInProductionTable()
        {
            var table=StringTableSetup.Read();
            foreach(var path in Directory.GetFiles("Assets/_Project/Scripts","*.cs",SearchOption.AllDirectories))
            foreach(Match match in Regex.Matches(File.ReadAllText(path), "(?:L10n|StringTableSetup)\\.(?:Text|Format)\\(\"([^\"]+)\"\\s*[,)]"))
                Assert.That(table.Contains(match.Groups[1].Value),Is.True,path+": "+match.Groups[1].Value);
        }
        [Test] public void StringTableIsARequiredBootAsset()
        {
            var manifest=JsonUtility.FromJson<BootResourceManifest>(File.ReadAllText(BootResourceSetup.ManifestFile));
            var slot=Array.Find(manifest.resources,x=>x.path==StringTable.ResourcePath);
            Assert.That(slot,Is.Not.Null); Assert.That(slot.required,Is.True); Assert.That(slot.kind,Is.EqualTo("Asset"));
        }
        [Test] public void EditorBindingRespectsAnExistingLabelsOverride()
        {
            var root=new GameObject("Label",typeof(RectTransform),typeof(TextMeshProUGUI));
            try
            {
                var label=root.GetComponent<TMP_Text>();
                var binding=root.AddComponent<LocalizedText>(); binding.SetKey("hub.link");
                label.text=StringTableSetup.Read().Get("common.back",GameLanguage.English);
                StringTableSetup.BindKnown(label);
                Assert.That(binding.Key,Is.EqualTo("common.back"));
            }
            finally {Object.DestroyImmediate(root);}
        }
        [Test]
        public void ExistingFontCanBePreparedTwiceWithoutMissingGlyphError()
        {
            var method = typeof(PocketArcade.Editor.StringTableSetup).GetMethod("EnsureFont", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            var table = PocketArcade.Editor.StringTableSetup.Read();
            object first = method.Invoke(null, new object[] { table });
            object second = method.Invoke(null, new object[] { table });
            Assert.That(second, Is.SameAs(first));
        }
    }
}
