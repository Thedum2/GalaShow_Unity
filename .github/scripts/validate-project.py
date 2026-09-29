"""Check the source inputs required by the separate licensed WebGL build."""
import json
from pathlib import Path
import re
import subprocess

root = Path(__file__).resolve().parents[2]
version = (root / "ProjectSettings/ProjectVersion.txt").read_text(encoding="utf-8")
assert re.search(r"^m_EditorVersion: 6000\.3\.\d+f\d+$", version, re.MULTILINE), "Expected Unity 6.3 LTS"
settings = (root / "ProjectSettings/ProjectSettings.asset").read_text(encoding="utf-8")
for field, expected in {"webGLCompressionFormat": "2", "webGLNameFilesAsHashes": "0"}.items():
    assert re.search(rf"^\s*{field}: {expected}$", settings, re.MULTILINE), f"React WebGL contract requires {field}: {expected}"

manifest = json.loads((root / "Packages/manifest.json").read_text(encoding="utf-8"))
lock = json.loads((root / "Packages/packages-lock.json").read_text(encoding="utf-8"))
for name, requested in manifest["dependencies"].items():
    assert name in lock["dependencies"], f"Package missing from lockfile: {name}"
    assert lock["dependencies"][name]["version"] == requested, f"Package version mismatch: {name}"

tracked = json.loads(subprocess.check_output(["git", "lfs", "ls-files", "--json"], cwd=root))
for entry in tracked["files"]:
    name = entry["name"]
    with (root / name).open("rb") as asset:
        assert not asset.read(100).startswith(b"version https://git-lfs.github.com/spec/v1"), f"Unresolved Git LFS pointer: {name}"
print("Unity 6.3 WebGL settings, direct package lock entries and LFS files verified.")
