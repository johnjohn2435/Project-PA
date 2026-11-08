from pathlib import Path
import argparse,json,time,requests
parser=argparse.ArgumentParser();parser.add_argument('--url',default='http://127.0.0.1:8188');args=parser.parse_args()
if not args.url.startswith(('http://127.0.0.1:','http://localhost:')):raise ValueError('localhost only')
root=Path(__file__).parent;g=json.loads((root/'comfy/final_api.json').read_text());mapping=json.loads((root/'comfy/input_map.json').read_text())
for node in g.values():
 if node['class_type']!='LoadImage':continue
 original=node['inputs']['image'];file=root/mapping[original]
 with file.open('rb') as f:
  r=requests.post(args.url+'/upload/image',files={'image':(file.name,f,'image/png')},data={'subfolder':'ProjectPA/RedrawReplay','overwrite':'false'},timeout=60);r.raise_for_status();d=r.json();node['inputs']['image']=d['subfolder']+'/'+d['name']
for node in g.values():
 if node['class_type']=='SaveImage':node['inputs']['filename_prefix']='ProjectPA/RedrawReplay/'+node['inputs']['filename_prefix'].split('/')[-1]
r=requests.post(args.url+'/prompt',json={'prompt':g},timeout=60);r.raise_for_status();pid=r.json()['prompt_id'];print(pid,flush=True)
for _ in range(900):
 history=requests.get(args.url+'/history/'+pid,timeout=30).json()
 if pid in history:
  item=history[pid]
  if item['status']['status_str']!='success':raise RuntimeError(item['status'])
  dest=root/'replay';dest.mkdir(exist_ok=True)
  for nid,node in item['outputs'].items():
   for desc in node.get('images',[]):
    r=requests.get(args.url+'/view',params=desc,timeout=60);r.raise_for_status();(dest/(g[nid]['inputs']['filename_prefix'].split('/')[-1]+'.png')).write_bytes(r.content)
  print(dest);break
 time.sleep(2)
else:raise TimeoutError(pid)
