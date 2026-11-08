using System;
using System.IO;
using PocketArcade.Editor;
using PocketArcade.BlockGravity.Unity;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object=UnityEngine.Object;
namespace PocketArcade.BlockGravity.Editor
{
    public static partial class BlockGravitySetup
    {
        public const string TutorialFile=ResourcesRoot+BlockGravityHud.TutorialKey+".prefab";
        [MenuItem("Pocket Arcade/Block Gravity/4. Apply Tutorial Audio And Characters")]
        public static void ApplyOnboarding()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException(Text("bg.error.play"));
            string backup="Logs/OnboardingBackup/"+DateTime.Now.ToString("yyyyMMdd-HHmmss-fff");Directory.CreateDirectory(backup);
            foreach(var path in new[]{HudFile,PauseFile,BootResourceSetup.ManifestFile})File.Copy(path,backup+"/"+Path.GetFileName(path),true);
            SharedCharacterSetup.Apply();
            CreateTutorial();UpgradeMenuPrefabs();RegisterResources();AssetDatabase.SaveAssets();
            Validate();
            Debug.Log(StringTableSetup.Format("bg.setup.done",backup));
        }
        private static void CreateTutorial()
        {
            if(File.Exists(TutorialFile))return;
            var root=Window<BlockGravityTutorialPopup>("BlockGravityTutorial",BlockGravityHud.TutorialKey);
            try
            {
                var popup=root.GetComponent<BlockGravityTutorialPopup>();Modal(popup,31020);
                var shade=Rect(root.transform,"Shade",new Vector2(.5f,.5f),Vector2.zero);Fill(shade);shade.gameObject.AddComponent<Image>().color=new Color(0,0,0,.55f);
                var card=Rect(root.transform,"Card",new Vector2(.5f,.5f),new Vector2(880,920));card.gameObject.AddComponent<Image>().color=new Color(.95f,.96f,.91f);
                Set(popup,"title",Label(card,"Title",new Vector2(.5f,.87f),new Vector2(800,100),44));
                Set(popup,"progress",Label(card,"Progress",new Vector2(.5f,.75f),new Vector2(760,70),28));
                Set(popup,"body",Label(card,"Body",new Vector2(.5f,.50f),new Vector2(780,330),36));
                Set(popup,"notice",Label(card,"Notice",new Vector2(.5f,.26f),new Vector2(780,90),24));
                Set(popup,"previous",Button(card,"Previous",new Vector2(.25f,.15f),"bg.tutorial.previous",new Vector2(320,85)));
                var next=Button(card,"Next",new Vector2(.75f,.15f),null,new Vector2(320,85));Set(popup,"next",next);Set(popup,"nextLabel",next.GetComponentInChildren<TMPro.TMP_Text>(true));
                Set(popup,"later",Button(card,"Later",new Vector2(.5f,.045f),"bg.tutorial.later",new Vector2(520,65)));
                root.SetActive(false);Save(root,TutorialFile);
            }
            finally{Object.DestroyImmediate(root);}
        }
    }
}
