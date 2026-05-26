from __future__ import annotations

import html
import re
from pathlib import Path

from docx import Document
from docx.enum.text import WD_ALIGN_PARAGRAPH
from docx.oxml import OxmlElement
from docx.oxml.ns import qn
from docx.shared import Inches, Pt, RGBColor

ROOT = Path(__file__).resolve().parents[1]
DOCS = ROOT / "docs"
SOURCE = DOCS / "Step3_Integration_Update.md"
HTML_OUT = DOCS / "Step3_Integration_Update.html"
DOCX_OUT = DOCS / "Step3_Integration_Update.docx"
PIPELINE_IMAGE = ROOT / "AutoRepair" / "pipeline_status.png"


def inline_html(text: str) -> str:
    text = html.escape(text)
    text = re.sub(r"`([^`]+)`", r"<code>\1</code>", text)
    text = re.sub(r"\*\*([^*]+)\*\*", r"<strong>\1</strong>", text)
    return text


def parse_table(lines: list[str], start: int) -> tuple[list[list[str]], int]:
    rows: list[list[str]] = []
    index = start
    while index < len(lines) and lines[index].strip().startswith("|"):
        row = [cell.strip() for cell in lines[index].strip().strip("|").split("|")]
        if not all(re.fullmatch(r":?-{3,}:?", cell) for cell in row):
            rows.append(row)
        index += 1
    return rows, index


def html_attr(style: str) -> str:
    return f' style="{style}"'


def render_hero(title: str, metadata: list[str]) -> str:
    meta = ""
    if metadata:
        meta = "".join(
                        f"<span{html_attr('display:inline-block; margin-right:18px; color:#48677f; font-size:13px; overflow-wrap:anywhere;')}>{inline_html(item)}</span>"
            for item in metadata
        )

    return f"""
<section{html_attr('background:#eaf4ff; color:#17324d; padding:30px 34px; border-radius:10px; margin:0 0 18px; border:1px solid #c8ddf0; border-left:8px solid #7aa9d6;')}>
    <p{html_attr('margin:0 0 8px; text-transform:uppercase; letter-spacing:0; font:700 12px Aptos, Calibri, sans-serif; color:#4f7fae;')}>AutoRepair research update</p>
    <h1{html_attr('font:700 34px Aptos, Calibri, sans-serif; line-height:1.1; margin:0 0 12px; color:#17324d;')}>{inline_html(title)}</h1>
    <p{html_attr('margin:0 0 18px; max-width:860px; font:400 15px Aptos, Calibri, sans-serif; color:#36556f;')}>Corrected Step 3 benchmark after the false-positive audit: 13 repositories attempted, 10 traceable repositories, 296 final violations, and 6 generated candidate patches that compile individually.</p>
    <div{html_attr('border-top:1px solid #c8ddf0; padding-top:12px;')}>{meta}</div>
</section>
"""


def render_pipeline_html() -> str:
    cells = [
        ("Step 1", "Trace", "Instrument C# and record runtime events"),
        ("Step 2", "Constraints", "Convert events into violation records"),
        ("Step 3", "Repair", "Classify and synthesize patch candidates"),
        ("Verify", "Build", "Apply each candidate and compile-check it"),
    ]
    cell_html = []
    for label, title, detail in cells:
        cell_html.append(
            f"<td{html_attr('width:25%; vertical-align:top; border:1px solid #cbddeb; border-radius:8px; background:#f7fbff; padding:12px 14px;')}>"
            f"<div{html_attr('font:700 11px Aptos, Calibri, sans-serif; color:#5b87b2; text-transform:uppercase; letter-spacing:0; margin-bottom:4px;')}>{label}</div>"
            f"<div{html_attr('font:700 16px Aptos, Calibri, sans-serif; color:#17324d; margin-bottom:4px;')}>{title}</div>"
            f"<div{html_attr('font:400 12.5px Aptos, Calibri, sans-serif; color:#5e7182; line-height:1.35;')}>{detail}</div>"
            "</td>"
        )

    return (
        f"<table{html_attr('width:100%; border-collapse:separate; border-spacing:8px; margin:0 0 26px;')}>"
        f"<tr>{''.join(cell_html)}</tr></table>"
    )


