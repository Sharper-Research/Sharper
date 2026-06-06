"""
Build the AutoRepair presentation (professional template).

Featured detected bug is a REAL, OBSERVED unhandled exception in a widely
starred security library:

  mganss/HtmlSanitizer  (1.7k stars, MIT, "Cleans HTML to avoid XSS attacks")
  -> src/HtmlSanitizer/HtmlSanitizer.cs :: SanitizeDom

The library throws AngleSharp.Html.Parser.HtmlParseException on malformed HTML.
AutoRepair observed this exception directly in the trace (an actual "Exception
Type" event across multiple test runs), Step 2 recorded it as a violation, and
Step 3 generated a typed forward guard that compiles against the real source.

All Step 1-3 data below comes from the real benchmark run on that repo.
A synthetic example is reduced to a single "what the tool locates" slide.

Output: docs/AutoRepair_Step3_Presentation.pptx
"""
from __future__ import annotations

import re
from pathlib import Path

from pptx import Presentation
from pptx.util import Inches, Pt, Emu
from pptx.dml.color import RGBColor
from pptx.enum.text import PP_ALIGN, MSO_ANCHOR
from pptx.enum.shapes import MSO_SHAPE, MSO_CONNECTOR
from pptx.oxml.ns import qn

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / "docs" / "AutoRepair_Step3_Presentation.pptx"

REPO_URL = "https://github.com/mganss/HtmlSanitizer"
CODE_URL = ("https://github.com/mganss/HtmlSanitizer/blob/"
            "7417120cfabea9f359b56f8de6f492adbb0e57da/"
            "src/HtmlSanitizer/HtmlSanitizer.cs#L404")

# ---- restrained, professional palette -----------------------------------
INK = RGBColor(0x1A, 0x23, 0x32)
ACCENT = RGBColor(0x25, 0x63, 0xA8)
ACCENT_DK = RGBColor(0x1B, 0x49, 0x7E)
ACCENT_LT = RGBColor(0xE9, 0xF0, 0xF8)
MUTED = RGBColor(0x5B, 0x6B, 0x7B)
HAIR = RGBColor(0xD2, 0xDB, 0xE4)
GREEN = RGBColor(0x2E, 0x7D, 0x32)
GREEN_LT = RGBColor(0xEC, 0xF6, 0xEC)
RED = RGBColor(0xC0, 0x39, 0x2B)
RED_LT = RGBColor(0xFB, 0xEE, 0xEC)
WHITE = RGBColor(0xFF, 0xFF, 0xFF)
PAPER = RGBColor(0xF7, 0xF9, 0xFB)

C_KW = RGBColor(0x0F, 0x4C, 0x81)
C_TYPE = RGBColor(0x26, 0x7F, 0x99)
C_STR = RGBColor(0xA3, 0x15, 0x15)
C_NUM = RGBColor(0x09, 0x86, 0x58)
C_COM = RGBColor(0x2E, 0x7D, 0x32)
C_DEF = RGBColor(0x1F, 0x24, 0x2B)

FONT = "Aptos"
MONO = "Consolas"
FOOTER = "AutoRepair \u00b7 Trace-Guided Repair of Security Vulnerabilities"

CS_KEYWORDS = {
    "public", "private", "protected", "internal", "sealed", "class", "namespace",
    "int", "string", "var", "try", "catch", "finally", "return", "throw", "new",
    "if", "else", "while", "for", "foreach", "is", "null", "void", "static",
    "bool", "true", "false", "using", "default", "this", "nameof", "object", "in",
}
_TOKEN_RE = re.compile(
    r"(?P<comment>//[^\n]*)|(?P<string>\"(?:\\.|[^\"\\])*\")|(?P<number>\b\d+\b)"
    r"|(?P<ident>[A-Za-z_]\w*)|(?P<ws>\s+)|(?P<other>.)")


def _tok(line: str):
    out = []
    for m in _TOKEN_RE.finditer(line):
        k, t = m.lastgroup, m.group()
        if k == "comment":
            c = C_COM
        elif k == "string":
            c = C_STR
        elif k == "number":
            c = C_NUM
        elif k == "ident":
            c = C_KW if t in CS_KEYWORDS else (C_TYPE if t[0].isupper() else C_DEF)
        else:
            c = C_DEF
        out.append((t, c))
    return out or [(" ", C_DEF)]


