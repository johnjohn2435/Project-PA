from pathlib import Path
from PIL import Image
import requests,json,time,uuid
out=Path(r'C:\Users\wsuk2\.codex\visualizations\2026\09\17\01a0aff8-5a1f-7303-8305-4df07e9f511e\reference_lineup_20261105');out.mkdir(exist_ok=True);u='http://127.0.0.1:8188'
ref=Path(r'C:\Users\wsuk2\AppData\Local\Temp\codex-clipboard-5d188380-17a5-4cef-9cd6-f5b9a961796f.png');(out/'reference.png').write_bytes(ref.read_bytes());Image.new('RGB',(1152,768),(250,246,232)).save(out/'blank.png')
def upload(p):
 with p.open('rb') as f:r=requests.post(u+'/upload/image',files={'image':('pa_ref_'+p.name,f,'image/png')},data={'overwrite':'true'},timeout=60)
 r.raise_for_status();return r.json()['name']
g=json.loads(Path('ArtPipeline/ComfyUI/Examples/Shared_Lineup_20261105/api_B.json').read_text());g.pop('4');g['18']={'class_type':'LoadImage','inputs':{'image':upload(ref)}};g['20']={'class_type':'LoadImage','inputs':{'image':upload(out/'blank.png')}};g['21']={'class_type':'LoadBackgroundRemovalModel','inputs':{'bg_removal_name':'birefnet.safetensors'}}
base='20'
for i,(x,y,w,h) in enumerate([(231,147,91,130),(345,116,112,160),(78,189,66,87)]):
 n=30+i*6
 g[str(n)]={'class_type':'ImageCrop','inputs':{'image':['18',0],'width':w,'height':h,'x':x,'y':y}}
 g[str(n+1)]={'class_type':'ImageScale','inputs':{'image':[str(n),0],'upscale_method':'bicubic','width':260,'height':350,'crop':'disabled'}}
 g[str(n+2)]={'class_type':'RemoveBackground','inputs':{'bg_removal_model':['21',0],'image':[str(n+1),0]}}
 g[str(n+3)]={'class_type':'ImageCompositeMasked','inputs':{'destination':[base,0],'source':[str(n+1),0],'x':76+i*370,'y':210,'resize_source':False,'mask':[str(n+2),0]}};base=str(n+3)
g['60']={'class_type':'SaveImage','inputs':{'images':[base,0],'filename_prefix':'ProjectPA/ReferenceLineup_20261105/reference_input'}}
g['61']={'class_type':'VAEEncode','inputs':{'pixels':[base,0],'vae':['16',0]}};g['5']['inputs'].update(latent_image=['61',0],seed=115630,denoise=.82,steps=30);g['17']['inputs']['guidance']=3.5
g['2']['inputs']['text']='''A simple hand drawn character lineup of three original cute animal friends, standing upright on two tiny feet, FULL BODY, all facing slightly right in the same neutral pose. Left a pale GREY MACKEREL TABBY CAT with small triangular ears, three simple dark grey forehead stripes. Middle a CREAM PUPPY with small tan floppy ears, plain cream face. Right a pale GOLDEN HAMSTER with tiny round ears, plain golden face. The same body proportions for all three: LARGE WIDE ROUND HEAD with a distinct curved chin line, a SEPARATE SMALL PLUMP TORSO under the head, two very short little round arms outside the torso, two tiny feet. Head about 60 percent of total height, body about 30 percent, feet about 10 percent. Small dark dot eyes, a tiny happy mouth, tiny straight brows and tiny eye reflections. Original characters, species differentiated by ears and colors. Charming cozy hand-drawn ink cartoon with smooth continuous dark ink outlines of even thickness with clean rounded joins, sparse flat pastel color fills and only one tiny faint underside shadow. Simple innocent understated facial expressions. Smooth rounded hands. No fingers, no toe details, no tails, no whiskers, no props, no clothes, no blush. Neutral standing pose, NOT holding anything. Plain warm ivory background, generous empty space around three separate characters. No text. Simple friendly picture book doodle, not polished vector, not glossy, no gradients, not realistic.'''
g['7']['inputs']['filename_prefix']='ProjectPA/ReferenceLineup_20261105/redraw'
(out/'api.json').write_text(json.dumps(g,indent=2));r=requests.post(u+'/prompt',json={'prompt':g,'client_id':str(uuid.uuid4())},timeout=60);r.raise_for_status();pid=r.json()['prompt_id'];print(pid,flush=True)
for _ in range(600):
 h=requests.get(u+'/history/'+pid,timeout=30).json()
 if pid in h:
  item=h[pid];assert item['status']['status_str']=='success',item['status'];(out/'history.json').write_text(json.dumps(item,indent=2))
  for k in ['60','7']:
   d=item['outputs'][k]['images'][0];r=requests.get(u+'/view',params=d,timeout=60);r.raise_for_status();(out/('reference_input.png' if k=='60' else 'redraw.png')).write_bytes(r.content)
  print('SUCCESS',flush=True);break
 time.sleep(2)
else:raise TimeoutError(pid)
