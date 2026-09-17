import json, pathlib, requests
ROOT=pathlib.Path(__file__).resolve().parents[1]
INFO=requests.get("http://127.0.0.1:8188/object_info",timeout=30).json()
STYLE="(flat vector illustration:1.4), minimalist 2D game sprite, (solid pastel colors:1.3), (thick dark outlines:1.3), simple rounded geometric shapes, tiny black dot eyes, tiny curved mouth, stubby short limbs, uniform color fills, full body centered, plain white background, ample empty margin"
CHARACTERS={
 "char_momo":"one cute chibi cat, (pale ivory white body:1.4), (coral pink scarf:1.4), small triangle ears, huge round head, tiny round body, two tiny feet, standing upright",
 "char_bori":"one cute chibi bear cub, (pale ivory white body:1.4), (mint green scarf:1.4), small round ears, huge round head, tiny round body, two tiny feet, standing upright",
 "char_nabi":"one cute chibi bunny, (pale ivory white body:1.4), (lavender purple scarf:1.5), short upright ears, huge round head, tiny round body, two tiny feet, standing upright"
}
NEGATIVE="(3d render:1.6), (shading:1.4), (gradients:1.4), (realistic:1.4), glossy, shiny, highlights, lighting, shadow, depth, toy, figurine, sculpture, plush, doll, photo, realistic fur, detailed fur, intricate texture, large eyes, sparkly eyes, human, anime girl, text, letters, logo, watermark, multiple characters, duplicate, character sheet, collage, border, cropped feet, busy background"
MODEL="DreamShaperXL_Turbo_v2_1.safetensors"
class Graph:
 def __init__(self): self.nodes=[]; self.links=[]; self.api={}
 def add(self,kind,title,pos,**values):
  index=len(self.nodes)+1; spec=INFO[kind]; inputs=[]; widgets=[]
  for name,decl in spec["input"].get("required",{}).items():
   value=values.get(name)
   typ=decl[0]; options=decl[1] if len(decl)>1 else {}
   if isinstance(value,tuple):
    source,slot=value; dtype=INFO[self.api[str(source)]["class_type"]]["output"][slot]
    assert dtype==typ,(kind,name,typ,dtype)
    link=len(self.links)+1; targetslot=len(inputs)
    inputs.append(dict(name=name,type=typ,link=link))
    self.links.append([link,source,slot,index,targetslot,dtype])
    self.nodes[source-1]["outputs"][slot]["links"].append(link)
   elif isinstance(typ,list) or typ in ("INT","FLOAT","STRING","BOOLEAN","COMBO"):
    if value is None: value=options.get("default",typ[0] if isinstance(typ,list) else None); values[name]=value
    widgets.append(value)
    if options.get("control_after_generate"): widgets.append("fixed")
    if options.get("image_upload"): widgets.append("image")
   else: raise ValueError((kind,name,typ))
  outputs=[dict(name=spec.get("output_name",spec["output"])[i],type=t,links=[],slot_index=i) for i,t in enumerate(spec["output"])]
  node=dict(id=index,type=kind,pos=list(pos),size=[340,300 if kind=="CLIPTextEncode" else 210],flags={},order=index-1,mode=0,inputs=inputs,outputs=outputs,properties={"Node name for S&R":kind},widgets_values=widgets,title=title)
  if kind=="CLIPTextEncode": node["color"]="#263d36" if "Positive" in title else "#493238"
  self.nodes.append(node)
  self.api[str(index)]={"class_type":kind,"inputs":{k:[str(v[0]),v[1]] if isinstance(v,tuple) else v for k,v in values.items()},"_meta":{"title":title}}
  return index
 def save(self,name):
  graph=dict(last_node_id=len(self.nodes),last_link_id=len(self.links),nodes=self.nodes,links=self.links,groups=[],config={},extra={"ds":{"scale":0.6,"offset":[30,80]}},version=0.4)
  (ROOT/"Workflows"/(name+".json")).write_text(json.dumps(graph,ensure_ascii=False,indent=2)+"\n",encoding="utf-8")
  (ROOT/"Api"/(name+".json")).write_text(json.dumps(self.api,ensure_ascii=False,indent=2)+"\n",encoding="utf-8")
  return graph