def render_metric_cards(rows: list[list[str]]) -> str:
    cards = rows[1:]
    html_rows: list[str] = []
    for start in range(0, len(cards), 3):
        cells = []
        for metric, value in cards[start:start + 3]:
            cells.append(
                f"<td{html_attr('width:33.33%; vertical-align:top; border:1px solid #cbddeb; background:#ffffff; border-top:4px solid #7aa9d6; padding:13px 14px;')}>"
                f"<div{html_attr('font:700 24px Aptos, Calibri, sans-serif; color:#17324d; margin-bottom:3px;')}>{inline_html(value)}</div>"
                f"<div{html_attr('font:600 12.5px Aptos, Calibri, sans-serif; color:#52687b;')}>{inline_html(metric)}</div>"
                "</td>"
            )
        while len(cells) < 3:
            cells.append("<td></td>")
        html_rows.append("<tr>" + "".join(cells) + "</tr>")

    return (
        f"<table{html_attr('width:100%; border-collapse:separate; border-spacing:10px; margin:16px 0 24px;')}>"
        + "".join(html_rows)
        + "</table>"
    )


def render_html_table(rows: list[list[str]]) -> str:
    if not rows:
        return ""

    if len(rows[0]) == 2 and rows[0][0] == "Metric" and rows[0][1] == "Final result":
        return render_metric_cards(rows)

    table_style = "width:100%; border-collapse:collapse; margin:14px 0 24px; font:400 13px Aptos, Calibri, sans-serif; color:#21354a;"
    th_style = "background:#e8f3ff; color:#17324d; border:1px solid #c7dceb; padding:8px 10px; text-align:left; font-weight:700;"
    td_style = "border:1px solid #d5e4f1; padding:8px 10px; vertical-align:top;"

    header = rows[0]
    body = rows[1:]
    parts = [f"<table{html_attr(table_style)}>"]
    parts.append("<thead><tr>" + "".join(f"<th{html_attr(th_style)}>{inline_html(cell)}</th>" for cell in header) + "</tr></thead>")
    parts.append("<tbody>")
    for row_index, row in enumerate(body):
        shade = " background:#f8fbff;" if row_index % 2 else ""
        parts.append("<tr>" + "".join(f"<td{html_attr(td_style + shade)}>{inline_html(cell)}</td>" for cell in row) + "</tr>")
    parts.append("</tbody></table>")
    return "".join(parts)


