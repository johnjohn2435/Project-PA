using System;
using System.Collections.Generic;
using UnityEngine;

namespace PocketArcade.BlockGravity.Unity
{
    public sealed class BlockGravityBoardView : MonoBehaviour
    {
        [SerializeField] private Camera playCamera;
        [SerializeField] private GameObject visualRoot;
        [SerializeField] private SpriteRenderer[] blocks;
        [SerializeField] private SpriteRenderer[] trayCells;
        [SerializeField] private SpriteRenderer[] ghostCells;
        [SerializeField] private SpriteRenderer[] draggedCells;
        private readonly Dictionary<int, SpriteRenderer> live = new Dictionary<int, SpriteRenderer>();
        private readonly List<SpriteRenderer> free = new List<SpriteRenderer>();
        private readonly List<int> removed = new List<int>();
        private readonly List<SpriteRenderer> moving = new List<SpriteRenderer>();
        private readonly List<Vector3> starts = new List<Vector3>();
        private readonly List<Vector3> targets = new List<Vector3>();
        private readonly Color[] palette = { new Color(.87f,.54f,.43f), new Color(.44f,.67f,.56f), new Color(.62f,.54f,.74f), new Color(.85f,.72f,.39f) };
        public PlayfieldLayout Layout { get; private set; }
        public Camera PlayCamera => playCamera;
        private float inputScale;
        private Vector3 cameraRestPosition;
        private float shakeElapsed, shakeDuration, shakeStrength;
        public void StartComboShake(int combo)
        {
            if (combo < 2) return;
            shakeElapsed = 0; shakeDuration = .20f;
            shakeStrength = Mathf.Min(.12f, .06f + combo * .005f);
        }
        public void TickShake(float deltaTime)
        {
            if (shakeDuration <= 0) return;
            shakeElapsed = Mathf.Min(shakeDuration, shakeElapsed + Mathf.Max(0,deltaTime));
            if (shakeElapsed >= shakeDuration) { ResetShake(); return; }
            float strength = shakeStrength * (1-shakeElapsed/shakeDuration);
            playCamera.transform.position = cameraRestPosition + new Vector3(Mathf.Sin(shakeElapsed*137f),Mathf.Cos(shakeElapsed*173f),0)*strength;
        }
        public void ResetShake()
        { shakeDuration = 0; if (playCamera != null && Layout != null) playCamera.transform.position = cameraRestPosition; }
        public int VisibleBlockCount => live.Count;
        public void Initialize()
        {
            if (playCamera == null || visualRoot == null || blocks == null || blocks.Length != 64 ||
                trayCells == null || trayCells.Length != 3 * ProgressivePieceSource.MaxPieceCells || ghostCells == null || ghostCells.Length != ProgressivePieceSource.MaxPieceCells ||
                draggedCells == null || draggedCells.Length != ProgressivePieceSource.MaxPieceCells)
                throw new InvalidOperationException(L10n.Text("bg.error.references"));
            live.Clear(); free.Clear();
            foreach (var sprite in blocks) { sprite.enabled = false; free.Add(sprite); }
            HideDrag();
            foreach (var sprite in trayCells) sprite.enabled = false;
        }
        public void Reflow(int width, int height, Rect safe)
        {
            Layout = PlayfieldLayout.Calculate(width, height, new LayoutRect(safe.x, safe.y, safe.width, safe.height));
            inputScale = Mathf.Min(safe.width/1080f,safe.height/1920f);
            playCamera.orthographic = true; playCamera.orthographicSize = (float)Layout.OrthographicSize;
            shakeDuration = 0;
            cameraRestPosition = new Vector3((float)Layout.CameraX, (float)Layout.CameraY, -10);
            playCamera.transform.SetPositionAndRotation(cameraRestPosition, Quaternion.identity);
        }
        public void SetVisible(bool visible) { visualRoot.SetActive(visible); }
        public Vector3 CellPosition(Cell cell) => new Vector3(cell.X - 3.5f, cell.Y - 3.5f, 0);
        public Vector3 ScreenToWorld(Vector2 screen) => playCamera.ScreenToWorldPoint(new Vector3(screen.x, screen.y, -playCamera.transform.position.z));
        private static void Place(SpriteRenderer sprite, Vector3 position, float size, Color color)
        { sprite.enabled = true; sprite.transform.position = position; sprite.transform.localScale = new Vector3(size, size, 1); sprite.color = color; }
        public void ShowBoard(BoardState board)
        {
            removed.Clear();
            foreach (var pair in live) removed.Add(pair.Key);
            for (int y = 0; y < board.Height; y++) for (int x = 0; x < board.Width; x++)
            { int id = board[x,y]; if (id != 0) removed.Remove(id); }
            foreach (int id in removed) { var sprite = live[id]; sprite.enabled = false; free.Add(sprite); live.Remove(id); }
            for (int y = 0; y < board.Height; y++) for (int x = 0; x < board.Width; x++)
            {
                int id = board[x,y]; if (id == 0) continue;
                if (!live.TryGetValue(id, out var sprite))
                {
                    if (free.Count == 0) throw new InvalidOperationException(L10n.Text("bg.error.pool"));
                    sprite = free[free.Count-1]; free.RemoveAt(free.Count-1); live.Add(id, sprite);
                }
                Place(sprite, CellPosition(new Cell(x,y)), .90f, palette[(id-1) % palette.Length]);
            }
        }
        public void BeginMovement(BoardState target)
        {
            moving.Clear(); starts.Clear(); targets.Clear();
            for (int y = 0; y < target.Height; y++) for (int x = 0; x < target.Width; x++)
                if (live.TryGetValue(target[x,y], out var sprite))
                { moving.Add(sprite); starts.Add(sprite.transform.position); targets.Add(CellPosition(new Cell(x,y))); }
        }
        public void AnimateMovement(float progress)
        { for (int i = 0; i < moving.Count; i++) moving[i].transform.position = Vector3.Lerp(starts[i], targets[i], Mathf.SmoothStep(0,1,progress)); }
        public void AnimateClear(LineMatch lines, BoardState previous, float progress)
        {
            foreach (var cell in lines.Cells)
                if (live.TryGetValue(previous[cell.X,cell.Y], out var sprite))
                { var color = sprite.color; color.a = 1-progress; sprite.color = color; sprite.transform.localScale = Vector3.one * (.9f * (1-progress)); }
        }
        public void ShowTray(IReadOnlyList<PieceDefinition> tray, int hiddenSlot = -1)
        {
            for (int slot = 0; slot < 3; slot++)
            {
                var piece = tray[slot];
                Vector2 pixel = new Vector2((float)(Layout.Tray.X + Layout.Tray.Width * (slot+.5)/3), (float)Layout.Tray.CenterY);
                DrawShape(trayCells, slot*ProgressivePieceSource.MaxPieceCells, piece?.Shape, ScreenToWorld(pixel), .6f, palette[slot], slot != hiddenSlot);
            }
        }
        public int SlotAt(Vector2 screen)
        {
            if (Layout == null || screen.x < Layout.Tray.X || screen.x >= Layout.Tray.X+Layout.Tray.Width ||
                screen.y < Layout.Tray.Y || screen.y >= Layout.Tray.Y+Layout.Tray.Height) return -1;
            return (int)((screen.x-Layout.Tray.X)/(Layout.Tray.Width/3));
        }
        public Cell DragOrigin(PieceShape shape, Vector2 pointer, bool touch)
        {
            Vector3 world = PointerWorld(pointer, touch);
            return new Cell(Mathf.FloorToInt(world.x+4-(shape.Width-1)*.5f), Mathf.FloorToInt(world.y+4-(shape.Height-1)*.5f));
        }
        private Vector3 PointerWorld(Vector2 screen, bool touch)
        {
            if (touch) screen.y += 120 * inputScale;
            return ScreenToWorld(screen);
        }
        public void ShowDrag(PieceShape shape, Vector2 pointer, bool touch, Cell origin, bool valid)
        {
            DrawShape(draggedCells, 0, shape, PointerWorld(pointer,touch), 1, palette[0], true);
            Color preview = valid ? new Color(.30f,.78f,.45f,.5f) : new Color(.95f,.28f,.25f,.5f);
            Vector3 center = CellPosition(origin) + new Vector3((shape.Width-1)*.5f,(shape.Height-1)*.5f,0);
            DrawShape(ghostCells, 0, shape, center, 1, preview, true);
        }
        private static void DrawShape(SpriteRenderer[] sprites, int offset, PieceShape shape, Vector3 center, float scale, Color color, bool visible)
        {
            if (shape != null && shape.Cells.Count > ProgressivePieceSource.MaxPieceCells) throw new ArgumentOutOfRangeException(nameof(shape));
            for (int i = 0; i < ProgressivePieceSource.MaxPieceCells; i++)
            {
                var sprite = sprites[offset+i];
                if (!visible || shape == null || i >= shape.Cells.Count) { sprite.enabled = false; continue; }
                var cell = shape.Cells[i];
                Place(sprite, center+new Vector3((cell.X-(shape.Width-1)*.5f)*scale,(cell.Y-(shape.Height-1)*.5f)*scale,0), scale*.9f,color);
            }
        }
        public void HideDrag()
        { foreach (var sprite in ghostCells) sprite.enabled = false; foreach (var sprite in draggedCells) sprite.enabled = false; }
    }
}
