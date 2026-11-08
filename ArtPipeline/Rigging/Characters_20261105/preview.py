from pathlib import Path
from PIL import Image,ImageDraw,ImageFont,ImageOps
import math,json
p=Path(__file__).parent;parts=p/'parts';W=H=768
bones={'body':{'parent':'root','position_px':[384,500],'pivot_px':[384,500],'size':[768,768]},'arm_L':{'parent':'body','position_px':[228,500],'pivot_px':[48,30],'size':[96,144]},'arm_R':{'parent':'body','position_px':[540,490],'pivot_px':[48,30],'size':[96,144]},'leg_L':{'parent':'body','position_px':[350,615],'pivot_px':[48,27],'size':[96,128]},'leg_R':{'parent':'body','position_px':[430,605],'pivot_px':[48,27],'size':[96,128]}}
rig={'schema_version':1,'canvas_px':[768,768],'pixels_per_unit':256,'root_px':[384,700],'default_facing':'right_three_quarter','left_facing':'mirror_root_x','vertical_motion':'reuse last left/right facing','draw_order':['leg_R','arm_R','leg_L','arm_L','body'],'bones':bones,'note':'L/R are fixed skeleton slot names, never renamed on mirroring. Both arms are drawn behind body to hide shoulder caps.'}
for name,b in bones.items():
 parentpos=rig['root_px'] if b['parent']=='root' else bones[b['parent']]['position_px'];x,y=b['position_px'];px,py=b['pivot_px'];w,h=b['size'];b['unity_local_position']=[(x-parentpos[0])/256,(parentpos[1]-y)/256,0];b['sprite_pivot_normalized']=[px/w,1-py/h]
(p/'shared_rig.json').write_text(json.dumps(rig,indent=2),encoding='utf-8')
walk={'duration_seconds':.64,'loop':True,'tracks':{'body.localPositionY':{'function':'base + 4*abs(sin(2*pi*t/duration)) pixels'},'arm_L.localRotationZ':{'amplitude_degrees':-18,'phase':0},'arm_R.localRotationZ':{'amplitude_degrees':18,'phase':0},'leg_L.localRotationZ':{'amplitude_degrees':22,'phase':0},'leg_R.localRotationZ':{'amplitude_degrees':-22,'phase':0}},'note':'Same transforms/curves used for every skin. Raster GIF is a review render, not Unity runtime validation.'}
(p/'shared_walk.json').write_text(json.dumps(walk,indent=2),encoding='utf-8')
def render(key,phase=0,walk=False,left=False):
 c=Image.new('RGBA',(768,768));bob=-4*abs(math.sin(phase)) if walk else 0
 for name in rig['draw_order']:
  b=bones[name];x,y=b['position_px'];y+=bob;px,py=b['pivot_px'];im=Image.open(parts/key/(name+'.png')).convert('RGBA');layer=Image.new('RGBA',(768,768));layer.alpha_composite(im,(round(x-px),round(y-py)))
  amp={'arm_L':-18,'arm_R':18,'leg_L':22,'leg_R':-22}.get(name,0);angle=amp*math.sin(phase) if walk else 0
  if angle:layer=layer.rotate(angle,Image.Resampling.BICUBIC,center=(x,y))
  c=Image.alpha_composite(c,layer)
 return ImageOps.mirror(c) if left else c
names={'goyeongi':'고영이','dog':'강아지','hamster':'햄스터'}
board=Image.new('RGB',(1080,510),'#f6f2e9');d=ImageDraw.Draw(board);font=lambda n:ImageFont.truetype('C:/Windows/Fonts/malgun.ttf',n)
for i,(key,name) in enumerate(names.items()):
 im=render(key);im.save(parts/key/'assembled_right.png');render(key,left=True).save(parts/key/'assembled_left.png');im=im.resize((360,360),Image.Resampling.LANCZOS);board.paste(im,(i*360,50),im);d.text((i*360+118,425),name,font=font(25),fill='#41352e')
board.save(p/'lineup.png')
frames=[]
for i in range(16):
 phase=2*math.pi*i/16;c=Image.new('RGB',(900,350),'#f6f2e9');d=ImageDraw.Draw(c)
 for j,(key,name) in enumerate(names.items()):
  im=render(key,phase,True).resize((300,300),Image.Resampling.LANCZOS);c.paste(im,(j*300,0),im);d.text((j*300+100,305),name,font=font(20),fill='#41352e')
 frames.append(c)
frames[0].save(p/'shared_walk_preview.gif',save_all=True,append_images=frames[1:],duration=40,loop=0,disposal=2)
print('Rendered same skeleton and gait for all 3 skins.')