def build_html(markdown: str) -> str:
    lines = markdown.splitlines()
    parts: list[str] = []
    index = 0
    in_code = False
    code_lines: list[str] = []
    list_kind: str | None = None

    def close_list() -> None:
        nonlocal list_kind
        if list_kind:
            parts.append(f"</{list_kind}>")
            list_kind = None

    while index < len(lines):
        line = lines[index]
        stripped = line.strip()

        if stripped.startswith("```"):
            if in_code:
                parts.append("<pre><code>" + html.escape("\n".join(code_lines)) + "</code></pre>")
                code_lines = []
                in_code = False
            else:
                in_code = True
            index += 1
            continue

        if in_code:
            code_lines.append(line)
            index += 1
            continue

        if not stripped:
            close_list()
            index += 1
            continue

        if stripped.startswith("|"):
            close_list()
            rows, index = parse_table(lines, index)
            parts.append(render_html_table(rows))
            continue

        if stripped.startswith("# "):
            close_list()
            title = stripped[2:]
            metadata: list[str] = []
            lookahead = index + 1
            while lookahead < len(lines):
                candidate = lines[lookahead].strip().rstrip()
                if not candidate:
                    lookahead += 1
                    continue
                if candidate.startswith("Prepared by") or candidate.startswith("Workspace:") or candidate.startswith("Final corrected run date:"):
                    metadata.append(candidate.rstrip("  "))
                    lookahead += 1
                    continue
                break
            parts.append(render_hero(title, metadata))
            parts.append(render_pipeline_html())
            index = lookahead
            continue
        elif stripped.startswith("## "):
            close_list()
            parts.append(f"<h2{html_attr('font:700 23px Aptos, Calibri, sans-serif; color:#17324d; margin:34px 0 10px; padding-bottom:7px; border-bottom:2px solid #d7e6f4;')}>{inline_html(stripped[3:])}</h2>")
        elif stripped.startswith("### "):
            close_list()
            parts.append(f"<h3{html_attr('font:700 17px Aptos, Calibri, sans-serif; color:#315470; margin:22px 0 8px;')}>{inline_html(stripped[4:])}</h3>")
        elif stripped.startswith("- "):
            if list_kind != "ul":
                close_list()
                parts.append(f"<ul{html_attr('margin:8px 0 14px 22px; padding:0; font:400 14px Aptos, Calibri, sans-serif; line-height:1.5; color:#24384d;')}>" )
                list_kind = "ul"
            parts.append(f"<li{html_attr('margin:4px 0;')}>{inline_html(stripped[2:])}</li>")
        elif re.match(r"^\d+\.\s+", stripped):
            if list_kind != "ol":
                close_list()
                parts.append(f"<ol{html_attr('margin:8px 0 14px 22px; padding:0; font:400 14px Aptos, Calibri, sans-serif; line-height:1.5; color:#24384d;')}>" )
                list_kind = "ol"
            item = re.sub(r"^\d+\.\s+", "", stripped)
            parts.append(f"<li{html_attr('margin:4px 0;')}>{inline_html(item)}</li>")
        elif stripped.startswith("> "):
            close_list()
            parts.append(f"<blockquote{html_attr('border-left:5px solid #7aa9d6; margin:16px 0; padding:10px 16px; background:#f0f7ff; color:#263d55; font:600 14px Aptos, Calibri, sans-serif;')}>{inline_html(stripped[2:])}</blockquote>")
        else:
            close_list()
            parts.append(f"<p{html_attr('margin:9px 0; font:400 14px Aptos, Calibri, sans-serif; line-height:1.55; color:#24384d;')}>{inline_html(stripped)}</p>")
        index += 1

    close_list()

    return f"""<!doctype html>
<html lang="en">
<head>
<meta charset="utf-8">
<title>AutoRepair Step 3 Integration Update</title>
<style>
:root {{
    --page: #eef5fb;
    --ink: #17324d;
    --muted: #61788c;
    --line: #cbddeb;
    --accent: #7aa9d6;
    --soft: #f7fbff;
}}
* {{
    box-sizing: border-box;
}}
html, body {{
    overflow-x: hidden;
}}
body {{
    margin: 0;
    background: var(--page);
    color: var(--ink);
    font-family: Aptos, Calibri, sans-serif;
    line-height: 1.55;
}}
main {{
    width: min(980px, calc(100% - 32px));
    max-width: 980px;
    margin: 28px auto;
    padding: 34px 40px 46px;
    background: #ffffff;
    border: 1px solid var(--line);
    box-shadow: 0 16px 38px rgba(48, 96, 138, .12);
}}
code {{
    font-family: Consolas, 'Courier New', monospace;
    background: #edf6ff;
    padding: 1px 4px;
    border-radius: 4px;
    color: #254b6d;
    overflow-wrap: anywhere;
}}
pre {{
    background: #f5f9ff;
    color: #1f344a;
    padding: 16px;
    overflow-x: auto;
    border-radius: 6px;
    font-size: 12.5px;
    border: 1px solid #cbddeb;
    border-left: 5px solid #8db8df;
}}
pre code {{ background: transparent; padding: 0; color: inherit; }}
@media print {{
  body {{ background: #fff; }}
    main {{ border: none; margin: 0; padding: 0; box-shadow:none; }}
}}
</style>
</head>
<body>
<main>
{chr(10).join(parts)}
</main>
</body>
</html>
"""


