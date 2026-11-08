"""
Builds mqpixel.ttf and mqpixeli.ttf (letters slanted), the pixel fonts used with TEXDraw for the Math Quest math display.

Run:   python tools/pixel-font/build_mqpixel.py
Needs: pip install fonttools

Design:
  * 1 pixel = 100 font units, 1 em = 1000 units = 10 pixels.
    So the font is perfectly crisp whenever its on-screen size is a multiple of 10.
  * Ascent 8 px, descent 2 px. Capitals and digits are 7 px tall, lowercase 5 px tall.
  * Digits are all the same width. Operators are centred on the middle of a digit (row 3).

To add a symbol: add a bitmap to GLYPHS (rows top to bottom, '#' = pixel, '.' = empty,
`d` = how many rows hang below the baseline) and map its code point in CODEPOINTS.
Then run this script again and re-import the font in Unity.
"""

from pathlib import Path

from fontTools.fontBuilder import FontBuilder
from fontTools.pens.ttGlyphPen import TTGlyphPen

PX = 100  # font units per pixel
UPM = 1000
ASCENT = 800
DESCENT = -200
GAP = 1  # blank pixel columns after each glyph

FONT_DIR = (
    Path(__file__).resolve().parents[2]
    / "Math Quest/Assets/Math Quest/Imported Assets/TEXDraw/Fonts/User"
)

# name -> (rows, d)
GLYPHS = {}


def g(name, rows, d=0):
    GLYPHS[name] = (rows, d)


# ---- Capitals (7 rows) ------------------------------------------------------
g("A", [".###.", "#...#", "#...#", "#####", "#...#", "#...#", "#...#"])
g("B", ["####.", "#...#", "#...#", "####.", "#...#", "#...#", "####."])
g("C", [".###.", "#...#", "#....", "#....", "#....", "#...#", ".###."])
g("D", ["####.", "#...#", "#...#", "#...#", "#...#", "#...#", "####."])
g("E", ["#####", "#....", "#....", "####.", "#....", "#....", "#####"])
g("F", ["#####", "#....", "#....", "####.", "#....", "#....", "#...."])
g("G", [".###.", "#...#", "#....", "#.###", "#...#", "#...#", ".###."])
g("H", ["#...#", "#...#", "#...#", "#####", "#...#", "#...#", "#...#"])
g("I", ["###", ".#.", ".#.", ".#.", ".#.", ".#.", "###"])
g("J", ["..###", "...#.", "...#.", "...#.", "...#.", "#..#.", ".##.."])
g("K", ["#...#", "#..#.", "#.#..", "##...", "#.#..", "#..#.", "#...#"])
g("L", ["#....", "#....", "#....", "#....", "#....", "#....", "#####"])
g("M", ["#...#", "##.##", "#.#.#", "#.#.#", "#...#", "#...#", "#...#"])
g("N", ["#...#", "##..#", "##..#", "#.#.#", "#..##", "#..##", "#...#"])
g("O", [".###.", "#...#", "#...#", "#...#", "#...#", "#...#", ".###."])
g("P", ["####.", "#...#", "#...#", "####.", "#....", "#....", "#...."])
g("Q", [".###.", "#...#", "#...#", "#...#", "#.#.#", "#..#.", ".##.#"])
g("R", ["####.", "#...#", "#...#", "####.", "#.#..", "#..#.", "#...#"])
g("S", [".####", "#....", "#....", ".###.", "....#", "....#", "####."])
g("T", ["#####", "..#..", "..#..", "..#..", "..#..", "..#..", "..#.."])
g("U", ["#...#", "#...#", "#...#", "#...#", "#...#", "#...#", ".###."])
g("V", ["#...#", "#...#", "#...#", "#...#", "#...#", ".#.#.", "..#.."])
g("W", ["#...#", "#...#", "#...#", "#.#.#", "#.#.#", "##.##", "#...#"])
g("X", ["#...#", "#...#", ".#.#.", "..#..", ".#.#.", "#...#", "#...#"])
g("Y", ["#...#", "#...#", ".#.#.", "..#..", "..#..", "..#..", "..#.."])
g("Z", ["#####", "....#", "...#.", "..#..", ".#...", "#....", "#####"])