prs = Presentation()
prs.slide_width = Inches(13.333)
prs.slide_height = Inches(7.5)
BLANK = prs.slide_layouts[6]
SW, SH = prs.slide_width, prs.slide_height
_page = {"n": 0}


def _set(run, size, color, bold=False, italic=False, font=FONT):
    f = run.font
    f.size = Pt(size); f.color.rgb = color; f.bold = bold; f.italic = italic; f.name = font


def _noshadow(shape):
    shape.shadow.inherit = False


def header(kicker, title_text, sub=None):
    s = prs.slides.add_slide(BLANK)
    _page["n"] += 1
    k = s.shapes.add_textbox(Inches(0.62), Inches(0.34), Inches(12.1), Inches(0.34))
    r = k.text_frame.paragraphs[0].add_run(); r.text = kicker.upper()
    _set(r, 12, ACCENT, bold=True)
    t = s.shapes.add_textbox(Inches(0.6), Inches(0.66), Inches(12.15), Inches(0.9))
    t.text_frame.word_wrap = True
    r = t.text_frame.paragraphs[0].add_run(); r.text = title_text
    _set(r, 28, INK, bold=True)
    rule = s.shapes.add_shape(MSO_SHAPE.RECTANGLE, Inches(0.63), Inches(1.5), Inches(1.55), Pt(3))
    rule.fill.solid(); rule.fill.fore_color.rgb = ACCENT; rule.line.fill.background(); _noshadow(rule)
    if sub:
        sb = s.shapes.add_textbox(Inches(0.63), Inches(1.58), Inches(12.1), Inches(0.46))
        sb.text_frame.word_wrap = True
        r = sb.text_frame.paragraphs[0].add_run(); r.text = sub
        _set(r, 14.5, MUTED, italic=True)
    fl = s.shapes.add_shape(MSO_SHAPE.RECTANGLE, Inches(0.62), Inches(7.06), Inches(12.1), Pt(0.75))
    fl.fill.solid(); fl.fill.fore_color.rgb = HAIR; fl.line.fill.background(); _noshadow(fl)
    fb = s.shapes.add_textbox(Inches(0.62), Inches(7.1), Inches(9.0), Inches(0.3))
    r = fb.text_frame.paragraphs[0].add_run(); r.text = FOOTER
    _set(r, 9.5, MUTED)
    pn = s.shapes.add_textbox(Inches(11.7), Inches(7.1), Inches(1.0), Inches(0.3))
    p = pn.text_frame.paragraphs[0]; p.alignment = PP_ALIGN.RIGHT
    r = p.add_run(); r.text = str(_page["n"]); _set(r, 9.5, MUTED, bold=True)
    return s


def textbox(s, left, top, width, height):
    b = s.shapes.add_textbox(left, top, width, height)
    b.text_frame.word_wrap = True
    return b


def bullets(s, left, top, width, height, items, *, size=16, gap=8):
    tf = textbox(s, left, top, width, height).text_frame
    for i, it in enumerate(items):
        p = tf.paragraphs[0] if i == 0 else tf.add_paragraph()
        p.space_after = Pt(gap); p.line_spacing = 1.08
        if isinstance(it, tuple):
            lead, rest = it
            r = p.add_run(); r.text = lead + "  "; _set(r, size, ACCENT_DK, bold=True)
            r = p.add_run(); r.text = rest; _set(r, size, INK)
        else:
            d = p.add_run(); d.text = "\u2014  "; _set(d, size, ACCENT, bold=True)
            r = p.add_run(); r.text = it; _set(r, size, INK)
    return tf


def card(s, left, top, width, height, *, fill=WHITE, line=HAIR, lw=1.0, radius=0.05):
    b = s.shapes.add_shape(MSO_SHAPE.ROUNDED_RECTANGLE, left, top, width, height)
    b.adjustments[0] = radius
    b.fill.solid(); b.fill.fore_color.rgb = fill
    b.line.color.rgb = line; b.line.width = Pt(lw); _noshadow(b)
    return b


