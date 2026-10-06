"""Update only the RotationSolver entry, using metadata from the released package."""
import argparse
import json
import re
from pathlib import Path
from time import time


def update(entries, manifest, version, repository, now=None):
    if not re.fullmatch(r"\d+\.\d+\.\d+\.\d+", version):
        raise ValueError("Expected a four-part version")
    if not re.fullmatch(r"[A-Za-z0-9_.-]+/[A-Za-z0-9_.-]+", repository):
        raise ValueError("Expected owner/repository")
    if not isinstance(entries, list) or any(not isinstance(e, dict) for e in entries):
        raise ValueError("pluginmaster.json must be an array of objects")
    if manifest.get("InternalName") != "RotationSolver" or manifest.get("AssemblyVersion") != version:
        raise ValueError("Released manifest identity/version mismatch")
    matches = [i for i, entry in enumerate(entries) if entry.get("InternalName") == "RotationSolver"]
    if len(matches) > 1:
        raise ValueError("Duplicate RotationSolver entries")
    result = [dict(entry) for entry in entries]
    entry = dict(result[matches[0]]) if matches else {}
    for key in ("Name", "Author", "Description", "Punchline", "InternalName",
                "ApplicableVersion", "DalamudApiLevel", "Tags", "CategoryTags",
                "LoadRequiredState", "LoadSync", "CanUnloadAsync", "LoadPriority",
                "IsTestingExclusive", "IconUrl", "AcceptsFeedback"):
        if key in manifest:
            entry[key] = manifest[key]
    url = f"https://github.com/{repository}/releases/download/v{version}/latest.zip"
    entry.update(AssemblyVersion=version, RepoUrl=f"https://github.com/{repository}",
                 DownloadLinkInstall=url, DownloadLinkUpdate=url)
    # Do not downgrade users who installed a newer release from another run.
    old = result[matches[0]] if matches else {}
    old_version = old.get("AssemblyVersion", "0.0.0.0")
    if tuple(map(int, old_version.split("."))) > tuple(map(int, version.split("."))):
        raise ValueError("Refusing to downgrade plugin repository")
    # Keep a deliberately separate testing channel; initialize only for new entries.
    if not matches:
        entry.update(TestingAssemblyVersion=version, TestingDalamudApiLevel=manifest["DalamudApiLevel"],
                     DownloadLinkTesting=url)
    if entry != old:
        entry["LastUpdate"] = int(time()) if now is None else now
    if matches:
        result[matches[0]] = entry
    else:
        result.append(entry)
    return result


def main():
    parser = argparse.ArgumentParser()
    for name in ("index", "manifest", "version", "repository"):
        parser.add_argument(name)
    args = parser.parse_args()
    path = Path(args.index)
    entries = json.loads(path.read_text(encoding="utf-8-sig"))
    manifest = json.loads(Path(args.manifest).read_text(encoding="utf-8-sig"))
    result = update(entries, manifest, args.version, args.repository)
    if result != entries:
        path.write_text(json.dumps(result, ensure_ascii=False, indent=4) + "\n", encoding="utf-8")


if __name__ == "__main__":
    main()