# ---- Digits (7 rows, all 5 wide) --------------------------------------------
g("zero", [".###.", "#...#", "#..##", "#.#.#", "##..#", "#...#", ".###."])
g("one", ["..#..", ".##..", "..#..", "..#..", "..#..", "..#..", ".###."])
g("two", [".###.", "#...#", "....#", "...#.", "..#..", ".#...", "#####"])
g("three", [".###.", "#...#", "....#", "..##.", "....#", "#...#", ".###."])
g("four", ["...#.", "..##.", ".#.#.", "#..#.", "#####", "...#.", "...#."])
g("five", ["#####", "#....", "####.", "....#", "....#", "#...#", ".###."])
g("six", ["..##.", ".#...", "#....", "####.", "#...#", "#...#", ".###."])
g("seven", ["#####", "....#", "...#.", "..#..", ".#...", ".#...", ".#..."])
g("eight", [".###.", "#...#", "#...#", ".###.", "#...#", "#...#", ".###."])
g("nine", [".###.", "#...#", "#...#", ".####", "....#", "...#.", ".##.."])

# ---- Lowercase (5 rows x-height, 7 with ascender, +2 with descender) --------
g("a_", [".###.", "....#", ".####", "#...#", ".####"])
g("b_", ["#....", "#....", "####.", "#...#", "#...#", "#...#", "####."])
g("c_", [".###.", "#...#", "#....", "#...#", ".###."])
g("d_", ["....#", "....#", ".####", "#...#", "#...#", "#...#", ".####"])
g("e_", [".###.", "#...#", "#####", "#....", ".###."])
g("f_", ["..##.", ".#...", "###..", ".#...", ".#...", ".#...", ".#..."])
g("g_", [".####", "#...#", "#...#", "#...#", ".####", "....#", ".###."], d=2)
g("h_", ["#....", "#....", "####.", "#...#", "#...#", "#...#", "#...#"])
g("i_", ["#", ".", "#", "#", "#", "#", "#"])
g("j_", ["..#", "...", "..#", "..#", "..#", "..#", "..#", "..#", "##."], d=2)
g("k_", ["#...", "#...", "#..#", "#.#.", "##..", "#.#.", "#..#"])
g("l_", ["##.", ".#.", ".#.", ".#.", ".#.", ".#.", ".##"])
g("m_", ["##.#.", "#.#.#", "#.#.#", "#.#.#", "#.#.#"])
g("n_", ["####.", "#...#", "#...#", "#...#", "#...#"])
g("o_", [".###.", "#...#", "#...#", "#...#", ".###."])
g("p_", ["####.", "#...#", "#...#", "#...#", "####.", "#....", "#...."], d=2)
g("q_", [".####", "#...#", "#...#", "#...#", ".####", "....#", "....#"], d=2)
g("r_", ["#.##.", "##..#", "#....", "#....", "#...."])
g("s_", [".####", "#....", ".###.", "....#", "####."])
g("t_", [".#..", "###.", ".#..", ".#..", ".#..", ".##."])
g("u_", ["#...#", "#...#", "#...#", "#...#", ".####"])
g("v_", ["#...#", "#...#", "#...#", ".#.#.", "..#.."])
g("w_", ["#...#", "#...#", "#.#.#", "#.#.#", ".#.#."])
g("x_", ["#...#", ".#.#.", "..#..", ".#.#.", "#...#"])
g("y_", ["#...#", "#...#", "#...#", "#...#", ".####", "....#", ".###."], d=2)
g("z_", ["#####", "...#.", "..#..", ".#...", "#####"])

