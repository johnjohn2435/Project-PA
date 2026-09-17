import json, pathlib, re
import jsonschema, requests
ROOT=pathlib.Path(__file__).resolve().parents[1]
info=requests.get("http://127.0.0.1:8188/object_info",timeout=30).json()
source=requests.get("https://raw.githubusercontent.com/Comfy-Org/docs/main/specs/workflow_json_0.4.mdx",timeout=30)
source.raise_for_status()
schema=json.loads(re.findall(r"```json\s*(.*?)```",source.text,re.S)[0])
for path in sorted((ROOT/"Workflows").glob("*.json")):
 w=json.loads(path.read_text(encoding="utf-8"))
 jsonschema.validate(w,schema)
 nodes={n["id"]:n for n in w["nodes"]}
 assert len(nodes)==len(w["nodes"])
 assert all(n["type"] in info for n in nodes.values())
 for link,src,slot,dest,inp,typ in w["links"]:
  assert nodes[src]["outputs"][slot]["type"]==typ==nodes[dest]["inputs"][inp]["type"]
  assert nodes[dest]["inputs"][inp]["link"]==link
  assert link in nodes[src]["outputs"][slot]["links"]
 api=json.loads((ROOT/"Api"/path.name).read_text(encoding="utf-8"))
 for id,node in api.items():
  required=info[node["class_type"]]["input"].get("required",{})
  assert set(required)<=set(node["inputs"]),(id,required)
  for name,value in node["inputs"].items():
   if isinstance(value,list):
    source_id,slot=value
    assert source_id in api
    assert info[api[source_id]["class_type"]]["output"][slot]==required[name][0]
 print(path.name,": schema, node availability, ports, API required inputs passed")
