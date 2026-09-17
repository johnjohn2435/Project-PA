from pathlib import Path
from PIL import Image, ImageDraw
import json
ROOT=Path(__file__).resolve().parents[3]
OUT=ROOT/"Client/Assets/_Project/Art/Characters/Dummy"
OUT.mkdir(parents=True,exist_ok=True)
specs=[("char_momo","Momo","#F5A69B"),("char_bori","Bori","#B9DEC9"),("char_nabi","Nabi","#C6B8E8")]
sheet=Image.new("RGB",(900,340),"#F9F4EA")
manifest=[]
for index,(id,name,color) in enumerate(specs):
 scale=4; im=Image.new("RGBA",(256*scale,256*scale))
 d=ImageDraw.Draw(im)
 def box(xy):return tuple(v*scale for v in xy)
 outline="#66534C"; cream="#FFF3D9"
 d.rounded_rectangle(box((74,142,182,222)),radius=26*scale,fill=color,outline=outline,width=5*scale)
 d.ellipse(box((52,36,204,176)),fill=cream,outline=outline,width=5*scale)
 d.ellipse(box((91,98,101,112)),fill=outline)
 d.ellipse(box((155,98,165,112)),fill=outline)
 d.arc(box((115,105,141,129)),start=0,end=180,fill=outline,width=4*scale)
 d.ellipse(box((72,119,92,130)),fill=color)
 d.ellipse(box((164,119,184,130)),fill=color)
 d.rounded_rectangle(box((77,208,116,231)),radius=10*scale,fill=cream,outline=outline,width=4*scale)
 d.rounded_rectangle(box((140,208,179,231)),radius=10*scale,fill=cream,outline=outline,width=4*scale)
 im=im.resize((256,256),Image.Resampling.LANCZOS)
 file=OUT/(id+"_dummy.png"); im.save(file)
 sheet.paste(im,(22+index*300,30),im)
 ImageDraw.Draw(sheet).text((95+index*300,298),name+" / DUMMY",fill=outline)
 manifest.append(dict(id=id,name=name,displayName={"char_momo":"모모","char_bori":"보리","char_nabi":"나비"}[id],themeColor=color,sprite="Assets/_Project/Art/Characters/Dummy/"+file.name,status="temporary",size=[256,256]))
(ROOT/"ArtPipeline/ComfyUI/Examples/dummy_preview.png").parent.mkdir(parents=True,exist_ok=True)
sheet.save(ROOT/"ArtPipeline/ComfyUI/Examples/dummy_preview.png")
(OUT/"dummy_characters.json").write_text(json.dumps(manifest,ensure_ascii=False,indent=2)+"\n",encoding="utf-8")
print("Created 3 transparent 256px dummy sprites.")
