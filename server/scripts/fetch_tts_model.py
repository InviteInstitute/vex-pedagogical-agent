"""Download the Paradee-8M v1.2 TTS model + config into server/models/ (idempotent).

python scripts/fetch_tts_model.py
"""

import urllib.request
from pathlib import Path

RELEASE = "https://huggingface.co/sahilmahendrakar/Paradee-8M-v1.2/resolve/v1.2"
FILES = {"paradee_int8.onnx": "onnx/paradee_int8.onnx", "paradee_config.json": "config.json"}
MODELS_DIR = Path(__file__).resolve().parents[1] / "models"


def main() -> None:
    MODELS_DIR.mkdir(exist_ok=True)
    for name, source in FILES.items():
        target = MODELS_DIR / name
        if target.exists():
            print(f"have {target}")
            continue
        partial = target.with_suffix(target.suffix + ".part")
        print(f"downloading {name} ...")
        urllib.request.urlretrieve(f"{RELEASE}/{source}", partial)
        partial.rename(target)  # only a complete download gets the real name
    print("done")


if __name__ == "__main__":
    main()