def cutout(g,image,prefix,x=1700):
 bg=g.add("LoadBackgroundRemovalModel","BiRefNet / background removal",(x,390),bg_removal_name="birefnet.safetensors")
 mask=g.add("RemoveBackground","Foreground mask",(x+390,310),bg_removal_model=(bg,0),image=(image,0))
 inverse=g.add("InvertMask","Required: foreground -> transparency",(x+780,370),mask=(mask,0))
 rgba=g.add("JoinImageWithAlpha","Transparent RGBA",(x+1170,130),image=(image,0),alpha=(inverse,0))
 g.add("SaveImage","RGBA master 1024",(x+1560,0),images=(rgba,0),filename_prefix=prefix+"/master")
 scale=g.add("ImageScale","Unity preview 512",(x+1560,340),image=(rgba,0),upscale_method="lanczos",width=512,height=512,crop="disabled")
 g.add("SaveImage","RGBA sprite 512",(x+1950,340),images=(scale,0),filename_prefix=prefix+"/sprite_512")

def generate(name,refine=False,background=False):
 g=Graph()
 ck=g.add("CheckpointLoaderSimple","DreamShaper XL Turbo v2.1",(0,0),ckpt_name=MODEL)
 positive=CHARACTERS["char_momo"]+", "+STYLE
 if background:
  positive="flat 2D cozy pastel mobile game background, small welcoming arcade room, rounded simple furniture, warm cream walls, coral mint lavender accents, bold clean brown outlines, gentle overhead perspective, empty open central floor for game UI, simple uncluttered shapes, no characters, no text, no signage"
 p=g.add("CLIPTextEncode","Positive / subject + shared style",(400,-60),clip=(ck,1),text=positive)
 n=g.add("CLIPTextEncode","Negative / avoid detail and realism",(400,320),clip=(ck,1),text=NEGATIVE if not background else "photo, 3d render, realistic textures, cluttered, text, logo, watermark, characters, people, dramatic shadows")
 if refine:
  load=g.add("LoadImage","Approved original PNG / replace this",(0,470),image="ProjectPA/momo_reference.png")
  size=g.add("ImageScale","Square reference: keep composition",(400,750),image=(load,0),upscale_method="lanczos",width=1024,height=1024,crop="disabled")
  latent=g.add("VAEEncode","Reference -> latent",(800,650),pixels=(size,0),vae=(ck,2))
 else:
  latent=g.add("EmptyLatentImage","Batch 1 / RTX 3080 10GB",(400,710),width=768 if background else 1024,height=1344 if background else 1024,batch_size=1)
 sampler=g.add("KSampler","Turbo: CFG 2 / fixed seed",(850,20),model=(ck,0),seed=243517,steps=12 if refine else 10,cfg=2.5,sampler_name="dpmpp_sde",scheduler="karras",positive=(p,0),negative=(n,0),latent_image=(latent,0),denoise=0.3 if refine else 1.0)
 decoded=g.add("VAEDecode","Decode artwork",(1250,0),samples=(sampler,0),vae=(ck,2))
 g.add("SaveImage","Raw master / preserve for refinement",(1250,340),images=(decoded,0),filename_prefix="ProjectPA/"+name+"/raw")
 if not background: cutout(g,decoded,"ProjectPA/"+name)
 return g.save(name)

for folder in ("Workflows","Api","Examples"): (ROOT/folder).mkdir(parents=True,exist_ok=True)
generate("01_character")
generate("02_refine",refine=True)
g=Graph()
source=g.add("LoadImage","Approved artwork / PNG",(0,0),image="ProjectPA/momo_reference.png")
cutout(g,source,"ProjectPA/03_cutout",400)
g.save("03_cutout")
generate("04_background",background=True)
presets={"style":STYLE,"negative":NEGATIVE,"characters":CHARACTERS,
 "props":{"coin":"one simple golden coin, engraved tiny paw symbol, front view, flat game reward icon","block":"one rounded coral square puzzle block, simple cream highlight, straight front view, flat game icon","gift":"one small mint gift box with coral ribbon, simple front three-quarter view, flat game reward icon"},
 "notes":"Species and accessories are proposed concepts, not approved final character designs."}
(ROOT/"prompt_presets.json").write_text(json.dumps(presets,ensure_ascii=False,indent=2)+"\n",encoding="utf-8")
print("Generated 4 UI workflows and 4 API graphs from live node schemas.")
