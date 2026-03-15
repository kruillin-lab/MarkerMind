"""
Convert DeepSpeed checkpoint (mp_rank_00_model_states.pt) to SDXL LoRA safetensors
Compatible with SD.Next / Automatic1111 / ComfyUI
"""

import torch
import argparse
import json
from collections import OrderedDict
from safetensors.torch import save_file
import os


def convert_deepspeed_to_lora(
    checkpoint_path: str,
    output_path: str,
    save_dtype: str = "fp16"
):
    """
    Convert DeepSpeed checkpoint to Kohya-style LoRA safetensors
    """
    print(f"Loading checkpoint from: {checkpoint_path}")
    
    # Load the checkpoint
    checkpoint = torch.load(checkpoint_path, map_location="cpu")
    
    print(f"Checkpoint type: {type(checkpoint)}")
    
    # Handle different checkpoint structures
    state_dict = None
    
    if isinstance(checkpoint, dict):
        print(f"Top-level keys: {list(checkpoint.keys())[:10]}")
        
        # Try to find the model state dict
        if "module" in checkpoint:
            state_dict = checkpoint["module"]
            print("Using 'module' key")
        elif "model" in checkpoint:
            state_dict = checkpoint["model"]
            print("Using 'model' key")
        elif "state_dict" in checkpoint:
            state_dict = checkpoint["state_dict"]
            print("Using 'state_dict' key")
        elif "lora_weights" in checkpoint:
            state_dict = checkpoint["lora_weights"]
            print("Using 'lora_weights' key")
        else:
            # Assume the dict itself is the state dict
            state_dict = checkpoint
            print("Using checkpoint as state_dict directly")
    else:
        print(f"WARNING: Checkpoint is not a dict, it's {type(checkpoint)}")
        return
    
    if state_dict is None:
        print("ERROR: Could not find state_dict in checkpoint")
        return
    
    print(f"Total keys in state_dict: {len(state_dict)}")
    
    # Print some sample keys
    print("\nSample keys in checkpoint:")
    for key in list(state_dict.keys())[:20]:
        print(f"  - {key}")
    
    # Filter only LoRA weights
    lora_weights = OrderedDict()
    
    for key, value in state_dict.items():
        # Look for LoRA keys (lora_up, lora_down, alpha)
        if any(x in key.lower() for x in ['lora_up', 'lora_down', 'lora_alpha', '.alpha']):
            # Convert to the correct dtype
            if save_dtype == "fp16":
                lora_weights[key] = value.detach().to(torch.float16)
            elif save_dtype == "bf16":
                lora_weights[key] = value.detach().to(torch.bfloat16)
            else:
                lora_weights[key] = value.detach().to(torch.float32)
    
    print(f"\nFound {len(lora_weights)} LoRA weight tensors")
    
    if len(lora_weights) == 0:
        print("\nWARNING: No LoRA weights found with standard key patterns!")
        print("Available keys (first 30):")
        for key in list(state_dict.keys())[:30]:
            print(f"  - {key}")
        return
    
    # Convert keys to Kohya/A1111 format if needed
    converted_weights = OrderedDict()
    
    for key, value in lora_weights.items():
        new_key = key
        
        # Handle different naming conventions
        # SDXL LoRA keys should look like:
        # lora_unet_down_blocks_0_attentions_0_transformer_blocks_0_attn1_to_q.lora_down.weight
        
        # Remove 'module.' prefix if present
        if new_key.startswith("module."):
            new_key = new_key[7:]
        
        # Convert diffusers format to Kohya format if needed
        if "diffusion_model" in new_key:
            new_key = new_key.replace("diffusion_model", "lora_unet")
        
        if "text_encoder" in new_key or "text_model" in new_key:
            # Handle text encoder keys
            if "text_encoder_2" in new_key or "text_model_2" in new_key:
                new_key = new_key.replace("text_encoder_2", "lora_te2").replace("text_model_2", "lora_te2")
            else:
                new_key = new_key.replace("text_encoder", "lora_te1").replace("text_model", "lora_te1")
        
        converted_weights[new_key] = value
    
    # Prepare metadata
    metadata = OrderedDict()
    metadata["format"] = "pt"
    metadata["modelspec.architecture"] = "stable-diffusion-xl-v1-base/lora"
    metadata["modelspec.sai_model_spec"] = "1.0.0"
    metadata["modelspec.prediction_type"] = "epsilon"
    metadata["ss_network_module"] = "networks.lora"
    metadata["ss_network_dim"] = "64"  # Adjust if you know your actual rank
    metadata["ss_network_alpha"] = "32"  # Adjust if you know your actual alpha
    
    # Save as safetensors
    output_dir = os.path.dirname(output_path)
    if output_dir:
        os.makedirs(output_dir, exist_ok=True)
    save_file(converted_weights, output_path, metadata=metadata)
    
    print(f"\n✅ Saved LoRA to: {output_path}")
    print(f"   Format: {save_dtype}")
    print(f"   Total tensors: {len(converted_weights)}")
    
    # Print sample keys
    print("\nSample keys in output:")
    for key in list(converted_weights.keys())[:5]:
        print(f"  - {key}: {converted_weights[key].shape}")


def main():
    parser = argparse.ArgumentParser(description="Convert DeepSpeed checkpoint to SDXL LoRA")
    parser.add_argument("--input", "-i", required=True, help="Path to mp_rank_00_model_states.pt")
    parser.add_argument("--output", "-o", required=True, help="Output path for .safetensors file")
    parser.add_argument("--dtype", choices=["fp16", "bf16", "fp32"], default="fp16", 
                        help="Output dtype (default: fp16)")
    
    args = parser.parse_args()
    
    convert_deepspeed_to_lora(args.input, args.output, args.dtype)


if __name__ == "__main__":
    main()
