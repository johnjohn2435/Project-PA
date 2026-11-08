using UnityEngine;
using UnityEngine.UI;

namespace PocketArcade.UI
{
    // All coordinates for the medal illustrations are in a 100 x 100 design square.
    // Only the uGUI mesh is rebuilt when selection or the RectTransform changes.
    public sealed class ArcadePaperGraphic : MaskableGraphic
    {
        public enum Shape { Paper, Banner, Rule, Capsule, Card, BlockMedal, FloorMedal, PortraitMedal, Check, EchoMedal }
        [SerializeField] private Shape shape;
        [SerializeField] private Color fill = Color.white;
        [SerializeField] private bool selected;
        public void Configure(Shape style, Color surface) { shape = style; fill = surface; SetVerticesDirty(); }
        public void Select(bool value) { if (selected != value) { selected = value; SetVerticesDirty(); } }

        static readonly Color Ink = Hex(0x45483B), Cream = Hex(0xFFF5D7), Gold = Hex(0xFFBF54);
        static readonly Color Teal = Hex(0x176F5C), Shadow = Hex(0x997343);
        static Color Hex(int rgb) => new Color32((byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb, 255);
        VertexHelper mesh;
        Vector2 origin;
        float scale;
        int Add(float x,float y,Color tint)
        {
            int index = mesh.currentVertCount;
            mesh.AddVert(new Vector3(origin.x + x*scale, origin.y + y*scale), tint * color, Vector2.zero);
            return index;
        }
        void Quad(float x0,float y0,float x1,float y1,Color tint)
        {
            int i=Add(x0,y0,tint); Add(x0,y1,tint); Add(x1,y1,tint); Add(x1,y0,tint);
            mesh.AddTriangle(i,i+1,i+2); mesh.AddTriangle(i,i+2,i+3);
        }
        void Oval(float x,float y,float rx,float ry,Color tint,float rotation=0,int segments=72)
        {
            float c=Mathf.Cos(rotation),s=Mathf.Sin(rotation);
            int center=Add(x,y,tint);
            for(int i=0;i<=segments;i++)
            {
                float angle=2*Mathf.PI*i/segments,dx=rx*Mathf.Cos(angle),dy=ry*Mathf.Sin(angle);
                int v=Add(x+dx*c-dy*s,y+dx*s+dy*c,tint);
                if(i>0) mesh.AddTriangle(center,v,v-1);
            }
        }
        void Round(float x,float y,float w,float h,float r,Color tint)
        {
            r=Mathf.Min(r,Mathf.Min(w,h)*.5f);
            int center=Add(x+w/2,y+h/2,tint),first=center+1;
            for(int corner=0;corner<4;corner++)
            {
                float cx=corner==0||corner==3?x+w-r:x+r;
                float cy=corner<2?y+h-r:y+r;
                for(int j=0;j<=10;j++)
                {
                    float angle=(corner+j/10f)*Mathf.PI*.5f;
                    int v=Add(cx+r*Mathf.Cos(angle),cy+r*Mathf.Sin(angle),tint);
                    if(v!=first) mesh.AddTriangle(center,v,v-1);
                }
            }
            mesh.AddTriangle(center,first,mesh.currentVertCount-1);
        }
        void Line(float x,float y,float endX,float endY,float width,Color tint)
        {
            Vector2 normal=new Vector2(y-endY,endX-x).normalized*width*.5f;
            int i=Add(x+normal.x,y+normal.y,tint); Add(endX+normal.x,endY+normal.y,tint);
            Add(endX-normal.x,endY-normal.y,tint); Add(x-normal.x,y-normal.y,tint);
            mesh.AddTriangle(i,i+1,i+2); mesh.AddTriangle(i,i+2,i+3);
        }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear(); Rect rect=rectTransform.rect;
            if(rect.width<=0 || rect.height<=0)return;
            mesh=vh; origin=rect.min; scale=1;
            float w=rect.width,h=rect.height;
            switch(shape)
            {
                case Shape.Banner:
                    Quad(0,0,w,h,fill);
                    for(float x=0;x<w;x+=28)
                    {
                        Quad(x,11,Mathf.Min(w,x+14),14,Hex(0x78B7A4));
                        Quad(x,h-14,Mathf.Min(w,x+14),h-11,Hex(0x78B7A4));
                    }
                    Quad(0,0,w,6,Ink);
                    break;
                case Shape.Paper:
                    Quad(0,0,w,h,fill);
                    // Even at tablet widths the dot pitch keeps this mesh below the canvas vertex limit.
                    float pitch=Mathf.Max(30,Mathf.Max(w,h)/66);
                    for(float y=pitch/2;y<h;y+=pitch)
                        for(float x=pitch/2;x<w;x+=pitch) Quad(x-1.8f,y-1.8f,x+1.8f,y+1.8f,Hex(0xDDC998));
                    break;
                case Shape.Rule:
                    for(float x=0;x<w;x+=26) Quad(x,0,Mathf.Min(x+15,w),h,fill);
                    break;
                case Shape.Card:
                case Shape.Capsule:
                    float rim=Mathf.Min(5,h*.045f), drop=Mathf.Min(10,h*.075f);
                    float radius=shape==Shape.Card?Mathf.Min(48,h*.15f):h*.5f;
                    Round(0,0,w,h-drop,radius,Shadow); Round(0,drop,w,h-drop,radius,Ink);
                    Round(rim,drop+rim,w-2*rim,h-drop-2*rim,radius-rim,fill);
                    break;
                default:
                    scale=Mathf.Min(w,h)/100; origin=rect.center-new Vector2(50,50)*scale;
                    if(shape==Shape.Check) { Badge(); break; }
                    Oval(50,48,48,46,Shadow);
                    Oval(50,51,48,47,Ink);
                    Oval(50,51,46.6f,45.6f,selected?Gold:Cream);
                    bool portrait=shape==Shape.PortraitMedal;
                    Oval(50,51,43.3f,42.3f,portrait && selected?Hex(0xA8D5BD):fill);
                    if(shape==Shape.BlockMedal) Blocks();
                    if(shape==Shape.FloorMedal) Disc();
                    if(shape==Shape.EchoMedal) Echo();
                    break;
            }
            mesh=null;
        }
        void Blocks()
        {
            Color grid=Hex(0xA0CBBE);
            for(int i=-3;i<=3;i++)
            {
                float d=i*13, extent=Mathf.Sqrt(1-d*d/(43f*43))*42;
                Quad(50+d-.35f,51-extent,50+d+.35f,51+extent,grid);
                Quad(50-extent,51+d-.35f,50+extent,51+d+.35f,grid);
            }
            Tile(20,24,Gold); Tile(36,24,Gold); Tile(20,40,Gold);
            Tile(52,24,Hex(0xA38DB2)); Tile(68,24,Hex(0xA38DB2)); Tile(68,40,Hex(0xA38DB2));
            Tile(40,54,Hex(0xEB907C)); Tile(40,70,Hex(0xEB907C)); Tile(56,70,Hex(0xEB907C));
        }
        void Tile(float x,float y,Color tint)
        {
            Round(x,y,15.5f,15.8f,3.2f,Ink); Round(x+.8f,y+2.1f,13.9f,13,2.5f,tint);
            Round(x+2,y+12.5f,11.8f,1.5f,.7f,Color.Lerp(tint,Color.white,.5f));
        }
        void Disc()
        {
            Oval(50,29,34,9,Ink,.22f); Oval(50,30.8f,33,8,Hex(0x6E9F8C),.22f);
            Oval(50,33,34,8.5f,Ink,.22f); Oval(50,33.5f,32.8f,7.3f,Hex(0xA5D6BC),.22f);
            Line(29,29,67,37.6f,1.8f,Hex(0xCBE4CB));
            Oval(76,72,4.7f,6.2f,Hex(0xAA6C70),.42f,6);
            Oval(75,74,2.4f,2.9f,Hex(0xDD9B91),.42f,5);
        }
        void Echo()
        {
            Round(22,24,56,53,5,Ink);
            Round(24,26,52,49,4,Hex(0xC6E4D8));
            Quad(47,27,49,74,Hex(0x93B5A0));
            Quad(24,48,76,50,Hex(0x93B5A0));
            Round(44,40,14,14,2,Hex(0x76B994));
            for(int i=0;i<5;i++)
                Oval(34+i*6,39+i*3,1.2f,1.2f,Hex(0x89769F),0,12);
            Pawn(35,58,Hex(0xC7B8D3));
            Pawn(63,43,Cream);
            Round(62,65,8,8,1.5f,Gold);
            Round(28,30,7,7,1.5f,Gold);
        }
        void Pawn(float x,float y,Color tint)
        {
            Round(x-6,y-11,12,16,5,Ink);
            Round(x-4.8f,y-9.5f,9.6f,13,4,tint);
            Oval(x,y+4,9.2f,8,Ink);
            Oval(x,y+4.5f,8,6.8f,tint);
            Oval(x-3,y+5,1.1f,1.5f,Ink,0,12);
            Oval(x+3,y+5,1.1f,1.5f,Ink,0,12);
        }
        void Badge()
        {
            Oval(50,50,48,48,Cream); Oval(50,50,41,41,Teal);
            Line(31,50,45,35,8,Cream); Line(44,35,70,66,8,Cream);
        }
    }
}
