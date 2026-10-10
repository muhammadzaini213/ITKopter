"""Manifest = latest snapshot of Drive contents + retrieve status, stored as JSON.

Schema for each entry (key = Drive file id):
{
  "id", "name", "mimeType", "modifiedTime", "relative_path", "webViewLink", "size",
  "status": "deleted_in_drive"   # only present once the file has disappeared from Drive
  "retrieve_status": "downloaded" | "skipped_name_conflict"   # set by retrieve.py, optional
  "downloaded_as", "downloaded_modified_time"                 # set by retrieve.py, optional
}

track.py MUST preserve all retrieve.* / downloaded_* fields when merging a fresh Drive
listing (otherwise retrieve.py loses its bookkeeping and re-notifies every run).
retrieve.py MUST preserve status: deleted_in_drive for ids missing from Drive.
This lets both scripts run independently without stepping on each other's data.
"""

import json
from pathlib import Path

# Fields owned by retrieve.py — track.py may refresh Drive metadata but must not drop these.
_RETRIEVE_KEYS = (
    "retrieve_status",
    "downloaded_as",
    "downloaded_modified_time",
)


def load_manifest(path):
    p = Path(path)
    if p.exists():
        return json.loads(p.read_text())
    return {}


def save_manifest(path, manifest):
    p = Path(path)
    p.parent.mkdir(parents=True, exist_ok=True)
    p.write_text(json.dumps(manifest, indent=2, ensure_ascii=False))


def diff_files(current: dict, previous: dict):
    """Compare the current Drive listing against the previous manifest.
    Returns (new_files, changed_files, deleted_files) - each a list of metadata dicts."""
    new_files, changed_files, deleted_files = [], [], []

    for file_id, meta in current.items():
        prev = previous.get(file_id)
        if prev is None:
            new_files.append(meta)
        elif prev.get("modifiedTime") != meta.get("modifiedTime"):
            changed_files.append(meta)

    for file_id, meta in previous.items():
        if file_id not in current and meta.get("status") != "deleted_in_drive":
            deleted_files.append(meta)

    return new_files, changed_files, deleted_files


def merge_current_into_manifest(current: dict, previous: dict, deleted_files: list) -> dict:
    """Used by track.py: merge the latest listing into the manifest, while PRESERVING
    retrieve bookkeeping fields, and flagging missing files as deleted_in_drive
    (instead of removing them from the manifest).

    Files already marked deleted_in_drive are only re-reported by diff_files when that
    flag is absent — so a persisted manifest must keep the flag across runs.
    """
    merged = dict(previous)  # start from the old manifest so retrieve fields carry over

    for file_id, meta in current.items():
        old = merged.get(file_id, {})
        new_entry = dict(meta)
        for key in _RETRIEVE_KEYS:
            if key in old:
                new_entry[key] = old[key]
        # File is present again → clear a stale deleted flag (new_entry has no "status").
        merged[file_id] = new_entry

    for f in deleted_files:
        fid = f["id"]
        if fid in merged:
            merged[fid]["status"] = "deleted_in_drive"

    return merged
