from pathlib import Path
import json,time,requests,math
from PIL import Image,ImageDraw,ImageFilter
ROOT=Path('D:/Project PA');OUT=ROOT/'ArtPipeline/ComfyUI/Examples/FaceIdentity_20261106';OUT.mkdir(exist_ok=True)
U='http://127.0.0.1:8188'
def make(kind,source,layers):
 g={}
 def n(t,**kw):
  k=str(len(g)+1);g[k]={'class_type':t,'inputs':kw};return [k,0]
 def load(p):
  with p.open('rb') as f:r=requests.post(U+'/upload/image',files={'image':(kind+'_'+p.name,f,'image/png')},data={'overwrite':'true'});r.raise_for_status()
  return n('LoadImage',image=r.json()['name'])
 base=load(source);cur=base
 for name,color,fun in layers:
  im=Image.new('L',(3072,3072));d=ImageDraw.Draw(im);fun(d);im=im.filter(ImageFilter.GaussianBlur(10)) if name=='face_clean' else im;mp=OUT/(kind+'_'+name+'_mask.png');im.resize((768,768),Image.Resampling.LANCZOS).save(mp)
  m=n('ImageToMask',image=load(mp),channel='red');cur=n('ImageCompositeMasked',destination=cur,source=n('EmptyImage',width=768,height=768,batch_size=1,color=color),mask=m,x=0,y=0,resize_source=False)
 a=n('SaveImage',images=n('JoinImageWithAlpha',image=cur,alpha=[base[0],1]),filename_prefix='PA_FaceIdentity/'+kind+'_master')
 p=n('ImageCompositeMasked',destination=n('EmptyImage',width=768,height=768,batch_size=1,color=0xf9f7ef),source=cur,mask=n('InvertMask',mask=[base[0],1]),x=0,y=0,resize_source=False)
 b=n('SaveImage',images=p,filename_prefix='PA_FaceIdentity/'+kind+'_preview')
 (OUT/(kind+'_api.json')).write_text(json.dumps(g,indent=2),encoding='utf-8')
 res=requests.post(U+'/prompt',json={'prompt':g});res.raise_for_status();pid=res.json()['prompt_id']
 h=None
 for _ in range(90):
  h=requests.get(U+'/history/'+pid).json().get(pid)
  if h:break
  time.sleep(1)
 assert h and h['status']['status_str']=='success',h
 for node,name in [(a,'master'),(b,'preview')]:
  v=h['outputs'][node[0]]['images'][0];(OUT/(kind+'_'+name+'.png')).write_bytes(requests.get(U+'/view',params=v).content)
 (OUT/(kind+'_run.json')).write_text(json.dumps({'prompt_id':pid,'status':h['status']},indent=2),encoding='utf-8')
 print(kind,pid)
def E(d,b,fill=255):d.ellipse(tuple(round(v*4) for v in b),fill=fill)
def R(d,b,rad=0,fill=255):d.rounded_rectangle(tuple(round(v*4) for v in b),radius=round(rad*4),fill=fill)
def L(d,pts,w=5):
 d.line([(round(x*4),round(y*4)) for x,y in pts],fill=255,width=round(w*4),joint='curve')
 for x,y in [pts[0],pts[-1]]:E(d,(x-w/2,y-w/2,x+w/2,y+w/2))
def Q(d,a,b,c,w=5):
 L(d,[((1-t)**2*a[0]+2*(1-t)*t*b[0]+t*t*c[0],(1-t)**2*a[1]+2*(1-t)*t*b[1]+t*t*c[1]) for t in [i/60 for i in range(61)]],w)
def hamster_clean(d):E(d,(332,313,436,365))
def hamster_muzzle(d):E(d,(313,305,387,366));E(d,(381,300,455,361))
def hamster_blush(d):E(d,(226,323,265,347));E(d,(514,310,553,334))
def hamster_mouth(d):Q(d,(359,332),(383,362),(408,328),5)
def tooth_border(d):R(d,(372,343,396,367),5)
def tooth(d):R(d,(375,344,393,364),3)
def tooth_split(d):L(d,[(384,344),(384,356)],2)
make('hamster',ROOT/'ArtPipeline/ComfyUI/Examples/Hamster_New_20261106/round_brows_master.png',[
 ('clean',0xf7f2e5,hamster_clean),('cheek_pouches',0xfffbef,hamster_muzzle),('blush',0xf0d7c4,hamster_blush),('smile',0x382e25,hamster_mouth),('tooth_border',0x48392b,tooth_border),('teeth',0xfffdf4,tooth),('tooth_split',0x9c8870,tooth_split)])
def poodle_clean(d):
 for b in [(276,325,333,385),(419,310,474,372),(263,267,307,307),(436,258,479,291)]:E(d,b)
def muzzle_clean(d):E(d,(302,352,458,459))
def eyes(d):E(d,(307,335,334,370));E(d,(419,325,446,360))
def eye_glint(d):E(d,(313,340,319,346));E(d,(425,330,431,336))
def eyebrows(d):Q(d,(308,311),(320,303),(331,307),5);Q(d,(420,299),(432,290),(444,296),5)
def nose(d):E(d,(365,369,399,397))
def nose_glint(d):E(d,(372,374,382,379))
def mouth(d):L(d,[(382,395),(382,404)],5);E(d,(372,406,395,430))
def tongue(d):E(d,(376,419,391,428))
def smile(d):Q(d,(362,403),(382,415),(403,400),4)
def foot_clean(d):E(d,(473,669,483,680))
def outline(d):
 Q(d,(489,433),(508,463),(520,489),7)
 Q(d,(520,489),(536,509),(538,527),6)
 # Make the outer forearm curves continuous without doubling inner seams.
 # Outer arm curve already exists; do not duplicate it.
 Q(d,(257,450),(237,474),(225,494),7)
 # Connect the small gaps at the feet.
 # Keep the existing foot outline.
make('poodle',ROOT/'ArtPipeline/Rigging/Poodle_20261106/PoseMatch/master.png',[
 ('face_clean',0xd99c5c,poodle_clean),('muzzle_clean',0xf7e3b3,muzzle_clean),('eyes',0x241b18,eyes),('eye_glints',0xfffbef,eye_glint),('brows',0x483125,eyebrows),('round_nose',0x30201c,nose),('nose_glint',0xdac2a3,nose_glint),('mouth',0x30201c,mouth),('tongue',0xd98d85,tongue),('smile',0x30201c,smile),('foot_clean',0xdda260,foot_clean),('outline_repair',0x15121a,outline)])
