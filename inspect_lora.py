"""Inspect LoRA safetensors metadata for trigger word"""
from safetensors.torch import load_file
import json

path = "C:\\Users\\kruil\\Documents\\StabilityMatrix-win-x64\\Data\\Models\\Lora\\lora.safetensors"

# Load with metadata
result = load_file(path, device="cpu")

print("=" * 60)
print("LORA FILE INSPECTION")
print("=" * 60)
print(f"\nFile: {path}")
print(f"Total tensors: {len(result)}")

# Get metadata
from safetensors import safe_open
with safe_open(path, framework="pt", device="cpu") as f:
    metadata = f.metadata()
    
print("\n" + "=" * 60)
print("METADATA:")
print("=" * 60)
if metadata:
    for key, value in sorted(metadata.items()):
        print(f"  {key}: {value}")
else:
    print("  (No metadata found)")

# Look for common training-related keys
print("\n" + "=" * 60)
print("LOOKING FOR TRIGGER WORD / TRAINING INFO:")
print("=" * 60)
trigger_keys = ['ss_trigger_words', 'ss_trigger_word', 'trigger_word', 'ss_training_comment', 
                'ss_dataset_dirs', 'ss_instance_prompt', 'ss_tag', 'ss_name', 'modelspec.title',
                'ss_base_model_version', 'ss_sd_model_name', 'ss_network_dim', 'ss_network_alpha']

found_any = False
if metadata:
    for key in trigger_keys:
        if key in metadata:
            print(f"  {key}: {metadata[key]}")
            found_any = True

if not found_any:
    print("  (No standard trigger word metadata found)")
    print("\n  All available metadata keys:")
    if metadata:
        for key in sorted(metadata.keys()):
            print(f"    - {key}")

# Check tensor keys for clues
print("\n" + "=" * 60)
print("SAMPLE TENSOR KEYS:")
print("=" * 60)
for i, key in enumerate(list(result.keys())[:10]):
    print(f"  {key}: {result[key].shape}")

# Look for alpha values
print("\n" + "=" * 60)
print("NETWORK INFO:")
print("=" * 60)
alpha_keys = [k for k in result.keys() if 'alpha' in k.lower()]
if alpha_keys:
    sample_alpha = result[alpha_keys[0]].item() if result[alpha_keys[0]].numel() == 1 else "multi-dim"
    print(f"  Alpha keys found: {len(alpha_keys)}")
    print(f"  Sample alpha: {sample_alpha}")

# Count up/down pairs to estimate rank
up_keys = [k for k in result.keys() if 'lora_up' in k or 'lora.up' in k]
down_keys = [k for k in result.keys() if 'lora_down' in k or 'lora.down' in k]
print(f"  LoRA up weights: {len(up_keys)}")
print(f"  LoRA down weights: {len(down_keys)}")

if up_keys:
    sample_shape = result[up_keys[0]].shape
    print(f"  Sample lora_up shape: {sample_shape}")
    if len(sample_shape) >= 2:
        print(f"  Estimated rank: {sample_shape[1] if sample_shape[0] > sample_shape[1] else sample_shape[0]}")

print("\n" + "=" * 60)