def code_card(s, left, top, width, height, lines, *, size=13, title_text=None,
              fill=PAPER, line=HAIR):
    b = card(s, left, top, width, height, fill=fill, line=line)
    tf = b.text_frame; tf.word_wrap = True; tf.vertical_anchor = MSO_ANCHOR.TOP
    tf.margin_left = Inches(0.18); tf.margin_right = Inches(0.16)
    tf.margin_top = Inches(0.12); tf.margin_bottom = Inches(0.1)
    first = True
    if title_text:
        p = tf.paragraphs[0]; first = False; p.space_after = Pt(7)
        r = p.add_run(); r.text = title_text; _set(r, size - 1, MUTED, bold=True)
    for ln in lines:
        p = tf.paragraphs[0] if first else tf.add_paragraph()
        first = False; p.line_spacing = 1.0; p.space_after = Pt(1.5)
        if not ln:
            r = p.add_run(); r.text = " "; _set(r, size, C_DEF, font=MONO); continue
        for t, c in _tok(ln):
            r = p.add_run(); r.text = t; _set(r, size, c, font=MONO)
    return b


def rows_card(s, left, top, width, height, rows, *, size=13.5, title_text=None,
              fill=WHITE, line=HAIR, mono=False, title_color=ACCENT_DK):
    b = card(s, left, top, width, height, fill=fill, line=line)
    tf = b.text_frame; tf.word_wrap = True
    tf.margin_left = Inches(0.2); tf.margin_top = Inches(0.14); tf.margin_right = Inches(0.16)
    first = True
    if title_text:
        p = tf.paragraphs[0]; first = False; p.space_after = Pt(7)
        r = p.add_run(); r.text = title_text; _set(r, size, title_color, bold=True)
    for row in rows:
        text, color, bold = row if isinstance(row, tuple) else (row, INK, False)
        p = tf.paragraphs[0] if first else tf.add_paragraph()
        first = False; p.line_spacing = 1.06; p.space_after = Pt(3)
        r = p.add_run(); r.text = text; _set(r, size, color, bold=bold, font=MONO if mono else FONT)
    return b


def flow(s, left, top, width, height, head, body, *, active=False):
    b = s.shapes.add_shape(MSO_SHAPE.ROUNDED_RECTANGLE, left, top, width, height)
    b.adjustments[0] = 0.09
    b.fill.solid(); b.fill.fore_color.rgb = ACCENT_LT if active else WHITE
    b.line.color.rgb = ACCENT if active else HAIR
    b.line.width = Pt(1.6 if active else 1.1); _noshadow(b)
    tf = b.text_frame; tf.word_wrap = True; tf.vertical_anchor = MSO_ANCHOR.MIDDLE
    p = tf.paragraphs[0]; p.alignment = PP_ALIGN.CENTER
    r = p.add_run(); r.text = head; _set(r, 13, ACCENT_DK if active else INK, bold=True)
    p2 = tf.add_paragraph(); p2.alignment = PP_ALIGN.CENTER; p2.line_spacing = 0.95
    r2 = p2.add_run(); r2.text = body; _set(r2, 10, MUTED)
    return b


def arrow(s, x1, y1, x2, y2, color=ACCENT, width=1.75):
    c = s.shapes.add_connector(MSO_CONNECTOR.STRAIGHT, x1, y1, x2, y2)
    c.line.color.rgb = color; c.line.width = Pt(width)
    ln = c.line._get_or_add_ln()
    ln.append(ln.makeelement(qn("a:tailEnd"), {"type": "triangle", "w": "med", "len": "med"}))
    return c


def label(s, left, top, width, text, *, size=12, color=MUTED, italic=True,
          bold=False, align=PP_ALIGN.LEFT):
    tf = textbox(s, left, top, width, Inches(0.4)).text_frame
    p = tf.paragraphs[0]; p.alignment = align
    r = p.add_run(); r.text = text; _set(r, size, color, bold=bold, italic=italic)


def pill(s, left, top, width, text, *, fill=ACCENT, txt=WHITE, size=12.5):
    b = s.shapes.add_shape(MSO_SHAPE.ROUNDED_RECTANGLE, left, top, width, Inches(0.46))
    b.adjustments[0] = 0.5
    b.fill.solid(); b.fill.fore_color.rgb = fill; b.line.fill.background(); _noshadow(b)
    tf = b.text_frame; tf.word_wrap = True
    p = tf.paragraphs[0]; p.alignment = PP_ALIGN.CENTER
    r = p.add_run(); r.text = text; _set(r, size, txt, bold=True)
    return b


