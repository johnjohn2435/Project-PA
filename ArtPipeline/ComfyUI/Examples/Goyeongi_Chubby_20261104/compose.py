from pathlib import Path
import requests,json,time
p=Path(__file__).parent;out=p/'Composed';out.mkdir(exist_ok=True);url='http://127.0.0.1:8188'
with Path('D:/Project PA/ArtPipeline/ComfyUI/Approved/Goyeongi/v3/master.png').open('rb') as f:
 r=requests.post(url+'/upload/image',files={'image':('goyeongi_head_source.png',f,'image/png')},data={'subfolder':'ProjectPA/Chubby','overwrite':'false'},timeout=30);r.raise_for_status();up=r.json()
g={}
def add(kind,**inputs):
 k=str(len(g)+1);g[k]={'class_type':kind,'inputs':inputs};return [k,0]
load=add('LoadImage',image=up['subfolder']+'/'+up['name']);oldalpha=add('InvertMask',mask=[load[0],1]);zero=add('SolidMask',value=0.,width=768,height=768)
seed=add('SolidMask',value=0.,width=128,height=128);point=add('SolidMask',value=1.,width=1,height=1);seed=add('MaskComposite',destination=seed,source=point,x=64,y=64,operation='add');circle=add('MaskToImage',mask=seed);circle=add('ImageBlur',image=circle,blur_radius=31,sigma=0.33);circle=add('ImageCrop',image=circle,width=33,height=33,x=48,y=48)
def ellipse(w,h,x,y):
 im=add('ImageScale',image=circle,upscale_method='bicubic',width=w,height=h,crop='disabled');m=add('ImageToMask',image=im,channel='red');m=add('ThresholdMask',mask=m,value=0.0005);return add('MaskComposite',destination=zero,source=m,x=x,y=y,operation='add')
def rect(w,h,x,y):return add('MaskComposite',destination=zero,source=add('SolidMask',value=1.,width=w,height=h),x=x,y=y,operation='add')
colors={k:add('EmptyImage',width=768,height=768,batch_size=1,color=v) for k,v in [('dark',0x24211f),('base',0xf2f2eb),('shade',0xd4d4ce)]}
base=colors['base'];total=zero
for w,h,x,y,color in [(88,74,270,641,'shade'),(88,74,410,641,'shade'),(400,260,184,425,'base'),(100,68,155,494,'base'),(100,68,513,494,'base')]:
 mask=ellipse(w,h,x,y);inner=add('GrowMask',mask=mask,expand=-8,tapered_corners=True)
 base=add('ImageCompositeMasked',destination=base,source=colors['dark'],mask=mask,x=0,y=0,resize_source=False);base=add('ImageCompositeMasked',destination=base,source=colors[color],mask=inner,x=0,y=0,resize_source=False)
 if w==400:
  shadow=add('MaskComposite',destination=inner,source=rect(768,170,0,605),x=0,y=0,operation='multiply');base=add('ImageCompositeMasked',destination=base,source=colors['shade'],mask=shadow,x=0,y=0,resize_source=False)
 total=add('MaskComposite',destination=total,source=mask,x=0,y=0,operation='add')
head=ellipse(450,300,159,200);top=rect(768,350,0,0);head=add('MaskComposite',destination=head,source=top,x=0,y=0,operation='add');head=add('MaskComposite',destination=head,source=oldalpha,x=0,y=0,operation='multiply')
base=add('ImageCompositeMasked',destination=base,source=load,mask=head,x=0,y=0,resize_source=False);inside=add('GrowMask',mask=head,expand=-8,tapered_corners=True);border=add('MaskComposite',destination=head,source=inside,x=0,y=0,operation='subtract');base=add('ImageCompositeMasked',destination=base,source=colors['dark'],mask=border,x=0,y=0,resize_source=False);total=add('MaskComposite',destination=total,source=head,x=0,y=0,operation='add');inv=add('InvertMask',mask=total);rgba=add('JoinImageWithAlpha',image=base,alpha=inv)
add('SaveImage',images=rgba,filename_prefix='ProjectPA/Goyeongi_Chubby_20261104/Composed/master');small=add('ImageScale',image=rgba,upscale_method='lanczos',width=512,height=512,crop='disabled');add('SaveImage',images=small,filename_prefix='ProjectPA/Goyeongi_Chubby_20261104/Composed/sprite_512')
(out/'api.json').write_text(json.dumps(g,indent=2),encoding='utf-8');r=requests.post(url+'/prompt',json={'prompt':g},timeout=60);r.raise_for_status();pid=r.json()['prompt_id']
for i in range(120):
 h=requests.get(url+'/history/'+pid,timeout=30).json()
 if pid in h:
  item=h[pid];(out/'history.json').write_text(json.dumps(item,indent=2),encoding='utf-8')
  if item['status']['status_str']!='success':raise RuntimeError(item['status'])
  for nid,node in item['outputs'].items():
   for desc in node.get('images',[]):
    r=requests.get(url+'/view',params=desc,timeout=30);r.raise_for_status();(out/(g[nid]['inputs']['filename_prefix'].split('/')[-1]+'.png')).write_bytes(r.content)
  print('SUCCESS');break
 time.sleep(1)