def add_runs_with_code(paragraph, text: str) -> None:
    parts = re.split(r"(`[^`]+`)", text)
    for part in parts:
        if not part:
            continue
        if part.startswith("`") and part.endswith("`"):
            run = paragraph.add_run(part[1:-1])
            run.font.name = "Consolas"
            run.font.size = Pt(9)
        else:
            paragraph.add_run(part.replace("  ", " "))


def shade_cell(cell, fill: str) -> None:
    tc_pr = cell._tc.get_or_add_tcPr()
    shd = OxmlElement("w:shd")
    shd.set(qn("w:fill"), fill)
    tc_pr.append(shd)


def set_cell_text(cell, text: str, *, bold: bool = False, size: float = 9.5) -> None:
    cell.text = ""
    paragraph = cell.paragraphs[0]
    add_runs_with_code(paragraph, text)
    for run in paragraph.runs:
        run.bold = bold
        run.font.size = Pt(size)


def add_docx_hero(document: Document) -> None:
    table = document.add_table(rows=1, cols=1)
    table.style = "Table Grid"
    cell = table.cell(0, 0)
    shade_cell(cell, "EAF4FF")
    paragraph = cell.paragraphs[0]
    paragraph.alignment = WD_ALIGN_PARAGRAPH.LEFT

    eyebrow = paragraph.add_run("AUTOREPAIR RESEARCH UPDATE\n")
    eyebrow.bold = True
    eyebrow.font.size = Pt(10)
    eyebrow.font.color.rgb = RGBColor(79, 127, 174)

    title = paragraph.add_run("AutoRepair Step 3 Integration Update for Prof. Jyoti\n")
    title.bold = True
    title.font.size = Pt(25)
    title.font.color.rgb = RGBColor(23, 50, 77)

    subtitle = paragraph.add_run(
        "Corrected Step 3 benchmark after the false-positive audit: 13 repositories attempted, "
        "10 traceable repositories, 239 final violations, and 6 generated candidate patches that compile individually.\n\n"
    )
    subtitle.font.size = Pt(11)
    subtitle.font.color.rgb = RGBColor(54, 85, 111)

    meta = paragraph.add_run(
        "Prepared by Manis and Dimitrios   |   "
        "Workspace: AutoRepair/multi_repo_workspace   |   "
        "Final corrected run date: 2026-05-25"
    )
    meta.font.size = Pt(9.5)
    meta.font.color.rgb = RGBColor(72, 103, 127)
    for run in paragraph.runs:
        run.font.name = "Aptos"
    document.add_paragraph()


def add_docx_pipeline(document: Document) -> None:
    rows = [
        ("Step 1", "Trace", "Instrument C# and record runtime events"),
        ("Step 2", "Constraints", "Convert events into violation records"),
        ("Step 3", "Repair", "Classify and synthesize patch candidates"),
        ("Verify", "Build", "Apply each candidate and compile-check it"),
    ]
    table = document.add_table(rows=1, cols=4)
    table.style = "Table Grid"
    for cell, (label, title, detail) in zip(table.rows[0].cells, rows):
        shade_cell(cell, "F7FBFF")
        cell.text = ""
        paragraph = cell.paragraphs[0]
        label_run = paragraph.add_run(label.upper() + "\n")
        label_run.bold = True
        label_run.font.size = Pt(8.5)
        label_run.font.color.rgb = RGBColor(91, 135, 178)
        title_run = paragraph.add_run(title + "\n")
        title_run.bold = True
        title_run.font.size = Pt(12)
        title_run.font.color.rgb = RGBColor(23, 50, 77)
        detail_run = paragraph.add_run(detail)
        detail_run.font.size = Pt(8.5)
        detail_run.font.color.rgb = RGBColor(94, 113, 130)
    document.add_paragraph()