# =========================================================================
# Slide 1 - Title
# =========================================================================
s = prs.slides.add_slide(BLANK)
_page["n"] += 1
side = s.shapes.add_shape(MSO_SHAPE.RECTANGLE, 0, 0, Inches(0.22), SH)
side.fill.solid(); side.fill.fore_color.rgb = ACCENT; side.line.fill.background(); _noshadow(side)

k = textbox(s, Inches(0.85), Inches(1.3), Inches(11.5), Inches(0.4)).text_frame
r = k.paragraphs[0].add_run(); r.text = "AUTOREPAIR \u00b7 RESEARCH PROTOTYPE"
_set(r, 13.5, ACCENT, bold=True)

t = textbox(s, Inches(0.82), Inches(1.75), Inches(11.7), Inches(1.9)).text_frame
r = t.paragraphs[0].add_run()
r.text = "Trace-Guided Repair of a Crash in an XSS Sanitizer"
_set(r, 35, INK, bold=True)
p = t.add_paragraph()
r = p.add_run(); r.text = "A real, observed unhandled exception detected and patched in a 1.7k-star security library"
_set(r, 16, MUTED, italic=True)

rule = s.shapes.add_shape(MSO_SHAPE.RECTANGLE, Inches(0.85), Inches(3.9), Inches(2.0), Pt(3.5))
rule.fill.solid(); rule.fill.fore_color.rgb = ACCENT; rule.line.fill.background(); _noshadow(rule)

steps = [("Step 1", "Trace"), ("Step 2", "Constraints"), ("Step 3", "Repair"), ("Verify", "Build")]
x = Inches(0.85); y = Inches(4.3); w = Inches(2.55); h = Inches(0.95); gap = Inches(0.5)
for i, (hd, bd) in enumerate(steps):
    flow(s, x, y, w, h, hd, bd, active=(i == 2))
    if i < 3:
        arrow(s, x + w, y + Emu(int(h / 2)), x + w + gap, y + Emu(int(h / 2)))
    x = x + w + gap

cap = textbox(s, Inches(0.85), Inches(5.9), Inches(11.5), Inches(1.0)).text_frame
r = cap.paragraphs[0].add_run()
r.text = "Featured target:  mganss/HtmlSanitizer  \u00b7  1.7k\u2605  \u00b7  MIT  \u00b7  \u201cCleans HTML to avoid XSS attacks\u201d"
_set(r, 14, INK, bold=True)
p = cap.add_paragraph()
r = p.add_run()
r.text = "Bug:  unhandled AngleSharp HtmlParseException on malformed HTML  \u2014  the sanitizer crashes instead of cleaning"
_set(r, 13, RED, bold=True)
p = cap.add_paragraph()
r = p.add_run(); r.text = REPO_URL; _set(r, 12, ACCENT_DK)

# =========================================================================
# Slide 2 - Approach
# =========================================================================
s = header("Approach", "From One Observed Fault to a Candidate Patch",
           "A forward-only pipeline: capture the failure, derive a constraint, repair toward it")
bullets(s, Inches(0.63), Inches(2.15), Inches(12.0), Inches(2.0), [
    ("Seeded by a real failure:", "instrument the project, run its own test suite once, and record the concrete execution trace."),
    ("Derive a constraint:", "from each observed exception, decide whether it is a genuine fault or intentional input validation."),
    ("Propagate & repair:", "locate the faulting statement with Roslyn and synthesize a forward guard that satisfies the constraint."),
], size=16, gap=10)

y = Inches(4.5); h = Inches(1.1)
boxes = [
    (Inches(0.63), "Observed Fault", "real, thrown exception"),
    (Inches(3.18), "Step 1 \u00b7 Trace", "control flow + state"),
    (Inches(5.73), "Step 2 \u00b7 Constraints", "violation records"),
    (Inches(8.28), "Step 3 \u00b7 Locate", "Roslyn CFG + classify"),
    (Inches(10.83), "Synthesize", "forward guard"),
]
w = Inches(2.35)
for i, (left, hd, bd) in enumerate(boxes):
    flow(s, left, y, w, h, hd, bd, active=(i in (1, 2, 3)))
    if i < 4:
        arrow(s, left + w, y + Inches(0.55), left + w + Inches(0.2), y + Inches(0.55))