# ---- Math and punctuation ---------------------------------------------------
g("plus", [".....", "..#..", "..#..", "#####", "..#..", "..#..", "....."])
g("minus", [".....", ".....", ".....", "#####", ".....", ".....", "....."])
g("hyphen", ["...", "...", "...", "###", "...", "...", "..."])
g("times", [".....", "#...#", ".#.#.", "..#..", ".#.#.", "#...#", "....."])
g("divide", [".....", "..#..", ".....", "#####", ".....", "..#..", "....."])
g("equal", [".....", ".....", "#####", ".....", "#####", ".....", "....."])
g("less", [".....", "...#.", "..#..", ".#...", "..#..", "...#.", "....."])
g("greater", [".....", ".#...", "..#..", "...#.", "..#..", ".#...", "....."])
g("lessequal", [".....", "...#.", "..#..", ".#...", "..#..", "...#.", "#####"])
g("greaterequal", [".....", ".#...", "..#..", "...#.", "..#..", ".#...", "#####"])
g("notequal", ["....#", "...#.", "#####", "..#..", "#####", ".#...", "#...."])
g("plusminus", ["..#..", "..#..", "#####", "..#..", "..#..", ".....", "#####"])
g("paren_l", ["..#", ".#.", "#..", "#..", "#..", "#..", ".#.", "..#"], d=1)
g("paren_r", ["#..", ".#.", "..#", "..#", "..#", "..#", ".#.", "#.."], d=1)
g("bracket_l", ["###", "#..", "#..", "#..", "#..", "#..", "#..", "###"], d=1)
g("bracket_r", ["###", "..#", "..#", "..#", "..#", "..#", "..#", "###"], d=1)
g("brace_l", ["..#", ".#.", ".#.", "#..", ".#.", ".#.", ".#.", "..#"], d=1)
g("brace_r", ["#..", ".#.", ".#.", "..#", ".#.", ".#.", ".#.", "#.."], d=1)
g("period", ["#"])
g("comma", ["#", "#"], d=1)
g("colon", ["#", ".", ".", ".", "#"])
g("semicolon", ["#", ".", ".", ".", "#", "#"], d=1)
g("exclam", ["#", "#", "#", "#", "#", ".", "#"])
g("question", [".###.", "#...#", "....#", "...#.", "..#..", ".....", "..#.."])
g("slash", ["....#", "....#", "...#.", "..#..", ".#...", "#....", "#...."])
g("bar", ["#", "#", "#", "#", "#", "#", "#", "#"], d=1)
g("caret", ["..#..", ".#.#.", "#...#"])
g("underscore", ["#####"], d=-0)  # placed below the baseline, see build()
g("percent", ["##..#", "##..#", "...#.", "..#..", ".#...", "#..##", "#..##"])
g("pi", ["#####", ".#.#.", ".#.#.", ".#.#.", ".#..#"])

# code point -> glyph name
CODEPOINTS = {0x20: "space"}
for ch, name in zip("ABCDEFGHIJKLMNOPQRSTUVWXYZ", "ABCDEFGHIJKLMNOPQRSTUVWXYZ"):
    CODEPOINTS[ord(ch)] = name
for ch, name in zip(
    "0123456789",
    ["zero", "one", "two", "three", "four", "five", "six", "seven", "eight", "nine"],
):
    CODEPOINTS[ord(ch)] = name
for ch in "abcdefghijklmnopqrstuvwxyz":
    CODEPOINTS[ord(ch)] = ch + "_"
CODEPOINTS.update(
    {
        ord("+"): "plus",
        0x2212: "minus",
        ord("-"): "hyphen",
        0xD7: "times",
        0xF7: "divide",
        ord("="): "equal",
        ord("<"): "less",
        ord(">"): "greater",
        0x2264: "lessequal",
        0x2265: "greaterequal",
        0x2260: "notequal",
        0xB1: "plusminus",
        ord("("): "paren_l",
        ord(")"): "paren_r",
        ord("["): "bracket_l",
        ord("]"): "bracket_r",
        ord("{"): "brace_l",
        ord("}"): "brace_r",
        ord("."): "period",
        ord(","): "comma",
        ord(":"): "colon",
        ord(";"): "semicolon",
        ord("!"): "exclam",
        ord("?"): "question",
        ord("/"): "slash",
        ord("|"): "bar",
        ord("^"): "caret",
        ord("_"): "underscore",
        ord("%"): "percent",
        0x3C0: "pi",
    }
)

SPACE_WIDTH = 3  # pixels


def runs(rows):
    """Per-row horizontal runs, then merge identical runs in neighbouring rows."""
    boxes = []  # [x0, x1, row0, row1]
    open_runs = {}
    for r, row in enumerate(rows):
        current = set()
        x = 0
        while x < len(row):
            if row[x] == "#":
                start = x
                while x < len(row) and row[x] == "#":
                    x += 1
                key = (start, x)
                current.add(key)
                if key in open_runs:
                    open_runs[key][3] = r + 1
                else:
                    open_runs[key] = [start, x, r, r + 1]
            else:
                x += 1
        for key in list(open_runs):
            if key not in current:
                boxes.append(open_runs.pop(key))
    boxes.extend(open_runs.values())
    return boxes


