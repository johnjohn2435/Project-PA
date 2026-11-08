from pathlib import Path
from PIL import Image
import requests,json,time,uuid
p=Path(r'C:\Users\wsuk2\.codex\visualizations\2026\09\17\01a0aff8-5a1f-7303-8305-4df07e9f511e\poodle_ganaji_20261105');p.mkdir(exist_ok=True);u='http://127.0.0.1:8188';photo=Path(r'C:\Users\wsuk2\AppData\Local\Temp\codex-clipboard-66b1ece5-1cbf-43b8-b940-b3adb095deed.png');(p/'reference.png').write_bytes(photo.read_bytes());Image.new('RGB',(768,768),(249,246,232)).save(p/'blank.png')
def upload(f):
 with f.open('rb') as fp:r=requests.post(u+'/upload/image',files={'image':('pa_poodle_'+f.name,fp,'image/png')},data={'overwrite':'true'},timeout=60)
 r.raise_for_status();return r.json()['name']
g=json.loads(Path('ArtPipeline/ComfyUI/Examples/Shared_Lineup_20261105/api_B.json').read_text());g.pop('4');g['18']={'class_type':'LoadImage','inputs':{'image':upload(p/'reference.png')}};g['19']={'class_type':'ImageCrop','inputs':{'image':['18',0],'x':206,'y':30,'width':239,'height':250}};g['20']={'class_type':'ImageScale','inputs':{'image':['19',0],'upscale_method':'bicubic','width':540,'height':470,'crop':'disabled'}};g['21']={'class_type':'LoadBackgroundRemovalModel','inputs':{'bg_removal_name':'birefnet.safetensors'}};g['22']={'class_type':'RemoveBackground','inputs':{'bg_removal_model':['21',0],'image':['20',0]}};g['23']={'class_type':'LoadImage','inputs':{'image':upload(p/'blank.png')}};g['24']={'class_type':'ImageCompositeMasked','inputs':{'destination':['23',0],'source':['20',0],'x':114,'y':45,'resize_source':False,'mask':['22',0]}}
base=['24',0]
rig=Path('ArtPipeline/Rigging/SharedCharacters6_20261105/sprites/char_dog')
for i,name in enumerate(['leg_R','leg_L','body','arm_R','arm_L']):
 n=30+i*2;g[str(n)]={'class_type':'LoadImage','inputs':{'image':upload(rig/(name+'.png'))}};g[str(n+1)]={'class_type':'InvertMask','inputs':{'mask':[str(n),1]}};g[str(n+20)]={'class_type':'ImageCompositeMasked','inputs':{'destination':base,'source':[str(n),0],'x':0,'y':0,'resize_source':False,'mask':[str(n+1),0]}};base=[str(n+20),0]
g['80']={'class_type':'VAEEncode','inputs':{'pixels':base,'vae':['16',0]}};g['5']['inputs'].update(latent_image=['80',0],seed=115801,denoise=.86,steps=30);g['17']['inputs']['guidance']=3.5
g['2']['inputs']['text']='''One original cute APRICOT TOY POODLE cartoon game mascot, standing upright on TWO tiny feet, full body, isolated on a plain warm ivory background. A very LARGE ROUND HEAD clearly separate from a TINY CHUBBY TORSO, head about two thirds of total height, tiny oval arms resting by the sides and two very short rounded feet. Short drooping fluffy ears, a rounded fluffy crown, a small short muzzle with pale cream accents and a tiny black oval nose. Represent fluffy poodle hair with only three or four broad rounded scallops around the head and ears. Very simple sparse shapes, NO individual hairs, no curls drawn inside the fur. Two small black DOT eyes with tiny white reflections, two tiny short eyebrows, a tiny happy mouth. Warm pale apricot and muted cream color fills, one very subtle underside shadow. Thick uniform dark brown BLACK smooth continuous outlines with clean rounded joins. Cozy minimal children's picture book cartoon matching a family of simple animal friends from an illustrated jazz cafe. Simple plump body template, no neck. No tail, no fingers, no toe lines, no clothing, no accessories, no props, no text, no realistic fur, no detailed shading, no glossy 3D. Redraw the reference dog as a very simple flat INK CARTOON, not a photograph. Original friendly dog design.'''
g['7']['inputs']['filename_prefix']='ProjectPA/PoodleGanaji_20261105/raw';g['81']={'class_type':'SaveImage','inputs':{'images':base,'filename_prefix':'ProjectPA/PoodleGanaji_20261105/reference_input'}}
(p/'api.json').write_text(json.dumps(g,indent=2));r=requests.post(u+'/prompt',json={'prompt':g,'client_id':str(uuid.uuid4())},timeout=60);r.raise_for_status();pid=r.json()['prompt_id'];print(pid,flush=True)
for _ in range(600):
 h=requests.get(u+'/history/'+pid,timeout=30).json()
 if pid in h:
  item=h[pid];assert item['status']['status_str']=='success',item['status'];(p/'history.json').write_text(json.dumps(item,indent=2))
  for k,name in [('7','raw.png'),('81','reference_input.png')]:
   d=item['outputs'][k]['images'][0];r=requests.get(u+'/view',params=d,timeout=60);r.raise_for_status();(p/name).write_bytes(r.content)
  print('SUCCESS',flush=True);break
 time.sleep(2)
else:raise TimeoutError(pid)
