using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace PocketArcade.Editor
{
    // Editor-authored vector park. The runtime loads one serialized mesh, not generated UI objects.
    public sealed class HubParkMeshBake
    {
        readonly List<Vector3> vertices = new List<Vector3>();
        readonly List<Color> colors = new List<Color>();
        readonly List<int> triangles = new List<int>();
        public static Color C(uint rgb) => new Color32((byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb, 255);
        static readonly Color Ink=C(0x54654D), Cream=C(0xFFF5D7), Green=C(0x609A70), Teal=C(0x168377), Gold=C(0xFFBF54);
        int V(float x,float y,Color c) { int n=vertices.Count; vertices.Add(new Vector3(x,y,0));colors.Add(QualitySettings.activeColorSpace==ColorSpace.Linear?c.linear:c);return n; }
        public void Poly(Color color,params Vector2[] points)
        {
            int first=vertices.Count;
            foreach(var p in points)V(p.x,p.y,color);
            for(int i=1;i<points.Length-1;i++){triangles.Add(first);triangles.Add(first+i);triangles.Add(first+i+1);}
        }
        public void Rect(float x,float y,float w,float h,Color c) => Poly(c,new Vector2(x,y),new Vector2(x+w,y),new Vector2(x+w,y+h),new Vector2(x,y+h));
        public void Oval(float x,float y,float rx,float ry,Color c,int steps=48)
        {
            int middle=V(x,y,c);
            for(int i=0;i<=steps;i++)
            {
                float a=Mathf.PI*2*i/steps;int n=V(x+rx*Mathf.Cos(a),y+ry*Mathf.Sin(a),c);
                if(i>0){triangles.Add(middle);triangles.Add(n-1);triangles.Add(n);}
            }
        }
        public void Round(float x,float y,float w,float h,float r,Color c)
        {
            Rect(x+r,y,w-2*r,h,c); Rect(x,y+r,w,h-2*r,c);
            Oval(x+r,y+r,r,r,c);Oval(x+w-r,y+r,r,r,c);Oval(x+r,y+h-r,r,r,c);Oval(x+w-r,y+h-r,r,r,c);
        }
        public void Line(float x,float y,float x1,float y1,float width,Color c)
        {
            var n=new Vector2(y-y1,x1-x).normalized*width*.5f;
            Poly(c,new Vector2(x,y)+n,new Vector2(x1,y1)+n,new Vector2(x1,y1)-n,new Vector2(x,y)-n);
            Oval(x,y,width*.5f,width*.5f,c,24);Oval(x1,y1,width*.5f,width*.5f,c,24);
        }
        void Tree(float x,float y,float size=1)
        {
            Oval(x+.12f,y-.1f,.5f*size,.23f*size,C(0x91B488));
            Round(x-.085f,y,.17f,.65f*size,.075f,C(0xA77C4F));
            Oval(x,y+.65f*size,.48f*size,.56f*size,Ink);
            Oval(x,y+.69f*size,.43f*size,.51f*size,Green);
            Oval(x-.14f*size,y+.84f*size,.19f*size,.23f*size,C(0x8AB986));
        }
        void Flower(float x,float y,Color color)
        {
            for(int i=0;i<5;i++){float a=i*Mathf.PI*.4f;Oval(x+Mathf.Cos(a)*.075f,y+Mathf.Sin(a)*.075f,.062f,.062f,color,10);}
            Oval(x,y,.045f,.045f,Gold,12);
        }
        void Pavilion(float x,float y,Color accent,int kind)
        {
            Round(x-1.55f,y-.12f,3.1f,2.18f,.3f,C(0x86A77C));
            Round(x-1.44f,y,2.88f,2.06f,.28f,Ink);
            Round(x-1.36f,y+.08f,2.72f,1.9f,.24f,Cream);
            Round(x-1.22f,y+.5f,2.44f,1.25f,.2f,C(0xF3E4B5));
            Round(x-.3f,y+.07f,.6f,.68f,.15f,Teal);
            // Scalloped canvas roof.
            Poly(Ink,new Vector2(x-1.55f,y+1.4f),new Vector2(x-1.05f,y+2.12f),new Vector2(x+1.05f,y+2.12f),new Vector2(x+1.55f,y+1.4f));
            Poly(accent,new Vector2(x-1.43f,y+1.45f),new Vector2(x-.99f,y+2.04f),new Vector2(x+.99f,y+2.04f),new Vector2(x+1.43f,y+1.45f));
            for(int i=0;i<6;i++)Oval(x-1.2f+i*.48f,y+1.43f,.24f,.17f,i%2==0?accent:Cream,24);
            Line(x,y+2.12f,x,y+2.6f,.055f,Ink);
            Poly(accent,new Vector2(x+.03f,y+2.6f),new Vector2(x+.55f,y+2.48f),new Vector2(x+.03f,y+2.3f));
            // Attraction emblems on the roof.
            Oval(x,y+1.92f,.65f,.63f,Ink);Oval(x,y+1.96f,.59f,.57f,Cream);
            if(kind==0)
            {
                for(int i=0;i<3;i++)Round(x-.39f+i*.27f,y+1.7f,.25f,.25f,.04f,Teal);
                Round(x-.12f,y+1.97f,.25f,.25f,.04f,Gold);
                Round(x+.15f,y+1.97f,.25f,.25f,.04f,accent);
            }
            else if(kind==1)
            {
                Oval(x,y+1.84f,.43f,.14f,Ink);Oval(x,y+1.89f,.4f,.12f,Gold);
                Line(x,y+1.91f,x,y+2.2f,.07f,Ink);
                Poly(accent,new Vector2(x-.42f,y+2.13f),new Vector2(x,y+2.37f),new Vector2(x+.42f,y+2.13f));
            }
            else
            {
                Oval(x,y+1.96f,.42f,.42f,accent);Oval(x,y+1.96f,.34f,.34f,Cream);
                Line(x,y+1.96f,x,y+2.18f,.045f,Ink);Line(x,y+1.96f,x+.18f,y+1.85f,.045f,Ink);
                Oval(x,y+1.96f,.055f,.055f,Ink);
            }
            // Cream name plaque and painted boarding circle.
            Round(x-1.48f,y-.17f,2.96f,.51f,.16f,Ink);
            Round(x-1.43f,y-.12f,2.86f,.43f,.13f,Cream);
            Oval(x,y-.66f,.51f,.26f,Gold);Oval(x,y-.63f,.39f,.19f,Cream);
            Poly(Teal,new Vector2(x-.14f,y-.67f),new Vector2(x,y-.53f),new Vector2(x+.14f,y-.67f));
        }
        public Mesh Build()
        {
            Rect(-20,-20,40,40,C(0xBFD5A1));
            Round(-5.15f,-5.7f,10.3f,13,.7f,C(0x91B488));
            Round(-4.98f,-5.53f,9.96f,12.66f,.6f,C(0xC8DDAA));
            // Lawn variation.
            Oval(-3.6f,-2.7f,1.05f,1.4f,C(0xB6CF98));Oval(3.7f,-2.8f,1,1.2f,C(0xB6CF98));
            foreach(float width in new[]{1.46f,1.30f})
            {
                var color=width>1.4f?C(0xD4C698):Cream;
                Line(0,-5.5f,0,-2.8f,width,color);
                Line(-.9f,-1.9f,-2.8f,.45f,width,color);Line(.9f,-1.9f,2.8f,.45f,width,color);
                Line(0,-.15f,0,4.0f,width,color);
            }
            Oval(0,-1.55f,2.05f,1.87f,C(0xD4C698));Oval(0,-1.5f,1.92f,1.77f,Cream);
            for(int i=0;i<20;i++){float a=i*Mathf.PI/10;Oval(Mathf.Cos(a)*1.56f,-1.5f+Mathf.Sin(a)*1.43f,.055f,.055f,C(0xD8C79E),10);}
            // Fountain in the central plaza.
            Oval(.08f,-1.66f,.93f,.62f,C(0xC7B88D));Oval(0,-1.5f,.85f,.65f,Ink);
            Oval(0,-1.45f,.78f,.58f,C(0xDCE8CD));Oval(0,-1.42f,.65f,.45f,C(0x71B8B3));
            Oval(0,-1.38f,.49f,.31f,C(0xAEDBD0));Round(-.09f,-1.4f,.18f,.48f,.07f,Cream);
            Oval(0,-.94f,.32f,.14f,Ink);Oval(0,-.89f,.29f,.12f,Gold);Oval(0,-.75f,.08f,.19f,C(0xD9F2E4));
            // Trees, garden beds and benches leave the paths open.
            for(int i=0;i<5;i++){Tree(-4.52f,-4.6f+i*2.25f,.68f);Tree(4.52f,-4.4f+i*2.22f,.68f);}
            Tree(-2.7f,5.8f,.85f);Tree(2.75f,5.7f,.85f);Tree(-3.8f,4.9f,.6f);Tree(3.8f,4.8f,.6f);
            foreach(float x in new[]{-3.2f,3.2f})
            {
                Round(x-.7f,-3.8f,1.4f,.5f,.2f,C(0x83A373));
                for(int i=0;i<5;i++)Flower(x-.5f+i*.25f,-3.55f,i%2==0?C(0xEB947D):Cream);
                Round(x-.52f,-2.55f,1.04f,.27f,.06f,Ink);Round(x-.5f,-2.46f,1,.22f,.05f,Gold);
                Rect(x-.4f,-2.72f,.08f,.22f,Ink);Rect(x+.32f,-2.72f,.08f,.22f,Ink);
            }
            Pavilion(-2.8f,1.12f,C(0xE99475),0);
            Pavilion(2.8f,1.12f,C(0x76AAA2),1);
            Pavilion(0,4.69f,C(0xACA0C7),2);
            // Entrance paving and lanterns.
            for(int i=0;i<4;i++)Rect(-.44f,-5.2f+i*.27f,.88f,.065f,C(0xD9C79A));
            foreach(float x in new[]{-1.16f,1.16f})
            {Line(x,-4.8f,x,-3.93f,.06f,Ink);Oval(x,-3.88f,.16f,.19f,Ink);Oval(x,-3.86f,.12f,.15f,Gold);}
            var mesh=new Mesh {name="Pocket Park map"};
            mesh.SetVertices(vertices);mesh.SetColors(colors);mesh.SetTriangles(triangles,0);mesh.RecalculateBounds();
            return mesh;
        }
    }
}
