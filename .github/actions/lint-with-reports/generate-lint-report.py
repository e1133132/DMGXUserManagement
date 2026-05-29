#!/usr/bin/env python3
"""Merge dotnet format JSON reports and emit summary JSON + HTML."""

from __future__ import annotations

import html
import json
import sys
from datetime import datetime, timezone
from pathlib import Path


def load_issues(report_dir: Path) -> list[dict]:
    issues: list[dict] = []
    for report_file in sorted(report_dir.glob("*-report.json")):
        category = report_file.stem.replace("-report", "")
        raw = report_file.read_text(encoding="utf-8").strip()
        if not raw:
            continue
        entries = json.loads(raw)
        if not isinstance(entries, list):
            entries = [entries]
        for entry in entries:
            for file_report in entry.get("FileReports", []):
                file_path = file_report.get("FilePath", "")
                for change in file_report.get("Changes", []):
                    issues.append(
                        {
                            "category": category,
                            "file": file_path,
                            "line": change.get("LineNumber", 0),
                            "column": change.get("CharNumber", 0),
                            "rule": change.get("DiagnosticId", ""),
                            "severity": change.get("Severity", "warning"),
                            "message": change.get("FormatDescription", ""),
                        }
                    )
    return issues


def build_html(issues: list[dict]) -> str:
    rows = []
    for issue in issues:
        rows.append(
            "<tr>"
            f"<td>{html.escape(issue['category'])}</td>"
            f"<td>{html.escape(issue['severity'])}</td>"
            f"<td><code>{html.escape(issue['rule'])}</code></td>"
            f"<td>{html.escape(issue['file'])}</td>"
            f"<td>{issue['line']}</td>"
            f"<td>{issue['column']}</td>"
            f"<td>{html.escape(issue['message'])}</td>"
            "</tr>"
        )

    body_rows = "\n".join(rows) if rows else (
        '<tr><td colspan="7">No lint issues found.</td></tr>'
    )
    generated_at = datetime.now(timezone.utc).strftime("%Y-%m-%d %H:%M:%S UTC")

    return f"""<!DOCTYPE html>
<html lang="en">
<head>
  <meta charset="utf-8" />
  <title>Lint Report</title>
  <style>
    body {{ font-family: Segoe UI, sans-serif; margin: 2rem; color: #1a1a1a; }}
    table {{ border-collapse: collapse; width: 100%; }}
    th, td {{ border: 1px solid #ddd; padding: 0.5rem 0.75rem; text-align: left; vertical-align: top; }}
    th {{ background: #f5f5f5; }}
    code {{ font-size: 0.9em; }}
  </style>
</head>
<body>
  <h1>Lint Report</h1>
  <p>Generated at {generated_at}</p>
  <p>Total issues: {len(issues)}</p>
  <table>
    <thead>
      <tr>
        <th>Category</th>
        <th>Severity</th>
        <th>Rule</th>
        <th>File</th>
        <th>Line</th>
        <th>Column</th>
        <th>Message</th>
      </tr>
    </thead>
    <tbody>
      {body_rows}
    </tbody>
  </table>
</body>
</html>
"""


def build_summary_markdown(issues: list[dict], style_exit: int, analyzers_exit: int) -> str:
    passed = style_exit == 0 and analyzers_exit == 0 and len(issues) == 0
    status = "passed" if passed else "failed"
    lines = [
        f"## Lint Results ({status})",
        "",
        f"- Style check exit code: `{style_exit}`",
        f"- Analyzers check exit code: `{analyzers_exit}`",
        f"- Issues in reports: `{len(issues)}`",
        "",
    ]
    if issues:
        lines.append("| Category | Severity | Rule | File | Line | Message |")
        lines.append("| --- | --- | --- | --- | ---: | --- |")
        for issue in issues[:50]:
            lines.append(
                f"| {issue['category']} | {issue['severity']} | `{issue['rule']}` "
                f"| `{issue['file']}` | {issue['line']} | {issue['message']} |"
            )
        if len(issues) > 50:
            lines.append(f"\n_...and {len(issues) - 50} more issues. See HTML artifact._")
    else:
        lines.append("No lint issues reported.")
    return "\n".join(lines)


def main() -> int:
    if len(sys.argv) < 4:
        print("Usage: generate-lint-report.py <report-dir> <style-exit> <analyzers-exit>")
        return 2

    report_dir = Path(sys.argv[1])
    style_exit = int(sys.argv[2])
    analyzers_exit = int(sys.argv[3])

    issues = load_issues(report_dir)
    summary = {
        "generatedAtUtc": datetime.now(timezone.utc).isoformat(),
        "styleExitCode": style_exit,
        "analyzersExitCode": analyzers_exit,
        "issueCount": len(issues),
        "passed": style_exit == 0 and analyzers_exit == 0 and len(issues) == 0,
        "issues": issues,
    }

    (report_dir / "lint-report.json").write_text(
        json.dumps(summary, indent=2), encoding="utf-8"
    )
    (report_dir / "lint-report.html").write_text(build_html(issues), encoding="utf-8")
    (report_dir / "lint-summary.md").write_text(
        build_summary_markdown(issues, style_exit, analyzers_exit), encoding="utf-8"
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
