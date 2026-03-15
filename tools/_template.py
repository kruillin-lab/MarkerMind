#!/usr/bin/env python3
"""
Template for B.L.A.S.T. automation tools.

All tools should be:
1. Atomic - do ONE thing well
2. Deterministic - same input = same output
3. Testable - can run independently
4. Documented - clear docstrings and comments

Usage:
    python tools/template.py --input data.json --output result.json
"""

import argparse
import json
import sys
from pathlib import Path
from dotenv import load_dotenv

# Load environment variables
load_dotenv()

# Constants
TMP_DIR = Path(".tmp")
TMP_DIR.mkdir(exist_ok=True)


def process_data(input_data: dict) -> dict:
    """
    Main processing logic.

    Args:
        input_data: The input payload

    Returns:
        Processed output payload
    """
    # TODO: Implement your logic here
    output_data = {
        "status": "success",
        "input_received": input_data,
        "result": "TODO: Add your processing logic",
    }
    return output_data


def main():
    parser = argparse.ArgumentParser(description="Tool description goes here")
    parser.add_argument("--input", "-i", type=str, help="Path to input JSON file")
    parser.add_argument(
        "--output",
        "-o",
        type=str,
        default=str(TMP_DIR / "output.json"),
        help="Path to output JSON file (default: .tmp/output.json)",
    )
    parser.add_argument(
        "--dry-run",
        action="store_true",
        help="Show what would be done without doing it",
    )

    args = parser.parse_args()

    # Load input
    if args.input:
        with open(args.input, "r") as f:
            input_data = json.load(f)
    else:
        # Default test data
        input_data = {"test": True}

    # Process
    if args.dry_run:
        print(f"DRY RUN: Would process {len(input_data)} fields")
        print(f"DRY RUN: Would output to {args.output}")
        return

    result = process_data(input_data)

    # Save output
    output_path = Path(args.output)
    output_path.parent.mkdir(parents=True, exist_ok=True)

    with open(output_path, "w") as f:
        json.dump(result, f, indent=2)

    print(f"✓ Output saved to: {output_path}")
    print(f"✓ Result: {result.get('status', 'unknown')}")


if __name__ == "__main__":
    try:
        main()
    except Exception as e:
        print(f"✗ Error: {e}", file=sys.stderr)
        sys.exit(1)