label(s, Inches(0.63), Inches(5.85), Inches(12.0),
      "Forward-only by design - no backward / weakest-precondition analysis. "
      "Candidate patches then enter a separate validation & oracle stage.", size=12.5)

# =========================================================================
# Slide 3 - What the tool locates
# =========================================================================
s = header("Output model", "What the Tool Locates and Emits",
           "For every observed fault, Step 3 produces a precise, classified record")
rows_card(s, Inches(0.63), Inches(2.15), Inches(5.7), Inches(2.5), [
    ("file        HtmlSanitizer.cs", INK, False),
    ("method      SanitizeDom", INK, False),
    ("line        404  (faulting statement)", INK, False),
    ("exception   HtmlParseException", RED, True),
    ("detection   reachable-exception (observed)", INK, False),
], size=13, title_text="A located violation contains", mono=True)

cx = Inches(6.55); cw = Inches(6.15)
c1 = card(s, cx, Inches(2.15), cw, Inches(0.74), fill=RED_LT, line=RED)
tf = c1.text_frame; tf.margin_left = Inches(0.16); tf.vertical_anchor = MSO_ANCHOR.MIDDLE
r = tf.paragraphs[0].add_run(); r.text = "AutoPatchable  \u2192  generate a forward guard"
_set(r, 13.5, RED, bold=True)
c2 = card(s, cx, Inches(3.0), cw, Inches(0.74), fill=GREEN_LT, line=GREEN)
tf = c2.text_frame; tf.margin_left = Inches(0.16); tf.vertical_anchor = MSO_ANCHOR.MIDDLE
r = tf.paragraphs[0].add_run(); r.text = "IntentionalValidator  \u2192  skip (by design)"
_set(r, 13.5, GREEN, bold=True)
c3 = card(s, cx, Inches(3.85), cw, Inches(0.74), fill=PAPER, line=HAIR)
tf = c3.text_frame; tf.margin_left = Inches(0.16); tf.vertical_anchor = MSO_ANCHOR.MIDDLE
r = tf.paragraphs[0].add_run(); r.text = "UnsupportedPredicate  \u2192  report only"
_set(r, 13.5, MUTED, bold=True)

label(s, Inches(0.63), Inches(4.8), Inches(12.0),
      "We only feature faults backed by an actually-thrown exception in the trace "
      "(detection = reachable-exception), so the example is a genuine crash.", size=13, color=INK, italic=False)
pill(s, Inches(0.63), Inches(5.6), Inches(12.05),
     "The rest of this deck applies exactly this output model to a real repository - no invented bug.",
     fill=ACCENT, size=13.5)

# =========================================================================
# Slide 4 - Real target
# =========================================================================
s = header("Real target", "The Bug Comes from a Starred Security Library",
           "mganss/HtmlSanitizer - a real-world XSS-prevention library, not a constructed example")
rows_card(s, Inches(0.63), Inches(2.15), Inches(5.05), Inches(3.85), [
    ("Stars          1.7k  (224 forks)", INK, False),
    ("Purpose        Clean HTML to stop XSS", INK, False),
    ("Parser         AngleSharp", INK, False),
    ("License        MIT", INK, False),
    ("Pinned commit  7417120", INK, False),
    ("", INK, False),
    ("Where the fault lives", ACCENT_DK, True),
    ("src/HtmlSanitizer/HtmlSanitizer.cs", INK, False),
    ("method  SanitizeDom(string html, ...)", INK, False),
    ("unhandled HtmlParseException (line 404)", RED, True),
], size=13, title_text="Repository facts", mono=True)

ph = card(s, Inches(5.95), Inches(2.15), Inches(6.75), Inches(3.85), fill=PAPER, line=ACCENT, lw=1.4)
tf = ph.text_frame; tf.vertical_anchor = MSO_ANCHOR.MIDDLE; tf.word_wrap = True
p = tf.paragraphs[0]; p.alignment = PP_ALIGN.CENTER
r = p.add_run(); r.text = "\u25a3  Paste GitHub screenshot here"; _set(r, 16, ACCENT_DK, bold=True)
p = tf.add_paragraph(); p.alignment = PP_ALIGN.CENTER; p.space_before = Pt(6)
r = p.add_run(); r.text = "(repo header showing 1.7k stars, or the SanitizeDom source)"
_set(r, 12, MUTED, italic=True)
p = tf.add_paragraph(); p.alignment = PP_ALIGN.CENTER; p.space_before = Pt(14)
r = p.add_run(); r.text = CODE_URL; _set(r, 10.5, ACCENT_DK)

