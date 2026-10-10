#!/usr/bin/env python3
"""Regression: deleted files must notify once; track must not drop retrieve fields."""
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from core import state  # noqa: E402


def test_deleted_notifies_once_when_manifest_persists():
    keep = {
        "id": "keep",
        "name": "keep.png",
        "modifiedTime": "t1",
        "relative_path": "keep.png",
    }
    gone = {
        "id": "gone",
        "name": "gone.png",
        "modifiedTime": "t1",
        "relative_path": "gone.png",
    }
    previous = {
        "keep": dict(keep),
        "gone": dict(gone, retrieve_status="downloaded", downloaded_as="gone.png",
                     downloaded_modified_time="t1"),
    }

    current = {"keep": dict(keep, modifiedTime="t2")}  # gone missing
    new, changed, deleted = state.diff_files(current, previous)
    assert [f["id"] for f in deleted] == ["gone"], deleted

    merged = state.merge_current_into_manifest(current, previous, deleted)
    assert merged["gone"]["status"] == "deleted_in_drive"

    # Second run with persisted manifest → no re-notify
    new2, changed2, deleted2 = state.diff_files(current, merged)
    assert deleted2 == [], deleted2
    assert merged["gone"]["retrieve_status"] == "downloaded"
    assert merged["gone"]["downloaded_modified_time"] == "t1"
    assert merged["gone"]["downloaded_as"] == "gone.png"


def test_track_merge_preserves_retrieve_fields_on_live_file():
    previous = {
        "a": {
            "id": "a",
            "name": "a.png",
            "modifiedTime": "t1",
            "relative_path": "a.png",
            "retrieve_status": "downloaded",
            "downloaded_as": "sub/a.png",
            "downloaded_modified_time": "t1",
        }
    }
    current = {
        "a": {
            "id": "a",
            "name": "a.png",
            "modifiedTime": "t2",
            "relative_path": "a.png",
        }
    }
    merged = state.merge_current_into_manifest(current, previous, [])
    assert merged["a"]["modifiedTime"] == "t2"
    assert merged["a"]["retrieve_status"] == "downloaded"
    assert merged["a"]["downloaded_as"] == "sub/a.png"
    assert merged["a"]["downloaded_modified_time"] == "t1"
    assert "status" not in merged["a"]


def test_file_returning_after_delete_clears_flag():
    previous = {
        "back": {
            "id": "back",
            "name": "back.png",
            "modifiedTime": "t1",
            "relative_path": "back.png",
            "status": "deleted_in_drive",
        }
    }
    current = {
        "back": {
            "id": "back",
            "name": "back.png",
            "modifiedTime": "t3",
            "relative_path": "back.png",
        }
    }
    new, changed, deleted = state.diff_files(current, previous)
    assert [f["id"] for f in changed] == ["back"], (new, changed)
    assert new == []
    assert deleted == []
    merged = state.merge_current_into_manifest(current, previous, [])
    assert "status" not in merged["back"]


if __name__ == "__main__":
    test_deleted_notifies_once_when_manifest_persists()
    test_track_merge_preserves_retrieve_fields_on_live_file()
    test_file_returning_after_delete_clears_flag()
    print("state tests OK")
