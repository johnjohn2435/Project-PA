from pathlib import Path
import json,math,hashlib
from PIL import Image,ImageDraw,ImageFont
import numpy as np
p=Path(r'C:\Users\wsuk2\.codex\visualizations\2026\09\17\01a0aff8-5a1f-7303-8305-4df07e9f511e\sixpart_rig_20261105');out=p/'preview';out.mkdir(exist_ok=True)
parts=['body','head','arm_L','arm_R','leg_L','leg_R'];pivots={'root':[384,710],'body':[384,580],'head':[384,480],'arm_L':[280,520],'arm_R':[484,520],'leg_L':[320,640],'leg_R':[448,640]};order=['leg_R','leg_L','body','arm_R','arm_L','head'];keys=['char_cat','char_dog','char_hamster'];labels=['고영이','가나지','햄쥐'];font=ImageFont.truetype('C:/Windows/Fonts/malgun.ttf',23)
spec={'schema':1,'canvas':[768,768],'pixels_per_unit':256,'root_pixel':pivots['root'],'draw_order':order,'skins':[{'key':k,'label':l} for k,l in zip(keys,labels)],'bones':[],'art_facing':'front-biased; current artwork is not a side-view solution','rig_type':'six rigid cutout transforms; no SpriteSkin required'}
for part in parts:
 parent='root' if part=='body' else 'body';x,y=pivots[part];px,py=pivots[parent];spec['bones'].append({'name':part,'parent':parent,'path':part if part=='body' else 'body/'+part,'pivot_pixel':[x,y],'unity_local_position':[(x-px)/256,(py-y)/256,0],'sprite_pivot_normalized':[x/768,1-y/768]})
(p/'shared_rig.json').write_text(json.dumps(spec,ensure_ascii=False,indent=2),encoding='utf-8')
def pose(mode,t):
 phase=2*math.pi*t
 angles={x:0. for x in parts};move={x:[0.,0.] for x in parts}
 if mode=='Idle':
  move['body'][1]=-1.5*math.sin(phase);angles['head']=1.2*math.sin(phase);angles['arm_L']=1.2*math.sin(phase);angles['arm_R']=-1.2*math.sin(phase)
 if mode=='Walk':
  move['body'][1]=-3*abs(math.sin(phase));angles.update(head=1.5*math.sin(phase),arm_L=-10*math.sin(phase),arm_R=10*math.sin(phase),leg_L=9*math.sin(phase),leg_R=-9*math.sin(phase));move['leg_L'][1]=-4*max(0.,math.sin(phase));move['leg_R'][1]=-4*max(0.,-math.sin(phase))
 return angles,move
def trans(x,y):return np.array([[1,0,x],[0,1,y],[0,0,1.]])
def rotation(deg):
 a=math.radians(-deg);c=math.cos(a);s=math.sin(a);return np.array([[c,-s,0],[s,c,0],[0,0,1.]])
sprites={k:{n:Image.open(p/'sprites'/k/(n+'.png')).convert('RGBA') for n in parts} for k in keys}
def render(key,mode='Neutral',t=0):
 angles,move=pose(mode,t);result=Image.new('RGBA',(768,768));body=trans(*move['body'])@trans(*pivots['body'])@rotation(angles['body'])@trans(*[-x for x in pivots['body']])
 for name in order:
  x,y=pivots[name];dx,dy=move[name]
  matrix=body if name=='body' else body@trans(dx,dy)@trans(x,y)@rotation(angles[name])@trans(-x,-y)
  inv=np.linalg.inv(matrix);layer=sprites[key][name].transform((768,768),Image.Transform.AFFINE,tuple(inv[:2,:].flatten()),Image.Resampling.BICUBIC);result.alpha_composite(layer)
 return result

def row(mode,t=0):
 im=Image.new('RGB',(1152,430),(249,246,232));d=ImageDraw.Draw(im)
 for i,k in enumerate(keys):
  spr=render(k,mode,t).resize((384,384),Image.Resampling.LANCZOS);im.paste(spr,(i*384,0),spr);d.text((i*384+192,396),labels[i],font=font,anchor='mm',fill='#4a392d')
 return im
row('Neutral').save(out/'lineup.png')
clips={'schema':1,'rotation_units':'Unity degrees about local Z','position_units':'Unity units','sample_interpolation':'linear','clips':[]}
for mode,duration in [('Idle',1.6),('Walk',.8)]:
 frames=[row(mode,i/32) for i in range(32)];frames[0].save(out/(mode.lower()+'.gif'),save_all=True,append_images=frames[1:],duration=round(duration*1000/32),loop=0,disposal=2)
 curves=[]
 for name in parts:
  path=name if name=='body' else 'body/'+name
  for prop in ['localEulerAnglesRaw.z','m_LocalPosition.x','m_LocalPosition.y','m_LocalPosition.z']:
   samples=[]
   bone=next(x for x in spec['bones'] if x['name']==name)
   for i in range(33):
    angles,move=pose(mode,i/32);value=angles[name] if prop=='localEulerAnglesRaw.z' else bone['unity_local_position'][1]-move[name][1]/256 if prop=='m_LocalPosition.y' else bone['unity_local_position'][0] if prop=='m_LocalPosition.x' else 0.;samples.append([duration*i/32,value])
   curves.append({'path':path,'property':prop,'keys':samples})
 clips['clips'].append({'name':mode,'duration':duration,'loop':True,'curves':curves})
(p/'shared_animation.json').write_text(json.dumps(clips,indent=2))
for key in keys:render(key).save(out/(key+'.png'))
# Show actual extracted parts in a labelled inspection sheet.
sheet=Image.new('RGB',(1152,750),(249,246,232));d=ImageDraw.Draw(sheet)
for r,key in enumerate(keys):
 for c,name in enumerate(['head','body','arm_L','arm_R','leg_L','leg_R']):
  im=sprites[key][name];box=im.getbbox();thumb=im.crop(box);thumb.thumbnail((160,190),Image.Resampling.LANCZOS);x=c*192+(192-thumb.width)//2;y=r*250+15+(190-thumb.height)//2;sheet.paste(thumb,(x,y),thumb);d.text((c*192+96,r*250+224),name,font=font,anchor='mm',fill='#4a392d')
sheet.save(out/'parts.png')
ext=Image.new('RGB',(1152,430*3),(249,246,232))
for i,t in enumerate([0,.25,.75]):ext.paste(row('Walk',t),(0,i*430))
ext.save(out/'walk_extremes.png')
validation={'pngs':[],'curves_reused_for_all_skins':True,'shared_hierarchy_and_pivots':True,'unity_execution':'pending','scope':'Idle and small in-place Walk; not a validated side view or attack/jump','visual_review':'pending'}
for key in keys:
 for name in parts:
  f=p/'sprites'/key/(name+'.png');im=Image.open(f);a=np.array(im.getchannel('A'));assert im.size==(768,768) and im.mode=='RGBA';assert a.max()>=254 and a.min()==0;assert (a>128).sum()>50;assert a[0].max()==0 and a[-1].max()==0
  validation['pngs'].append({'skin':key,'part':name,'sha256':hashlib.sha256(f.read_bytes()).hexdigest(),'bounds':list(im.getbbox()),'opaque_pixels':int((a>128).sum()),'pivot_alpha':int(a[pivots[name][1],pivots[name][0]])})
(p/'validation.json').write_text(json.dumps(validation,indent=2));print('18 PNGs validated; shared pivots, clips and actual sprite previews written')

