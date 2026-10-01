"""Prepare a published Blazor app for a GitHub Pages repository subpath."""
import json
import os
from datetime import datetime, timezone
from pathlib import Path
import sys

root = Path(sys.argv[1])
repository = os.environ.get("REPOSITORY_NAME", "PlanetSimulator")
index = root / "index.html"
html = index.read_text(encoding="utf-8-sig")
if '<base href="/" />' not in html:
    raise SystemExit("Expected root base href was not found")
index.write_text(html.replace('<base href="/" />', f'<base href="/{repository}/" />'), encoding="utf-8")
(root / ".nojekyll").touch()
(root / "build.json").write_text(json.dumps({
    "commit": os.environ.get("BUILD_SHA", "local"),
    "builtAt": datetime.now(timezone.utc).isoformat(),
}), encoding="utf-8")
