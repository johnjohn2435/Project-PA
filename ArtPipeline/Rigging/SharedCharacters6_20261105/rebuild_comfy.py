from pathlib import Path
import json,time,uuid,requests
root=Path(__file__).resolve().parent
url="http://127.0.0.1:8188"
output=root/"replay";output.mkdir(exist_ok=True)
for name,mapname in [("parts","input_map"),("common_body","common_body_input_map")]:
    graph=json.loads((root/"comfy"/(name+"_api.json")).read_text())
    mapping=json.loads((root/"comfy"/(mapname+".json")).read_text())
    uploaded={}
    for node in graph.values():
        if node["class_type"]!="LoadImage":continue
        ref=node["inputs"]["image"]
        if ref not in uploaded:
            relative=Path(mapping[ref]);path=root/relative
            if relative.parts[0]=="sprites" and (output/relative).is_file():path=output/relative
            with path.open("rb") as f:
                response=requests.post(url+"/upload/image",files={"image":("pa_replay_"+uuid.uuid4().hex+".png",f,"image/png")},timeout=60)
            response.raise_for_status();uploaded[ref]=response.json()["name"]
        node["inputs"]["image"]=uploaded[ref]
    response=requests.post(url+"/prompt",json={"prompt":graph,"client_id":str(uuid.uuid4())},timeout=60)
    response.raise_for_status();pid=response.json()["prompt_id"]
    for _ in range(600):
        history=requests.get(url+"/history/"+pid,timeout=30).json()
        if pid in history:break
        time.sleep(1)
    else:raise TimeoutError(pid)
    item=history[pid]
    if item["status"]["status_str"]!="success":raise RuntimeError(item["status"])
    for nid,node in item["outputs"].items():
        if graph[nid]["class_type"]!="SaveImage":continue
        prefix=graph[nid]["inputs"]["filename_prefix"].split("/")
        folder=output/"sprites"/prefix[-2] if prefix[-2].startswith("char_") else output
        folder.mkdir(parents=True,exist_ok=True)
        for descriptor in node.get("images",[]):
            response=requests.get(url+"/view",params=descriptor,timeout=60);response.raise_for_status()
            (folder/(prefix[-1]+".png")).write_bytes(response.content)
    (output/(name+"_history.json")).write_text(json.dumps(item,indent=2))
print(output)
