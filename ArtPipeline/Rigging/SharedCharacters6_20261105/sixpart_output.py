from pathlib import Path
from PIL import Image
import requests,json,time,uuid
p=Path(r'C:\Users\wsuk2\.codex\visualizations\2026\09\17\01a0aff8-5a1f-7303-8305-4df07e9f511e\sixpart_rig_20261105');u='http://127.0.0.1:8188';skins=json.loads((p/'segmentation.json').read_text(encoding='utf-8'));Image.new('RGB',(1152,768),(12,10,9)).save(p/'inputs/black_source.png');g={};n=0;mapping={}
def node(c,**kw):
 global n
 n+=1;g[str(n)]={'class_type':c,'inputs':kw};return [str(n),0]
def load(f):
 with f.open('rb') as fp:r=requests.post(u+'/upload/image',files={'image':('pa_sixpart_'+f.name,fp,'image/png')},data={'overwrite':'true'},timeout=60)
 r.raise_for_status();nm=r.json()['name'];mapping[nm]=str(f.relative_to(p));return node('LoadImage',image=nm)
def maskfile(f):return node('ImageToMask',image=load(f),channel='red')
def crop(image,rect):return node('ImageCrop',image=image,x=rect[0],y=rect[1],width=rect[2],height=rect[3])
def scale(image,w,h):return node('ImageScale',image=image,upscale_method='bicubic',width=w,height=h,crop='disabled')
src=load(p/'inputs/source.png');black=load(p/'inputs/black_source.png');canvas=load(p/'inputs/black.png');model=node('LoadBackgroundRemovalModel',bg_removal_name='birefnet.safetensors');fg=node('RemoveBackground',bg_removal_model=model,image=src);rep=maskfile(p/'inputs/body_repair.png');clean=src
for rect,target in [([182,430,53,55],[130,418,145,75]),([572,432,39,54],[515,418,139,76]),([913,430,57,55],[862,414,147,70])]:
 patch=scale(crop(src,rect),target[2],target[3]);patch_full=node('ImageCompositeMasked',destination=src,source=patch,x=target[0],y=target[1],resize_source=False)
 # Each patch affects only its selected local torso region.
 area=node('SolidMask',value=1.,width=target[2],height=target[3]);zero=node('SolidMask',value=0.,width=1152,height=768);placed=node('MaskComposite',destination=zero,source=area,x=target[0],y=target[1],operation='add');limited=node('MaskComposite',destination=placed,source=rep,x=0,y=0,operation='multiply');clean=node('ImageCompositeMasked',destination=clean,source=patch_full,x=0,y=0,resize_source=False,mask=limited)
node('SaveImage',images=clean,filename_prefix='ProjectPA/SixPartRig_20261105/body_backplate_source')
placements={'head':[84,55,600,450],'body':[249,470,270,200],'arm_L':[253,508,86,113],'arm_R':[457,508,65,107],'leg_L':[270,585,100,125],'leg_R':[398,585,100,125]};saveids={}
for key,v in skins.items():
 for part in ['head','body','arm_L','arm_R','leg_L','leg_R']:
  select=maskfile(p/'inputs'/(key+'_'+part+'.png'));alpha=node('MaskComposite',destination=fg,source=select,x=0,y=0,operation='multiply');image=clean if part=='body' else src
  if part in ['head','body']:
   image=node('ImageCompositeMasked',destination=black,source=image,x=0,y=0,resize_source=False,mask=alpha);alpha=node('GrowMask',mask=alpha,expand=1,tapered_corners=True)
  rect=v['head_crop'] if part=='head' else v['body_crop'] if part=='body' else v['crops'][part]
  x,y,w,h=placements[part];color=scale(crop(image,rect),w,h);mi=scale(crop(node('MaskToImage',mask=alpha),rect),w,h);am=node('ImageToMask',image=mi,channel='red');rgb=node('ImageCompositeMasked',destination=canvas,source=color,x=x,y=y,resize_source=False);full=node('MaskComposite',destination=node('SolidMask',value=0.,width=768,height=768),source=am,x=x,y=y,operation='add');rgba=node('JoinImageWithAlpha',image=rgb,alpha=node('InvertMask',mask=full));save=node('SaveImage',images=rgba,filename_prefix='ProjectPA/SixPartRig_20261105/'+key+'/'+part);saveids[save[0]]=[key,part]
(p/'parts_api.json').write_text(json.dumps(g,indent=2));(p/'input_map.json').write_text(json.dumps(mapping,indent=2));(p/'placements.json').write_text(json.dumps(placements,indent=2));r=requests.post(u+'/prompt',json={'prompt':g,'client_id':str(uuid.uuid4())},timeout=60);r.raise_for_status();pid=r.json()['prompt_id'];print(pid,flush=True)
for _ in range(600):
 h=requests.get(u+'/history/'+pid,timeout=30).json()
 if pid in h:
  item=h[pid];assert item['status']['status_str']=='success',item['status'];(p/'parts_history.json').write_text(json.dumps(item,indent=2))
  for k,(key,part) in saveids.items():
   d=item['outputs'][k]['images'][0];r=requests.get(u+'/view',params=d,timeout=60);r.raise_for_status();folder=p/'sprites'/key;folder.mkdir(parents=True,exist_ok=True);(folder/(part+'.png')).write_bytes(r.content)
  print('SUCCESS: 18 parts',flush=True);break
 time.sleep(2)
else:raise TimeoutError(pid)
