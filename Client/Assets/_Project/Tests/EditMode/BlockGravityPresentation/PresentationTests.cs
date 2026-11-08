using System.IO;
using NUnit.Framework;
using PocketArcade.BlockGravity;
using PocketArcade.Editor;
using PocketArcade.BlockGravity.Editor;
using PocketArcade.BlockGravity.Unity;
using PocketArcade.UI;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace PocketArcade.Tests.BlockGravityPresentation
{
    public sealed class PresentationTests
    {
        private GameObject instance;
        [SetUp] public void Setup() => L10n.Initialize(File.ReadAllText("Assets/_Project/Resources/Data/StringTable.json"));
        [TearDown] public void Cleanup() { if(instance!=null)Object.DestroyImmediate(instance);L10n.Reset(); }
        private BlockGravityBoardView View()
        {
            instance=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(BlockGravitySetup.WorldFile));
            var view=instance.GetComponent<BlockGravityBoardView>();view.Initialize();view.Reflow(1080,1920,new Rect(0,0,1080,1920));return view;
        }
        [Test] public void PrefabsUseTheNewUIFrameContract()
        {
            foreach(var path in new[]{BlockGravitySetup.HudFile,BlockGravitySetup.PauseFile})
            {
                var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path);Assert.That(prefab,Is.Not.Null);
                Assert.That(prefab.GetComponentsInChildren<UIBase>(true).Length,Is.EqualTo(1));
                prefab.GetComponent<UIBase>().ValidateDefinition();
                foreach(var text in prefab.GetComponentsInChildren<TMP_Text>(true))Assert.That(text.font,Is.Not.Null);
            }
        }
        [Test] public void TemporarySpriteHasOneWorldUnitPerCell()
        {
            var sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Resources/MiniGames/BlockGravity/Cell.png");
            Assert.That(sprite.pixelsPerUnit,Is.EqualTo(128));Assert.That(sprite.bounds.size.x,Is.EqualTo(1));
            Assert.That(sprite.pivot,Is.EqualTo(new Vector2(64,64)));
        }
        [Test] public void BoardPoolReusesRenderersAndRestoresClearedIds()
        {
            var view=View();var board=new BoardState(8,8);board[0,0]=1;board[7,7]=2;view.ShowBoard(board);
            Assert.That(view.VisibleBlockCount,Is.EqualTo(2));
            board[0,0]=0;board[1,0]=3;view.ShowBoard(board);Assert.That(view.VisibleBlockCount,Is.EqualTo(2));
            int count=0;foreach(var sprite in instance.GetComponentsInChildren<SpriteRenderer>(true))
                if(sprite.sortingOrder==20 && sprite.enabled){count++;Assert.That(sprite.color.a,Is.EqualTo(1));}
            Assert.That(count,Is.EqualTo(2));view.ShowBoard(new BoardState(8,8));Assert.That(view.VisibleBlockCount,Is.Zero);
        }
        [Test] public void MovementInterpolatesBetweenActualCellCenters()
        {
            var view=View();var board=new BoardState(8,8);board[2,6]=1;view.ShowBoard(board);
            var target=GravityResolver.Resolve(board,GravityDirection.Down).Board;view.BeginMovement(target);view.AnimateMovement(.5f);
            foreach(var sprite in instance.GetComponentsInChildren<SpriteRenderer>(true))
                if(sprite.sortingOrder==20 && sprite.enabled)Assert.That(sprite.transform.position.y,Is.EqualTo(-.5f).Within(.001));
            view.AnimateMovement(1);view.ShowBoard(target);Assert.That(view.VisibleBlockCount,Is.EqualTo(1));
        }
        [Test] public void InvalidAndValidPlacementHaveDifferentPreviewColors()
        {
            var view=View();var shape=DefaultPieceSet.Create()[0].Shape;
            var point=(Vector2)view.PlayCamera.WorldToScreenPoint(Vector3.zero);
            view.ShowDrag(shape,point,false,new Cell(4,4),true);
            foreach(var sprite in instance.GetComponentsInChildren<SpriteRenderer>(true))
                if(sprite.sortingOrder==30 && sprite.enabled)Assert.That(sprite.color.g,Is.GreaterThan(sprite.color.r));
            view.ShowDrag(shape,point,false,new Cell(-1,4),false);
            foreach(var sprite in instance.GetComponentsInChildren<SpriteRenderer>(true))
                if(sprite.sortingOrder==30 && sprite.enabled)Assert.That(sprite.color.r,Is.GreaterThan(sprite.color.g));
            view.HideDrag();foreach(var sprite in instance.GetComponentsInChildren<SpriteRenderer>(true))
                if(sprite.sortingOrder==30 || sprite.sortingOrder==100)Assert.That(sprite.enabled,Is.False);
        }
        [Test] public void TrayUsesThreeDistinctSlotsAndHonorsUsedPieces()
        {
            var view=View();var pieces=DefaultPieceSet.Create();view.ShowTray(new[]{pieces[0],null,pieces[9]});
            int count=0;foreach(var sprite in instance.GetComponentsInChildren<SpriteRenderer>(true))if(sprite.sortingOrder==40 && sprite.enabled)count++;
            Assert.That(count,Is.EqualTo(5));
            for(int i=0;i<3;i++)Assert.That(view.SlotAt(new Vector2((float)(view.Layout.Tray.X+view.Layout.Tray.Width*(i+.5)/3),(float)view.Layout.Tray.CenterY)),Is.EqualTo(i));
            Assert.That(view.SlotAt(Vector2.zero),Is.EqualTo(-1));
        }
        [Test] public void DragAnchorMatchesFixedOrientationShapeCenter()
        {
            var view=View();foreach(var piece in DefaultPieceSet.Create())
            {
                var center=view.CellPosition(new Cell(2,3))+new Vector3((piece.Shape.Width-1)*.5f,(piece.Shape.Height-1)*.5f,0);
                // Camera projection uses its actual pixel rect; Reflow sets the intended world scale.
                var point=(Vector2)view.PlayCamera.WorldToScreenPoint(center);
                Assert.That(view.DragOrigin(piece.Shape,point,false),Is.EqualTo(new Cell(2,3)));
            }
        }

        [Test] public void NineCellPiecesUseSeparateTrayPoolsAndFullDragPreview()
        {
            var view=View();var source=new ProgressivePieceSource(1);PieceDefinition large=null;
            for(int i=0;i<200&&large==null;i++){var p=source.Next();if(p.Shape.Cells.Count==9)large=p;}
            Assert.That(large,Is.Not.Null);
            view.ShowTray(new[]{large,large,large});
            var data=new SerializedObject(view);
            var tray=data.FindProperty("trayCells");
            for(int slot=0;slot<3;slot++)
            {
                var positions=new System.Collections.Generic.HashSet<Vector3>();
                for(int cell=0;cell<9;cell++)
                {
                    var sprite=(SpriteRenderer)tray.GetArrayElementAtIndex(slot*9+cell).objectReferenceValue;
                    Assert.That(sprite.enabled,Is.True);Assert.That(positions.Add(sprite.transform.position),Is.True);
                }
            }
            var point=(Vector2)view.PlayCamera.WorldToScreenPoint(Vector3.zero);
            view.ShowDrag(large.Shape,point,false,new Cell(2,3),true);
            foreach(string field in new[]{"ghostCells","draggedCells"})
            {
                var pool=data.FindProperty(field);Assert.That(pool.arraySize,Is.EqualTo(9));
                for(int i=0;i<9;i++)Assert.That(((SpriteRenderer)pool.GetArrayElementAtIndex(i).objectReferenceValue).enabled,Is.True);
            }
            view.ShowTray(new[]{DefaultPieceSet.Create()[0],null,null});view.HideDrag();
            int visible=0;foreach(var sprite in instance.GetComponentsInChildren<SpriteRenderer>(true))
                if(sprite.sortingOrder>=30&&sprite.enabled)visible++;
            Assert.That(visible,Is.EqualTo(1));
        }

        [Test] public void NewStringKeysHaveBothLanguagesAndMatchingArguments()
        {
            var table=StringTableSetup.Read();int count=0;
            foreach(var row in table.Rows)if(row.key.StartsWith("bg.")){count++;Assert.That(row.ko,Is.Not.Empty);Assert.That(row.en,Is.Not.Empty);}
            Assert.That(count,Is.GreaterThanOrEqualTo(25));
            Assert.That(table.Format("bg.score",GameLanguage.English,250),Does.Contain("250"));
        }
        [Test] public void GeneratedSceneAndResourceInventoryValidateTogether() => BlockGravitySetup.Validate();
        [Test] public void CameraShakeIsBoundedAndReflowRestoresItsOrigin()
        {
            var view=View();var rest=view.PlayCamera.transform.position;
            view.StartComboShake(1);view.TickShake(.02f);Assert.That(view.PlayCamera.transform.position,Is.EqualTo(rest));
            view.StartComboShake(20);view.TickShake(.02f);
            Assert.That(Vector3.Distance(view.PlayCamera.transform.position,rest),Is.InRange(.001f,.18f));
            view.TickShake(.5f);Assert.That(view.PlayCamera.transform.position,Is.EqualTo(rest));
            view.StartComboShake(2);view.TickShake(.01f);view.Reflow(1080,1920,new Rect(0,0,1080,1920));
            Assert.That(view.PlayCamera.transform.position,Is.EqualTo(rest));
            view.TickShake(.01f);Assert.That(view.PlayCamera.transform.position,Is.EqualTo(rest));
        }
    }
}