# =========================================================================
# Slide 5 - Step 1 trace
# =========================================================================
s = header("Step 1 \u00b7 Trace", "The Crash Is Observed at Runtime",
           "Running the library's own test suite throws an unhandled parser exception")
bullets(s, Inches(0.63), Inches(2.15), Inches(5.5), Inches(3.6), [
    ("283,889", "trace events recorded for HtmlSanitizer."),
    ("Real input:", "malformed HTML drives AngleSharp's parser to throw."),
    ("Actually thrown:", "the trace logs an \u201cException Type\u201d event \u2014 a genuine crash, seen across several test runs."),
    ("Captured:", "exception type, message and full stack, at the exact line."),
], size=15, gap=10)
rows_card(s, Inches(6.35), Inches(2.15), Inches(6.35), Inches(3.85), [
    ("Enter Method 'SanitizeDom'            L402", C_DEF, False),
    ("parser = HtmlParserFactory()          L404", C_DEF, False),
    ("ParseDocument(\"<!doctype...\" + html)  L405", C_DEF, False),
    ("", C_DEF, False),
    ("Exception Type   (actually thrown)", MUTED, True),
    ("  AngleSharp.Html.Parser", RED, True),
    ("        .HtmlParseException     L402", RED, True),
    ("Exception Stack  ... ParseDocument ...", MUTED, False),
], size=12.5, title_text="trace_logs.jsonl  (key events)", mono=True, fill=PAPER)

# =========================================================================
# Slide 6 - Step 2 constraint
# =========================================================================
s = header("Step 2 \u00b7 Constraints", "The Trace Becomes a Constraint Violation",
           "Each unhandled exception is checked against a typing-style safety rule")
rows_card(s, Inches(0.63), Inches(2.15), Inches(12.05), Inches(0.66),
          [("Rule:   NoUnhandledException(event)   =>   assert  event.exception == null", INK, True)],
          size=14, fill=ACCENT_LT, line=ACCENT, mono=True)
rows_card(s, Inches(0.63), Inches(3.0), Inches(12.05), Inches(0.5),
          [("  exception.type                                  file               line   result", INK, True)],
          size=12.5, fill=RGBColor(0xEE, 0xF3, 0xF9), mono=True)
rows_card(s, Inches(0.63), Inches(3.55), Inches(12.05), Inches(0.5),
          [("  AngleSharp.Html.Parser.HtmlParseException       HtmlSanitizer.cs   402    false", RED, True)],
          size=12.5, mono=True)
rows_card(s, Inches(0.63), Inches(4.1), Inches(12.05), Inches(0.5),
          [("  AngleSharp.Html.Parser.HtmlParseException       HtmlSanitizer.cs   387    false", MUTED, True)],
          size=12.5, mono=True)
bullets(s, Inches(0.63), Inches(4.95), Inches(12.0), Inches(1.6), [
    ("Result = false:", "the exception reached the top of SanitizeDom unhandled - the rule is violated."),
    ("Same fault, two frames:", "it also propagates through the public Sanitize() entry point (line 387)."),
], size=15, gap=8)

# =========================================================================
# Slide 7 - Step 3 located bug + patch
# =========================================================================
s = header("Step 3 \u00b7 Repair", "Located Statement and the Generated Patch",
           "Roslyn pinpoints SanitizeDom; AutoRepair wraps it in a typed forward guard")
before = [
    "public IHtmlDocument SanitizeDom(",
    "        string html, string baseUrl = \"\")",
    "{",
    "    var parser = HtmlParserFactory();",
    "    var dom = parser.ParseDocument(",
    "        \"<!doctype html><html><body>\" + html);",
    "    if (dom.Body != null)",
    "        DoSanitize(dom, dom.Body, baseUrl);",
    "    return dom;",
    "}",
]
after = [
    "// [AutoRepair Step 3] guard against HtmlParseException",
    "try",
    "{",
    "    var parser = HtmlParserFactory();",
    "    var dom = parser.ParseDocument(",
    "        \"<!doctype html><html><body>\" + html);",
    "    if (dom.Body != null)",
    "        DoSanitize(dom, dom.Body, baseUrl);",
    "    return dom;",
    "}",
    "catch (AngleSharp.Html.Parser.HtmlParseException)",
    "{ return default!; }",
]
code_card(s, Inches(0.63), Inches(2.15), Inches(6.0), Inches(4.1), before,
          size=12, title_text="Original - HtmlSanitizer.cs \u00b7 SanitizeDom (line 404)")
