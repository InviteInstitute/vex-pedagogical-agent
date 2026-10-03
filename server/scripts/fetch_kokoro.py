"""Download the Kokoro-82M ONNX model + voices into server/models/ (idempotent).

    python scripts/fetch_kokoro.py
"""
import urllib.request
from pathlib import Path

RELEASE = "https://github.com/thewh1teagle/kokoro-onnx/releases/download/model-files-v1.0"
FILES = ["kokoro-v1.0.onnx", "voices-v1.0.bin"]
MODELS_DIR = Path(__file__).resolve().parents[1] / "models"


def main() -> None:
    MODELS_DIR.mkdir(exist_ok=True)
    for name in FILES:
        target = MODELS_DIR / name
        if target.exists():
            print(f"have {target}")
            continue
        partial = target.with_suffix(target.suffix + ".part")
        print(f"downloading {name} ...")
        urllib.request.urlretrieve(f"{RELEASE}/{name}", partial)
        partial.rename(target)  # only a complete download gets the real name
    print("done")


if __name__ == "__main__":
    main()
