from pathlib import Path
from PIL import Image,ImageDraw,ImageFilter
import requests,json,time,uuid
p=Path(r'C:\Users\wsuk2\.codex\visualizations\2026\09\17\01a0aff8-5a1f-7303-8305-4df07e9f511e\poodle_ganaji_20261105');inputs=p/'inputs';inputs.mkdir(exist_ok=True);u='http://127.0.0.1:8188';raw=Image.open(p/'raw.png').convert('RGB');Image.new('RGB',(768,768),(12,10,9)).save(inputs/'black.png');Image.new('RGB',(768,768),(255,205,142)).save(inputs/'body_tint.png')
def mask(name,shapes):
 im=Image.new('L',(768*3,768*3));d=ImageDraw.Draw(im)
 for typ,coords in shapes:
  if typ=='polygon':d.polygon([(x*3,y*3) for x,y in coords],fill=255)
  else:getattr(d,typ)(tuple(x*3 for x in coords),fill=255)
 im.resize((768,768),Image.Resampling.LANCZOS).convert('RGB').save(inputs/(name+'.png'))
mask('head_keep',[('polygon',[(95,75),(660,75),(660,465),(605,475),(551,466),(514,453),(487,444),(454,451),(415,457),(374,459),(332,456),(290,448),(264,437),(245,448),(203,454),(155,453),(105,433),(95,400)])])
mask('eye_erase',[('ellipse',(287,300,343,373)),('ellipse',(423,311,482,387))]);Image.new('RGB',(768,768),raw.getpixel((365,330))).save(inputs/'eye_fill.png');eye_mask=Image.new('RGB',(25,34));ImageDraw.Draw(eye_mask).ellipse((0,0,24,33),fill='white');eye_mask.save(inputs/'eye_small_mask.png')
g={};n=0;mapping={};outputs={}
def node(c,**kw):
 global n
 n+=1;g[str(n)]={'class_type':c,'inputs':kw};return [str(n),0]
def load(f):
 with f.open('rb') as fp:r=requests.post(u+'/upload/image',files={'image':('pa_poodle_final_'+f.parent.name+'_'+f.name,fp,'image/png')},data={'overwrite':'true'},timeout=60)
 r.raise_for_status();nm=r.json()['name'];mapping[nm]=str(f);v=node('LoadImage',image=nm);return v,[v[0],1]
def maskload(f):return node('ImageToMask',image=load(f)[0],channel='red')
def scale(v,w,h):return node('ImageScale',image=v,upscale_method='bicubic',width=w,height=h,crop='disabled')
source,_=load(p/'raw.png');black,_=load(inputs/'black.png');clean=node('ImageCompositeMasked',destination=source,source=load(inputs/'eye_fill.png')[0],x=0,y=0,resize_source=False,mask=maskload(inputs/'eye_erase.png'))
for x,y,w,h,dx,dy in [(291,304,51,69,303,321),(426,316,54,71,440,333)]:
 eye=scale(node('ImageCrop',image=source,x=x,y=y,width=w,height=h),25,34);clean=node('ImageCompositeMasked',destination=clean,source=eye,x=dx,y=dy,resize_source=False,mask=maskload(inputs/'eye_small_mask.png'))
fg=node('RemoveBackground',bg_removal_model=node('LoadBackgroundRemovalModel',bg_removal_name='birefnet.safetensors'),image=source);a=node('MaskComposite',destination=fg,source=maskload(inputs/'head_keep.png'),x=0,y=0,operation='multiply');rgb=node('ImageCrop',image=clean,x=103,y=90,width=545,height=385);ma=node('ImageCrop',image=node('MaskToImage',mask=a),x=103,y=90,width=545,height=385);rgb=scale(rgb,600,450);ma=scale(ma,600,450);alpha=node('ImageToMask',image=ma,channel='red');whole=node('MaskComposite',destination=node('SolidMask',value=0.,width=768,height=768),source=alpha,x=84,y=55,operation='add');rgb=node('ImageCompositeMasked',destination=black,source=rgb,x=84,y=55,resize_source=False);head=node('JoinImageWithAlpha',image=rgb,alpha=node('InvertMask',mask=whole));save=node('SaveImage',images=head,filename_prefix='ProjectPA/PoodleGanaji_20261105/head');outputs[save[0]]='head';partnodes={'head':(rgb,whole)}
tint,_=load(inputs/'body_tint.png');rig=Path('ArtPipeline/Rigging/SharedCharacters6_20261105/sprites/char_cat')
for part in ['body','arm_L','arm_R','leg_L','leg_R']:
 image,maskref=load(rig/(part+'.png'));colored=node('ImageBlend',image1=image,image2=tint,blend_factor=1.,blend_mode='multiply');rgba=node('JoinImageWithAlpha',image=colored,alpha=maskref);save=node('SaveImage',images=rgba,filename_prefix='ProjectPA/PoodleGanaji_20261105/'+part);outputs[save[0]]=part;partnodes[part]=(colored,node('InvertMask',mask=maskref))
bg=load(p/'blank.png')[0];assembled=bg
for part in ['leg_R','leg_L','body','arm_R','arm_L','head']:
 rgb,alpha=partnodes[part];assembled=node('ImageCompositeMasked',destination=assembled,source=rgb,x=0,y=0,resize_source=False,mask=alpha)
save=node('SaveImage',images=assembled,filename_prefix='ProjectPA/PoodleGanaji_20261105/preview');outputs[save[0]]='preview'
(p/'final_api.json').write_text(json.dumps(g,indent=2));(p/'input_map.json').write_text(json.dumps(mapping,indent=2));r=requests.post(u+'/prompt',json={'prompt':g,'client_id':str(uuid.uuid4())},timeout=60);r.raise_for_status();pid=r.json()['prompt_id']
for _ in range(300):
 h=requests.get(u+'/history/'+pid,timeout=30).json()
 if pid in h:
  item=h[pid];assert item['status']['status_str']=='success',item['status'];(p/'final_history.json').write_text(json.dumps(item,indent=2))
  for k,part in outputs.items():
   d=item['outputs'][k]['images'][0];r=requests.get(u+'/view',params=d,timeout=60);r.raise_for_status();folder=p if part=='preview' else p/'sprites';folder.mkdir(exist_ok=True);(folder/(part+'.png')).write_bytes(r.content)
  print('SUCCESS: poodle on same six-part template',flush=True);break
 time.sleep(1)

