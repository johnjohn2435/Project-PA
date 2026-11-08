from pathlib import Path
import json,time,requests
u='http://127.0.0.1:8189';src=Path('D:/Project PA/ArtPipeline/Rigging/Poodle_20261106');out=src/'PoseMatch';out.mkdir(exist_ok=True)
g={}
def n(t,**kw):
 k=str(len(g)+1);g[k]={'class_type':t,'inputs':kw};return [k,0]
def load(p):
 with p.open('rb') as f:r=requests.post(u+'/upload/image',files={'image':(p.name,f,'image/png')},data={'overwrite':'true'});r.raise_for_status()
 return n('LoadImage',image=r.json()['name'])
from PIL import Image,ImageDraw
mask=Image.new('L',(768,768));d=ImageDraw.Draw(mask)
# Keep the natural neck shadow.
d.ellipse((258,466,276,496),fill=255);d.ellipse((496,465,515,498),fill=255)
mask.save(out/'torso_cleanup_mask.png')
import numpy as np
Y,X=np.mgrid[:768,:768]
for part,left in [('arm_L',True),('arm_R',False)]:
 top=np.ones_like(Y)
 inner=np.clip((306-X)/25,0,1) if left else np.clip((X-473)/25,0,1)
 Image.fromarray((top*inner*255).astype('uint8')).save(out/(part+'_blend_mask.png'))
line=Image.new('L',(3072,3072));ld=ImageDraw.Draw(line)
ld.line([(4*485,4*423),(4*512,4*471),(4*524,4*498)],fill=255,width=24)
line.resize((768,768),Image.Resampling.LANCZOS).save(out/'shoulder_line_mask.png')
saves=[];parts={}
for part,angle,pivot in [('head',8,(384,420)),('arm_L',26,(286,449)),('arm_R',-26,(480,449)),('torso_mesh',0,(384,420))]:
 a=load(src/(part+'.png'))
 if part.startswith('arm_'):
  blended=n('MaskComposite',destination=n('InvertMask',mask=[a[0],1]),source=n('ImageToMask',image=load(out/(part+'_blend_mask.png')),channel='red'),x=0,y=0,operation='multiply')
  a=n('PorterDuffImageComposite',source=a,source_alpha=n('InvertMask',mask=blended),destination=a,destination_alpha=n('InvertMask',mask=blended),mode='SRC')
 b=n('PAPoseRotate',image=a,mask=[a[0],1],angle=angle,pivot_x=pivot[0],pivot_y=pivot[1]);parts[part]=b
 if part=='head':
  coverage=n('InvertMask',mask=[b[0],1])
  shifted=n('ImageCompositeMasked',destination=n('EmptyImage',width=768,height=768,batch_size=1,color=0),source=b,mask=coverage,x=0,y=28,resize_source=False)
  shiftedmask=n('MaskComposite',destination=n('SolidMask',value=0.0,width=768,height=768),source=coverage,x=0,y=28,operation='add')
  b=n('PorterDuffImageComposite',source=shifted,source_alpha=n('InvertMask',mask=shiftedmask),destination=shifted,destination_alpha=n('InvertMask',mask=shiftedmask),mode='SRC');parts[part]=b
 if part=='torso_mesh':
  mm=n('ImageToMask',image=load(out/'torso_cleanup_mask.png'),channel='red')
  filled=n('ImageCompositeMasked',destination=b,source=n('EmptyImage',width=768,height=768,batch_size=1,color=0xdda260),mask=mm,x=0,y=0,resize_source=False)
  line=n('ImageToMask',image=load(out/'shoulder_line_mask.png'),channel='red')
  filled=n('ImageCompositeMasked',destination=filled,source=n('EmptyImage',width=768,height=768,batch_size=1,color=0x111018),mask=line,x=0,y=0,resize_source=False)
  # Keep original alpha; patch only the existing torso.
  b=n('PorterDuffImageComposite',source=filled,source_alpha=[b[0],1],destination=filled,destination_alpha=[b[0],1],mode='SRC');parts[part]=b
 s=n('SaveImage',images=n('JoinImageWithAlpha',image=b,alpha=[b[0],1]),filename_prefix='PoodlePose/'+part);saves.append((s,part+'.png'))
# Source-over in the same rendering order as the rig.
c=parts['torso_mesh']
for part in ['arm_L','arm_R','head']:
 p=parts[part];c=n('PorterDuffImageComposite',source=p,source_alpha=[p[0],1],destination=c,destination_alpha=[c[0],1],mode='SRC_OVER')
s=n('SaveImage',images=n('JoinImageWithAlpha',image=c,alpha=[c[0],1]),filename_prefix='PoodlePose/master');saves.append((s,'master.png'))
bg=n('EmptyImage',width=768,height=768,batch_size=1,color=0xf9f7ef)
prev=n('ImageCompositeMasked',destination=bg,source=c,mask=n('InvertMask',mask=[c[0],1]),x=0,y=0,resize_source=False)
s=n('SaveImage',images=prev,filename_prefix='PoodlePose/preview');saves.append((s,'preview.png'))
(out/'pose_api.json').write_text(json.dumps(g,indent=2),encoding='utf-8')
r=requests.post(u+'/prompt',json={'prompt':g});r.raise_for_status();pid=r.json()['prompt_id']
for _ in range(90):
 h=requests.get(u+'/history/'+pid).json().get(pid)
 if h:break
 time.sleep(1)
assert h and h['status']['status_str']=='success',h
for s,name in saves:
 obj=h['outputs'][s[0]]['images'][0];(out/name).write_bytes(requests.get(u+'/view',params=obj).content)
(out/'run.json').write_text(json.dumps({'prompt_id':pid,'status':h['status']},indent=2),encoding='utf-8');print('POSE_COMPLETE',pid)


