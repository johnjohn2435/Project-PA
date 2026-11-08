from pathlib import Path
import requests,json,time
root=Path(__file__).parent;out=root/'Outlined';out.mkdir(exist_ok=True);url='http://127.0.0.1:8188'
with (root/'F/raw.png').open('rb') as f:
 r=requests.post(url+'/upload/image',files={'image':('goyeongi_minimal_F.png',f,'image/png')},data={'subfolder':'ProjectPA/20261104','overwrite':'false'},timeout=30);r.raise_for_status();up=r.json()
g={
'1':{'class_type':'LoadImage','inputs':{'image':up['subfolder']+'/'+up['name']}},
'2':{'class_type':'LoadBackgroundRemovalModel','inputs':{'bg_removal_name':'birefnet.safetensors'}},
'3':{'class_type':'RemoveBackground','inputs':{'bg_removal_model':['2',0],'image':['1',0]}},
'4':{'class_type':'GrowMask','inputs':{'mask':['3',0],'expand':11,'tapered_corners':True}},
'5':{'class_type':'EmptyImage','inputs':{'width':768,'height':768,'batch_size':1,'color':0x24211f}},
'6':{'class_type':'ImageCompositeMasked','inputs':{'destination':['5',0],'source':['1',0],'x':0,'y':0,'resize_source':False,'mask':['3',0]}},
'7':{'class_type':'InvertMask','inputs':{'mask':['4',0]}},
'8':{'class_type':'JoinImageWithAlpha','inputs':{'image':['6',0],'alpha':['7',0]}},
'9':{'class_type':'SaveImage','inputs':{'images':['8',0],'filename_prefix':'ProjectPA/Goyeongi_Minimal_20261104/Outlined/master'}},
'10':{'class_type':'ImageScale','inputs':{'image':['8',0],'upscale_method':'lanczos','width':512,'height':512,'crop':'disabled'}},
'11':{'class_type':'SaveImage','inputs':{'images':['10',0],'filename_prefix':'ProjectPA/Goyeongi_Minimal_20261104/Outlined/sprite_512'}}}
(out/'api.json').write_text(json.dumps(g,indent=2),encoding='utf-8')
r=requests.post(url+'/prompt',json={'prompt':g},timeout=30);r.raise_for_status();pid=r.json()['prompt_id'];print(pid,flush=True)
for i in range(120):
 h=requests.get(url+'/history/'+pid,timeout=30).json()
 if pid in h:
  item=h[pid];(out/'history.json').write_text(json.dumps(item,indent=2),encoding='utf-8')
  if item['status']['status_str']!='success':raise RuntimeError(item['status'])
  for nid,node in item['outputs'].items():
   for desc in node.get('images',[]):
    r=requests.get(url+'/view',params=desc,timeout=30);r.raise_for_status();(out/(g[nid]['inputs']['filename_prefix'].split('/')[-1]+'.png')).write_bytes(r.content)
  print('SUCCESS',flush=True);break
 time.sleep(2)
