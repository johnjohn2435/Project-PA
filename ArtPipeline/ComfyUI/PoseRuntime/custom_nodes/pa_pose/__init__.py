import numpy as np
import torch
from PIL import Image
class PAPoseRotate:
    @classmethod
    def INPUT_TYPES(cls):
        return {"required":{"image":("IMAGE",),"mask":("MASK",),"angle":("FLOAT",{"default":0.0,"min":-180,"max":180}),"pivot_x":("INT",{"default":384}),"pivot_y":("INT",{"default":420})}}
    RETURN_TYPES=("IMAGE","MASK")
    FUNCTION="rotate"
    CATEGORY="Project PA/Pose"
    def rotate(self,image,mask,angle,pivot_x,pivot_y):
        out=[]; masks=[]
        for i in range(image.shape[0]):
            rgb=(image[i].cpu().numpy()*255).clip(0,255).astype(np.uint8)
            a=((1-mask[min(i,mask.shape[0]-1)].cpu().numpy())*255).clip(0,255).astype(np.uint8)
            im=Image.fromarray(np.dstack((rgb[:,:,:3],a)),"RGBA")
            im=im.rotate(angle,resample=Image.Resampling.BICUBIC,center=(pivot_x,pivot_y))
            arr=np.asarray(im).astype(np.float32)/255
            out.append(torch.from_numpy(arr[:,:,:3].copy()));masks.append(torch.from_numpy(1-arr[:,:,3].copy()))
        return (torch.stack(out),torch.stack(masks))
NODE_CLASS_MAPPINGS={"PAPoseRotate":PAPoseRotate}
NODE_DISPLAY_NAME_MAPPINGS={"PAPoseRotate":"PA Preserve Art Pose Rotation"}
