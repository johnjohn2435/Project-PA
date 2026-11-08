from pathlib import Path
import json,time,requests
from PIL import Image, ImageDraw
P=Path('D:/Project PA/ArtPipeline/ComfyUI/Examples/Hamster_New_20261106')
url='http://127.0.0.1:8188'
g={}
def node(t,**kw):
 k=str(len(g)+1);g[k]={'class_type':t,'inputs':kw};return [k,0]
def upload(p):
 with open(p,'rb') as f:r=requests.post(url+'/upload/image',files={'image':(p.name,f,'image/png')},data={'overwrite':'true'});r.raise_for_status();return r.json()['name']
def mask(name,draw):
 im=Image.new('L',(3072,3072));d=ImageDraw.Draw(im);draw(d);p=P/(name+'.png');im.resize((768,768),Image.Resampling.LANCZOS).save(p);return node('ImageToMask',image=node('LoadImage',image=upload(p)),channel='red')
def rect(d,box,fill=255):d.rectangle(tuple(int(x*4) for x in box),fill=fill)
def ellipse(d,box,fill=255):d.ellipse(tuple(int(x*4) for x in box),fill=fill)
base=node('LoadImage',image=upload(P/'ivory_master.png'));cur=base
# Start from the original round eyes. Only replace the straight eyebrows.
def erase(d):
 for b in [(262,226,301,245),(467,213,505,233)]:rect(d,b)
def brows(d):
 for x,y in [(281,235),(486,222)]:
  points=[]
  for i in range(41):
   t=i/40
   points.append((round(4*(x-11+22*t)),round(4*(y-6*4*t*(1-t)))))
  d.line(points,fill=255,width=20,joint='curve')
  for xx in [x-11,x+11]:ellipse(d,(xx-2.5,y-2.5,xx+2.5,y+2.5))
for name,fun,color in [('round_brows_erase',erase,0xf7f2e5),('round_brows_shape',brows,0x483c2e)]:
 m=mask(name,fun);cur=node('ImageCompositeMasked',destination=cur,source=node('EmptyImage',width=768,height=768,batch_size=1,color=color),mask=m,x=0,y=0,resize_source=False)
alpha=[base[0],1]
master=node('JoinImageWithAlpha',image=cur,alpha=alpha)
a=node('SaveImage',images=master,filename_prefix='PA_Hamster_RoundBrows_Master')
prev=node('ImageCompositeMasked',destination=node('EmptyImage',width=768,height=768,batch_size=1,color=0xf9f7ef),source=cur,mask=node('InvertMask',mask=alpha),x=0,y=0,resize_source=False)
b=node('SaveImage',images=prev,filename_prefix='PA_Hamster_RoundBrows_Preview')
(P/'round_brows_api.json').write_text(json.dumps(g,indent=2),encoding='utf-8')
r=requests.post(url+'/prompt',json={'prompt':g});r.raise_for_status();pid=r.json()['prompt_id']
for _ in range(120):
 h=requests.get(url+'/history/'+pid).json().get(pid)
 if h:break
 time.sleep(1)
assert h and h['status']['status_str']=='success',h
for n,name in [(a,'round_brows_master.png'),(b,'round_brows_preview.png')]:
 out=h['outputs'][n[0]]['images'][0];(P/name).write_bytes(requests.get(url+'/view',params=out).content)
(P/'round_brows_run.json').write_text(json.dumps({'prompt_id':pid,'status':h['status']},indent=2),encoding='utf-8')
print('ComfyUI round_brows hamster complete',pid)
