"""Inspect DeepSpeed checkpoint structure"""
import torch

ckpt = torch.load("C:\\Users\\kruil\\Documents\\StabilityMatrix-win-x64\\Data\\Models\\Lora\\mp_rank_00_model_states.pt", map_location="cpu")

print("Top-level keys:")
for k in ckpt.keys():
    v = ckpt[k]
    if v is None:
        print(f"  {k}: None")
    elif isinstance(v, dict):
        print(f"  {k}: dict with {len(v)} keys")
    elif isinstance(v, list):
        print(f"  {k}: list with {len(v)} items")
    else:
        print(f"  {k}: {type(v)}")

# Check param_shapes for LoRA clues
if 'param_shapes' in ckpt and ckpt['param_shapes']:
    print("\nSample param_shapes:")
    for shape in ckpt['param_shapes'][:20]:
        print(f"  {shape}")

# Check if there's a separate file
print("\nLooking for zero checkpoint info...")
if 'checkpoint_path' in ckpt:
    print(f"Checkpoint path: {ckpt['checkpoint_path']}")

print("\nChecking optimizer...")
if 'optimizer' in ckpt and ckpt['optimizer']:
    opt = ckpt['optimizer']
    if isinstance(opt, dict):
        print(f"Optimizer keys: {list(opt.keys())[:10]}")
    else:
        print(f"Optimizer type: {type(opt)}")
