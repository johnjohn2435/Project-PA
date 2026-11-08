from pathlib import Path
from PIL import Image
import requests,json,time,uuid
p=Path(r'C:\Users\wsuk2\.codex\visualizations\2026\09\17\01a0aff8-5a1f-7303-8305-4df07e9f511e\sixpart_rig_20261105');u='http://127.0.0.1:8188';g={};n=0;mapping={}
def node(c,**kw):
 global n
 n+=1;g[str(n)]={'class_type':c,'inputs':kw};return [str(n),0]
def load(f):
 with f.open('rb') as fp:r=requests.post(u+'/upload/image',files={'image':('pa_common_'+f.parent.name+'_'+f.name,fp,'image/png')},data={'overwrite':'true'},timeout=60)
 r.raise_for_status();nm=r.json()['name'];mapping[nm]=str(f.relative_to(p));v=node('LoadImage',image=nm);return v,[v[0],1]
outputs={}
for key,color in [('char_dog',(246,236,206)),('char_hamster',(221,168,104))]:
 f=p/'inputs'/(key+'_tint.png');Image.new('RGB',(768,768),color).save(f);tint,_=load(f)
 for part in ['body','arm_L','arm_R','leg_L','leg_R']:
  image,mask=load(p/'sprites/char_cat'/(part+'.png'));colored=node('ImageBlend',image1=image,image2=tint,blend_factor=1.,blend_mode='overlay');rgba=node('JoinImageWithAlpha',image=colored,alpha=mask);save=node('SaveImage',images=rgba,filename_prefix='ProjectPA/SixPartRig_20261105/common/'+key+'/'+part);outputs[save[0]]=[key,part]
(p/'common_body_api.json').write_text(json.dumps(g,indent=2));(p/'common_body_input_map.json').write_text(json.dumps(mapping,indent=2));r=requests.post(u+'/prompt',json={'prompt':g,'client_id':str(uuid.uuid4())},timeout=60);r.raise_for_status();pid=r.json()['prompt_id']
for _ in range(120):
 h=requests.get(u+'/history/'+pid,timeout=30).json()
 if pid in h:
  item=h[pid];assert item['status']['status_str']=='success',item['status'];(p/'common_body_history.json').write_text(json.dumps(item,indent=2))
  for k,(key,part) in outputs.items():
   d=item['outputs'][k]['images'][0];r=requests.get(u+'/view',params=d,timeout=60);r.raise_for_status();(p/'sprites'/key/(part+'.png')).write_bytes(r.content)
  print('10 common anatomy skin parts output');break
 time.sleep(1)
