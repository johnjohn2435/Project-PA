import argparse, concurrent.futures, hashlib, json, os, pathlib, time, requests
ROOT=pathlib.Path(__file__).resolve().parents[1]
parser=argparse.ArgumentParser()
parser.add_argument("--models-dir",type=pathlib.Path,default=pathlib.Path(os.environ["USERPROFILE"])/"ComfyUI-Shared/models")
args=parser.parse_args()
MODELS=args.models_dir.resolve()
SPECS=[
 dict(repo="Lykon/dreamshaper-xl-v2-turbo",revision="f70ad5bce436c640d23fc02cd0a6ccc787280b6e",remote="DreamShaperXL_Turbo_v2_1.safetensors",local="checkpoints/DreamShaperXL_Turbo_v2_1.safetensors",size=6939220250,sha256="4496b36d48bfd7cfe4e5dbce3485db567bcefa2bef7238d290dbd45612125083",license="OpenRAIL++; see upstream model card"),
 dict(repo="Comfy-Org/BiRefNet",revision="5a1bd8ae750548f8cd42e3c8afa854fd3eba0fb1",remote="background_removal/birefnet.safetensors",local="background_removal/birefnet.safetensors",size=444473596,sha256="9ab37426bf4de0567af6b5d21b16151357149139362e6e8992021b8ce356a154",license="See upstream BiRefNet model card")
]
def digest(path):
 h=hashlib.sha256()
 with path.open("rb") as f:
  while block:=f.read(8*1024*1024):h.update(block)
 return h.hexdigest()
for spec in SPECS:
 dest=MODELS/spec["local"]; dest.parent.mkdir(parents=True,exist_ok=True)
 if dest.exists():
  if dest.stat().st_size==spec["size"] and digest(dest)==spec["sha256"]:
   print("Verified existing",dest.name,flush=True);continue
  raise RuntimeError(f"Existing file differs; refusing overwrite: {dest}")
 partial=dest.with_suffix(dest.suffix+".partial")
 prefix=partial.stat().st_size if partial.exists() else 0
 assert prefix<=spec["size"]
 parts=dest.with_suffix(dest.suffix+".parts");parts.mkdir(exist_ok=True)
 url=f"https://huggingface.co/{spec['repo']}/resolve/{spec['revision']}/{spec['remote']}"
 ranges=[(start,min(start+64*1024*1024,spec["size"])-1) for start in range(prefix,spec["size"],64*1024*1024)]
 def fetch(pair):
  start,end=pair;file=parts/f"{start}-{end}.part";length=end-start+1
  if file.exists() and file.stat().st_size==length:return file
  for attempt in range(4):
   try:
    with requests.get(url,headers={"Range":f"bytes={start}-{end}"},stream=True,timeout=(30,90)) as r:
     r.raise_for_status()
     assert r.status_code==206 and r.headers.get("Content-Range")==f"bytes {start}-{end}/{spec['size']}",r.headers.get("Content-Range")
     with file.open("wb") as out:
      for chunk in r.iter_content(1024*1024):out.write(chunk)
    assert file.stat().st_size==length
    return file
   except Exception:
    if attempt==3:raise
    time.sleep(2)
 print(f"Resuming {dest.name} at {prefix/spec['size']:.0%}",flush=True)
 with concurrent.futures.ThreadPoolExecutor(max_workers=6) as pool:
  futures=[pool.submit(fetch,pair) for pair in ranges]
  done_bytes=prefix
  for future in concurrent.futures.as_completed(futures):
   done_bytes+=future.result().stat().st_size
   print(f"{dest.name}: {done_bytes/spec['size']:.0%}",flush=True)
 with partial.open("ab") as out:
  for start,end in ranges:
   with (parts/f"{start}-{end}.part").open("rb") as source:
    while chunk:=source.read(8*1024*1024):out.write(chunk)
 if partial.stat().st_size!=spec["size"] or digest(partial)!=spec["sha256"]:raise RuntimeError("SHA-256 verification failed")
 partial.rename(dest)
 for file in parts.iterdir():
  if file.resolve().is_relative_to(MODELS) and file.suffix==".part":file.unlink()
 parts.rmdir()
 print("VERIFIED",dest.name,flush=True)
(ROOT/"models.lock.json").write_text(json.dumps(SPECS,indent=2)+"\n",encoding="utf-8")