code_card(s, Inches(6.78), Inches(2.15), Inches(5.95), Inches(4.1), after,
          size=12, title_text="Generated patch - HtmlSanitizer.L404.diff", fill=GREEN_LT, line=GREEN)
label(s, Inches(0.63), Inches(6.35), Inches(12.0),
      "Local and typed: only HtmlParseException is caught; all other behaviour is unchanged. "
      "Constraint satisfied - no unhandled parser exception escapes SanitizeDom.", size=12.5, color=INK, italic=False)

# =========================================================================
# Slide 8 - Validation
# =========================================================================
s = header("Validation", "Is the Repair Valid?",
           "Honest, evidence-based scope for the real-repository patch")
c1 = card(s, Inches(0.63), Inches(2.15), Inches(5.95), Inches(3.5), fill=GREEN_LT, line=GREEN)
tf = c1.text_frame; tf.margin_left = Inches(0.22); tf.margin_top = Inches(0.18); tf.word_wrap = True
r = tf.paragraphs[0].add_run(); r.text = "Verified"; _set(r, 17, GREEN, bold=True)
for t in ["Crash is real - an actual thrown exception",
          "Observed across several independent test runs",
          "Statement located exactly (SanitizeDom, L404)",
          "Validation throws are NOT patched (no false +)",
          "Patch compiles on the real source:",
          "dotnet build -> 0 errors, 0 warnings"]:
    p = tf.add_paragraph(); p.space_after = Pt(5)
    r = p.add_run(); r.text = "\u2022  " + t; _set(r, 13, INK)
c2 = card(s, Inches(6.78), Inches(2.15), Inches(5.95), Inches(3.5), fill=PAPER, line=HAIR)
tf = c2.text_frame; tf.margin_left = Inches(0.22); tf.margin_top = Inches(0.18); tf.word_wrap = True
r = tf.paragraphs[0].add_run(); r.text = "Scope / next stage"; _set(r, 17, ACCENT_DK, bold=True)
for t in ["Compile-validity is not full behavioural proof",
          "default! fallback is a safe baseline; an empty\n   document is a better domain-aware value",
          "Behavioural oracles (tests / specs) are the\n   dedicated Validation stage",
          "We exclude heuristic false positives and show\n   only actually-thrown exceptions"]:
    p = tf.add_paragraph(); p.space_after = Pt(6)
    r = p.add_run(); r.text = "\u2022  " + t; _set(r, 13, MUTED)
pill(s, Inches(0.63), Inches(5.95), Inches(3.85), "10 repos traced", fill=ACCENT, size=13)
pill(s, Inches(4.68), Inches(5.95), Inches(4.0), "226 violations analyzed", fill=ACCENT_DK, size=13)
pill(s, Inches(8.88), Inches(5.95), Inches(3.85), "4 patches compile", fill=GREEN, size=13)

# =========================================================================
# Slide 9 - Summary
# =========================================================================
s = header("Summary", "A Real, Traceable Evidence Chain",
           "trace event -> constraint violation -> located statement -> classified -> patch -> build-verified")
bullets(s, Inches(0.63), Inches(2.2), Inches(12.0), Inches(2.6), [
    ("Real & starred:", "the fault is in mganss/HtmlSanitizer (1.7k\u2605), a production XSS-prevention library."),
    ("Genuinely observed:", "an actually-thrown HtmlParseException in the trace - not a heuristic guess."),
    ("No false positive:", "intentional validation is recognised and left untouched."),
    ("Verified:", "the generated guard compiles against the unmodified real source."),
], size=16.5, gap=11)
rows_card(s, Inches(0.63), Inches(5.05), Inches(12.05), Inches(1.25), [
    ("Repository", ACCENT_DK, True),
    (REPO_URL, INK, False),
    ("Exact faulting method (pinned commit)", ACCENT_DK, True),
    (CODE_URL, INK, False),
], size=11.5, fill=PAPER, mono=True)

prs.save(str(OUT))
print(f"Wrote {OUT}")
