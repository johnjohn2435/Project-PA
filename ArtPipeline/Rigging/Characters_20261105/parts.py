from pathlib import Path
from PIL import Image
import json,requests,time
root=Path('D:/Project PA/ArtPipeline/ComfyUI/Approved');out=Path(__file__).parent/'parts';out.mkdir(exist_ok=True);url='http://127.0.0.1:8188'
chars=[('goyeongi','Goyeongi/v3',(160,160,450,405),[(283,323,43,69),(442,323,43,69)],(355,363,62,36)),('dog','Dog/v2',(135,155,505,400),[(294,326,37,65),(453,326,37,65)],(348,371,87,46)),('hamster','Hamster/v2',(145,140,495,450),[(291,324,41,68),(452,324,41,68)],(351,371,82,45))]
for key,version,crop,eyes,mouth in chars:
 folder=out/key;folder.mkdir(exist_ok=True);source=root/version/'master.png';im=Image.open(source).convert('RGBA');rgb=im.getpixel((384,325))[:3];basecolor=(rgb[0]<<16)+(rgb[1]<<8)+rgb[2];shadowrgb=tuple(int(v*.87) for v in rgb);shadowcolor=(shadowrgb[0]<<16)+(shadowrgb[1]<<8)+shadowrgb[2]
 with source.open('rb') as f:
  r=requests.post(url+'/upload/image',files={'image':(key+'_rig_source.png',f,'image/png')},data={'subfolder':'ProjectPA/RigReady','overwrite':'false'},timeout=60);r.raise_for_status();up=r.json()
 g={}
 def add(kind,**inputs):
  k=str(len(g)+1);g[k]={'class_type':kind,'inputs':inputs};return [k,0]
 def color(w,h,c):return add('EmptyImage',width=w,height=h,batch_size=1,color=c)
 def cropimage(image,box):
  x,y,w,h=box;return add('ImageCrop',image=image,width=w,height=h,x=x,y=y)
 def scale(image,w,h):return add('ImageScale',image=image,upscale_method='lanczos',width=w,height=h,crop='disabled')
 def comp(dest,source,x=0,y=0,mask=None):
  args=dict(destination=dest,source=source,x=x,y=y,resize_source=False)
  if mask is not None:args['mask']=mask
  return add('ImageCompositeMasked',**args)
 def mcomp(a,b,x=0,y=0,op='add'):return add('MaskComposite',destination=a,source=b,x=x,y=y,operation=op)
 def rect(W,H,w,h,x,y):return mcomp(add('SolidMask',value=0.,width=W,height=H),add('SolidMask',value=1.,width=w,height=h),x,y)
 def save(image,name):add('SaveImage',images=image,filename_prefix='ProjectPA/RigParts_20261105/'+key+'/'+name)
 load=add('LoadImage',image=up['subfolder']+'/'+up['name']);alpha=add('InvertMask',mask=[load[0],1]);base=load
 # Clean old front-facing facial marks, then place separate features in a right-turned perspective.
 patchmask=add('SolidMask',value=1.,width=330,height=210);patchmask=add('FeatherMask',mask=patchmask,left=40,top=35,right=40,bottom=40)
 base=comp(base,color(330,210,basecolor),230,280,patchmask)
 base=comp(base,load,mask=rect(768,768,768,315,0,0))
 for box,x,y,w,h in [(eyes[0],355,323,44,70),(eyes[1],492,328,29,60),(mouth,435,381,75,42)]:
  original=cropimage(load,box);dark=add('ImageToMask',image=original,channel='red');dark=add('InvertMask',mask=dark);dark=add('ThresholdMask',mask=dark,value=.45);dark=add('GrowMask',mask=dark,expand=5,tapered_corners=True);dark=add('GrowMask',mask=dark,expand=-2,tapered_corners=True);mi=scale(add('MaskToImage',mask=dark),w,h);mi=add('ImageToMask',image=mi,channel='red');feature=scale(original,w,h);base=comp(base,feature,x,y,mi)
 # Keep a complete clean body behind the limbs; suppress embedded paws in the old dog/hamster art.
 if key=='dog':
  for x in [140,580]:base=comp(base,color(768,768,basecolor),mask=rect(768,768,85,110,x,415))
  mirror=add('ImageFlip',image=base,flip_method='y-axis: horizontally');mi=add('MaskToImage',mask=alpha);mi=add('ImageFlip',image=mi,flip_method='y-axis: horizontally');mi=add('ImageToMask',image=mi,channel='red');region=rect(768,768,115,150,575,290);outside=add('InvertMask',mask=region);allowed=mcomp(mi,outside);alpha=mcomp(alpha,allowed,op='multiply');base=comp(base,mirror,mask=region)
 if key=='hamster':
  for x in [155,555]:base=comp(base,color(768,768,basecolor),mask=rect(768,768,65,75,x,445))
 base=scale(cropimage(base,crop),400,470);am=add('MaskToImage',mask=alpha);am=scale(cropimage(am,crop),400,470);alpha=add('ImageToMask',image=am,channel='red')
 # Smooth ellipse template, kept continuous until final target resolution.
 z=add('SolidMask',value=0.,width=128,height=128);pt=add('SolidMask',value=1.,width=1,height=1);pt=mcomp(z,pt,64,64);gauss=add('MaskToImage',mask=pt);gauss=add('ImageBlur',image=gauss,blur_radius=31,sigma=.33);gauss=cropimage(gauss,(48,48,33,33))
 def ellipse(w,h,W,H,x,y):
  field=add('ImageScale',image=gauss,upscale_method='bicubic',width=w,height=h,crop='disabled');mask=add('ImageToMask',image=field,channel='red');mask=add('ThresholdMask',mask=mask,value=.0005);return mcomp(add('SolidMask',value=0.,width=W,height=H),mask,x,y)
 envelope=ellipse(400,420,400,470,0,50);top=rect(400,470,400,250,0,0);envelope=mcomp(envelope,top);alpha=mcomp(alpha,envelope,op='multiply')
 # Rebuild a continuous dark contour after body/foot clipping.
 erode=add('GrowMask',mask=alpha,expand=-8,tapered_corners=True);border=mcomp(alpha,erode,op='subtract');base=comp(base,color(400,470,0x24211f),mask=border)
 canvas=color(768,768,0x24211f);canvas=comp(canvas,base,184,180);fullalpha=mcomp(add('SolidMask',value=0.,width=768,height=768),alpha,184,180);trans=add('InvertMask',mask=fullalpha);body=add('JoinImageWithAlpha',image=canvas,alpha=trans);save(body,'body')
 for part in ['arm_L','arm_R','leg_L','leg_R']:
  arm=part.startswith('arm');W,H=(96,144) if arm else (96,128);w,h=(62,112) if arm else (76,90);x,y=(17,10) if arm else (10,16)
  mask=ellipse(w,h,W,H,x,y);inside=add('GrowMask',mask=mask,expand=-5,tapered_corners=True);image=color(W,H,0x24211f);image=comp(image,color(W,H,basecolor),mask=inside);shadow=mcomp(inside,rect(W,H,W,H//3,0,H*2//3),op='multiply');image=comp(image,color(W,H,shadowcolor),mask=shadow)
  if part.endswith('R'):image=comp(image,color(W,H,shadowcolor),mask=inside)
  trans=add('InvertMask',mask=mask);rgba=add('JoinImageWithAlpha',image=image,alpha=trans);save(rgba,part)
 (folder/'api.json').write_text(json.dumps(g,indent=2),encoding='utf-8');(folder/'source.json').write_text(json.dumps({'approved_source':version,'base_color':rgb,'shadow_color':shadowrgb,'facing':'right_three_quarter_feature_perspective','notes':'Existing approved face features repositioned in ComfyUI; no regeneration of identity. Five complete parts.'},indent=2),encoding='utf-8')
 r=requests.post(url+'/prompt',json={'prompt':g},timeout=60);r.raise_for_status();pid=r.json()['prompt_id'];print(key,pid,flush=True)
 for i in range(240):
  history=requests.get(url+'/history/'+pid,timeout=30).json()
  if pid in history:
   item=history[pid];(folder/'history.json').write_text(json.dumps(item,indent=2),encoding='utf-8')
   if item['status']['status_str']!='success':raise RuntimeError(item['status'])
   for nid,node in item['outputs'].items():
    for desc in node.get('images',[]):
     r=requests.get(url+'/view',params=desc,timeout=60);r.raise_for_status();(folder/(g[nid]['inputs']['filename_prefix'].split('/')[-1]+'.png')).write_bytes(r.content)
   print(key,'PARTS SUCCESS',flush=True);break
  time.sleep(1)

