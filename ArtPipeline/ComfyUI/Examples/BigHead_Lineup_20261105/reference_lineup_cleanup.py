from pathlib import Path
from PIL import Image,ImageDraw,ImageFilter
import requests,json,time,uuid
out=Path(r'C:\Users\wsuk2\.codex\visualizations\2026\09\17\01a0aff8-5a1f-7303-8305-4df07e9f511e\reference_lineup_20261105');u='http://127.0.0.1:8188';im=Image.open(out/'redraw.png').convert('RGB');g={};n=0
inputdir=out/'cleanup_inputs';inputdir.mkdir(exist_ok=True)
def upload(p):
 with p.open('rb') as f:r=requests.post(u+'/upload/image',files={'image':('pa_lineup_'+p.name,f,'image/png')},data={'overwrite':'true'},timeout=60)
 r.raise_for_status();return r.json()['name']
def load(p):
 global n
 n+=1;g[str(n)]={'class_type':'LoadImage','inputs':{'image':upload(p)}};return str(n)
base=load(out/'redraw.png')
def edit(name,shapes,color):
 global n,base
 mask=Image.new('RGB',im.size,'black');d=ImageDraw.Draw(mask)
 for typ,coords in shapes:
  getattr(d,typ)(coords,fill='white')
 if name.endswith('forehead'):
  for x in range(im.width):
   if mask.getpixel((x,250))[0]:
    hits=[y for y in range(190,255) if max(abs(im.getpixel((x,y))[k]-im.getpixel((x,160))[k]) for k in range(3))>40]
    if hits:
     for y in range(min(hits)+6):mask.putpixel((x,y),(0,0,0))
 mask=mask.filter(ImageFilter.GaussianBlur(.65));p=inputdir/(name+'_mask.png');mask.save(p);m=load(p);n+=1;mid=str(n);g[mid]={'class_type':'ImageToMask','inputs':{'image':[m,0],'channel':'red'}}
 p=inputdir/(name+'_color.png');Image.new('RGB',im.size,color).save(p);c=load(p);n+=1;new=str(n);g[new]={'class_type':'ImageCompositeMasked','inputs':{'destination':[base,0],'source':[c,0],'x':0,'y':0,'resize_source':False,'mask':[mid,0]}};base=new
# Source-following masks remove only tails outside the newly painted bodies.
edit('cat_tail',[('polygon',[(92,448),(119,449),(135,478),(141,497),(148,514),(106,518),(91,492)])],im.getpixel((91,450)))
edit('dog_tail',[('polygon',[(645,463),(685,460),(683,514),(633,514),(637,500),(641,480)])],im.getpixel((684,467)))
edit('dog_forehead',[('rectangle',(547,200,597,262))],im.getpixel((601,260)))
edit('dog_edge',[('polygon',[(642,460),(647,460),(647,478),(643,497),(637,513),(631,519),(628,515),(632,510),(638,495),(642,478)])],(14,12,10))
brows=[];dots=[]
for x,y in [(158,333),(265,328),(526,300),(644,286),(889,331),(984,325)]:
 brows.append(('rounded_rectangle',(x-6,y-25,x+6,y-22)))
 dots.append(('ellipse',(x-4,y-6,x-1,y-3)))
edit('brows',brows,(46,37,30));edit('eye_glints',dots,(255,253,243))
n+=1;g[str(n)]={'class_type':'SaveImage','inputs':{'images':[base,0],'filename_prefix':'ProjectPA/ReferenceLineup_20261105/final'}}
(out/'cleanup_api.json').write_text(json.dumps(g,indent=2));r=requests.post(u+'/prompt',json={'prompt':g,'client_id':str(uuid.uuid4())},timeout=60);r.raise_for_status();pid=r.json()['prompt_id']
for _ in range(120):
 h=requests.get(u+'/history/'+pid,timeout=30).json()
 if pid in h:
  item=h[pid];assert item['status']['status_str']=='success',item['status'];(out/'cleanup_history.json').write_text(json.dumps(item,indent=2));d=item['outputs'][str(n)]['images'][0];r=requests.get(u+'/view',params=d,timeout=60);r.raise_for_status();(out/'lineup_final.png').write_bytes(r.content);print('SUCCESS');break
 time.sleep(1)