def is_letter(name):
    return name == "pi" or (len(name) == 1 and name.isupper()) or (len(name) == 2 and name.endswith("_"))


def slant(rows, d):
    """Pixel italic: the upper rows (3 px or more above the baseline) move right 1 px."""
    out = []
    shifts = []
    for r, row in enumerate(rows):
        y = len(rows) - d - r - 1  # 0 = first row above the baseline
        shift = 1 if y >= 3 else 0
        shifts.append(shift)
        out.append("." * shift + row)
    width = max(len(r) for r in out)
    return [r.ljust(width, ".") for r in out]


def build_glyph(name, italic=False):
    rows, d = GLYPHS[name]
    if italic and is_letter(name):
        rows = slant(rows, d)
    pen = TTGlyphPen(None)
    width_px = max(len(r) for r in rows)
    min_x = min((b[0] for b in runs(rows)), default=0)
    # Underscore lives wholly below the baseline.
    drop = 1 if name == "underscore" else 0
    for x0, x1, r0, r1 in runs(rows):
        top = (len(rows) - r0 - d - drop) * PX
        bottom = (len(rows) - r1 - d - drop) * PX
        left, right = x0 * PX, x1 * PX
        # Clockwise outer contour (TrueType fill direction).
        pen.moveTo((left, bottom))
        pen.lineTo((left, top))
        pen.lineTo((right, top))
        pen.lineTo((right, bottom))
        pen.closePath()
    return pen.glyph(), (width_px + GAP) * PX, min_x * PX


def build(italic):
    names = [".notdef", "space"] + list(GLYPHS)
    glyphs = {}
    metrics = {}

    # .notdef: an empty box so missing characters are visible.
    notdef = TTGlyphPen(None)
    for box in ((0, 0, 500, 100), (0, 600, 500, 700), (0, 100, 100, 600), (400, 100, 500, 600)):
        l, b, r, t = box
        notdef.moveTo((l, b))
        notdef.lineTo((l, t))
        notdef.lineTo((r, t))
        notdef.lineTo((r, b))
        notdef.closePath()
    glyphs[".notdef"] = notdef.glyph()
    metrics[".notdef"] = (600, 0)

    glyphs["space"] = TTGlyphPen(None).glyph()
    metrics["space"] = (SPACE_WIDTH * PX, 0)

    for name in GLYPHS:
        glyph, advance, lsb = build_glyph(name, italic)
        glyphs[name] = glyph
        metrics[name] = (advance, lsb)

    style = "Italic" if italic else "Regular"
    out = FONT_DIR / ("mqpixeli.ttf" if italic else "mqpixel.ttf")
    fb = FontBuilder(UPM, isTTF=True)
    fb.setupGlyphOrder(names)
    fb.setupCharacterMap(CODEPOINTS)
    fb.setupGlyf(glyphs)
    fb.setupHorizontalMetrics({n: metrics[n] for n in names})
    fb.setupHorizontalHeader(ascent=ASCENT, descent=DESCENT, lineGap=0)
    fb.setupNameTable(
        {
            "familyName": "MQPixel",
            "styleName": style,
            "uniqueFontIdentifier": f"MQPixel-{style}",
            "fullName": f"MQPixel {style}",
            "psName": f"MQPixel-{style}",
            "version": "Version 0.1",
        }
    )
    fb.setupOS2(
        sTypoAscender=ASCENT,
        sTypoDescender=DESCENT,
        sTypoLineGap=0,
        usWinAscent=ASCENT,
        usWinDescent=-DESCENT,
        sxHeight=5 * PX,
        sCapHeight=7 * PX,
        fsType=0,
    )
    fb.setupPost(isFixedPitch=0)
    out.parent.mkdir(parents=True, exist_ok=True)
    fb.save(str(out))
    print(f"wrote {out} ({len(GLYPHS)} glyphs, {len(CODEPOINTS)} characters)")


def main():
    build(italic=False)
    build(italic=True)  # letters lean, digits and operators stay upright


if __name__ == "__main__":
    main()
