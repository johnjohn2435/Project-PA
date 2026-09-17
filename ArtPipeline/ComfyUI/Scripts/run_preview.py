"""Execute a bundled workflow against a local ComfyUI server for validation."""
import argparse, json, pathlib, time, uuid
import requests
ROOT=pathlib.Path(__file__).resolve().parents[1]
parser=argparse.ArgumentParser()
parser.add_argument("workflow",choices=["01_character","02_refine","03_cutout","04_background"])
parser.add_argument("--url",default="http://127.0.0.1:8188")
parser.add_argument("--character",default="char_momo",choices=["char_momo","char_bori","char_nabi"])
parser.add_argument("--seed",type=int,default=243517)
parser.add_argument("--timeout",type=int,default=900)
args=parser.parse_args()
if not args.url.startswith(("http://127.0.0.1:","http://localhost:")):raise ValueError("Use a localhost ComfyUI server.")
graph=json.loads((ROOT/"Api"/(args.workflow+".json")).read_text(encoding="utf-8"))
ui=json.loads((ROOT/"Workflows"/(args.workflow+".json")).read_text(encoding="utf-8"))
presets=json.loads((ROOT/"prompt_presets.json").read_text(encoding="utf-8"))
for id,node in graph.items():
 uinode=next(n for n in ui["nodes"] if str(n["id"])==id)
 if node["class_type"]=="KSampler":
  node["inputs"]["seed"]=args.seed;uinode["widgets_values"][0]=args.seed
 if node["class_type"]=="CLIPTextEncode" and "Positive" in node["_meta"]["title"] and args.workflow!="04_background":
  text=presets["characters"][args.character]+", "+presets["style"]
  node["inputs"]["text"]=text;uinode["widgets_values"][0]=text
 if node["class_type"]=="SaveImage":
  prefix=node["inputs"]["filename_prefix"]
  prefix=prefix.replace("/"+args.workflow+"/","/"+args.workflow+"/"+args.character+"/")
  node["inputs"]["filename_prefix"]=prefix;uinode["widgets_values"][0]=prefix
start=time.monotonic()
response=requests.post(args.url+"/prompt",json={"prompt":graph,"client_id":str(uuid.uuid4()),"extra_data":{"extra_pnginfo":{"workflow":ui}}},timeout=60)
if not response.ok: raise RuntimeError(response.text)
info=response.json()
if info.get("node_errors"):raise RuntimeError(info["node_errors"])
prompt_id=info["prompt_id"];print("Queued",prompt_id,flush=True)
deadline=time.monotonic()+args.timeout
while time.monotonic()<deadline:
 history=requests.get(args.url+"/history/"+prompt_id,timeout=30).json()
 if prompt_id in history:
  item=history[prompt_id]
  if item["status"]["status_str"]!="success":raise RuntimeError(json.dumps(item["status"]))
  results=[]
  for node_id,out in item["outputs"].items():
   for image in out.get("images",[]):
    r=requests.get(args.url+"/view",params=image,timeout=60);r.raise_for_status()
    role=graph[node_id]["inputs"]["filename_prefix"].split("/")[-1]
    file=ROOT/"Examples"/f"{args.workflow}_{args.character}_{role}.png"
    file.write_bytes(r.content);results.append(str(file))
  report={"workflow":args.workflow,"character":args.character,"seed":args.seed,"elapsed_seconds":round(time.monotonic()-start,2),"prompt_id":prompt_id,"outputs":results,"status":"success"}
  (ROOT/"Logs"/f"{args.workflow}_{args.character}_validation.json").write_text(json.dumps(report,indent=2)+"\n",encoding="utf-8")
  print(json.dumps(report,indent=2),flush=True)
  break
 time.sleep(2)
else:raise TimeoutError("Prompt still pending; check ComfyUI queue before resubmitting.")