def add_metric_cards(document: Document, rows: list[list[str]]) -> None:
    cards = rows[1:]
    for start in range(0, len(cards), 3):
        table = document.add_table(rows=1, cols=3)
        table.style = "Table Grid"
        for index, cell in enumerate(table.rows[0].cells):
            if start + index >= len(cards):
                shade_cell(cell, "FFFFFF")
                continue
            metric, value = cards[start + index]
            shade_cell(cell, "F7FBFF")
            cell.text = ""
            paragraph = cell.paragraphs[0]
            value_run = paragraph.add_run(value + "\n")
            value_run.bold = True
            value_run.font.size = Pt(18)
            value_run.font.color.rgb = RGBColor(23, 50, 77)
            metric_run = paragraph.add_run(metric)
            metric_run.font.size = Pt(8.5)
            metric_run.font.color.rgb = RGBColor(82, 104, 123)
        document.add_paragraph()


def add_table(document: Document, rows: list[list[str]]) -> None:
    if not rows:
        return
    if len(rows[0]) == 2 and rows[0][0] == "Metric" and rows[0][1] == "Final result":
        add_metric_cards(document, rows)
        return

    table = document.add_table(rows=1, cols=len(rows[0]))
    table.style = "Table Grid"
    header_cells = table.rows[0].cells
    for cell, value in zip(header_cells, rows[0]):
        shade_cell(cell, "E8F3FF")
        set_cell_text(cell, value, bold=True)
    for row in rows[1:]:
        cells = table.add_row().cells
        for index, value in enumerate(row):
            if index < len(cells):
                set_cell_text(cells[index], value)
    document.add_paragraph()


def build_docx(markdown: str) -> None:
    document = Document()
    styles = document.styles
    styles["Normal"].font.name = "Aptos"
    styles["Normal"].font.size = Pt(10.5)

    section = document.sections[0]
    section.top_margin = Inches(0.7)
    section.bottom_margin = Inches(0.7)
    section.left_margin = Inches(0.75)
    section.right_margin = Inches(0.75)

    add_docx_hero(document)
    add_docx_pipeline(document)

    lines = markdown.splitlines()
    index = 0
    in_code = False
    code_lines: list[str] = []

    while index < len(lines):
        line = lines[index]
        stripped = line.strip()

        if stripped.startswith("```"):
            if in_code:
                paragraph = document.add_paragraph()
                run = paragraph.add_run("\n".join(code_lines))
                run.font.name = "Consolas"
                run.font.size = Pt(8.5)
                code_lines = []
                in_code = False
            else:
                in_code = True
            index += 1
            continue

        if in_code:
            code_lines.append(line)
            index += 1
            continue

        if not stripped:
            index += 1
            continue

        if stripped.startswith("|"):
            rows, index = parse_table(lines, index)
            add_table(document, rows)
            continue

        if stripped.startswith("# "):
            index += 1
            continue
        if stripped.startswith("Prepared by") or stripped.startswith("Workspace:") or stripped.startswith("Final corrected run date:"):
            index += 1
            continue
        if stripped.startswith("## "):
            document.add_heading(stripped[3:], level=1)
        elif stripped.startswith("### "):
            document.add_heading(stripped[4:], level=2)
        elif stripped.startswith("- "):
            paragraph = document.add_paragraph(style="List Bullet")
            add_runs_with_code(paragraph, stripped[2:])
        elif re.match(r"^\d+\.\s+", stripped):
            paragraph = document.add_paragraph(style="List Number")
            add_runs_with_code(paragraph, re.sub(r"^\d+\.\s+", "", stripped))
        elif stripped.startswith("> "):
            paragraph = document.add_paragraph()
            run = paragraph.add_run(stripped[2:])
            run.italic = True
        else:
            paragraph = document.add_paragraph()
            add_runs_with_code(paragraph, stripped)

        index += 1

    document.save(DOCX_OUT)


def main() -> None:
    markdown = SOURCE.read_text(encoding="utf-8")
    HTML_OUT.write_text(build_html(markdown), encoding="utf-8")
    build_docx(markdown)
    print(f"Wrote {HTML_OUT}")
    print(f"Wrote {DOCX_OUT}")


if __name__ == "__main__":
    main()
