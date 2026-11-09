"""
Generates Prefabs/KeypadHolder.prefab: the number keypad layout (display + 5 rows of keys).

Run:  python tools/keypad-prefab/build_keypad_prefab.py

The output is an ordinary Unity prefab. After generating it you can open it in Unity and
restyle or move anything; the NumberKeypadPanel script only needs its button and display
references to stay assigned. The generator is kept so the layout can be rebuilt from numbers
(key size, spacing) without clicking through 17 objects.

Layout (all sizes in canvas units):
    Display                                  height DISPLAY_HEIGHT
    Row_Top      [ C ][ +/- ][ DEL ]         height KEY_HEIGHT
    Row_789      [ 7 ][ 8 ][ 9 ]
    Row_456      [ 4 ][ 5 ][ 6 ]
    Row_123      [ 1 ][ 2 ][ 3 ]
    Row_Bottom   [   0   ][ . ]              0 is twice as wide as .
"""

from pathlib import Path

PREFAB_DIR = Path(__file__).resolve().parents[2] / "Math Quest/Assets/Math Quest/Prefabs"
PREFAB_NAME = "KeypadHolder"
PREFAB_GUID = "f1b50b3bb0514d7eaa76f0f64a28ba9b"

# NumberKeypadPanel.cs script guid (Scripts/Battle/NumberKeypadPanel.cs.meta)
KEYPAD_SCRIPT_GUID = "5685eee2323f4104b7d39e3433be2cff"

# ---- layout numbers: change these and rerun ----------------------------------------------
PADDING = 20          # space around everything inside the holder
SPACING = 15          # space between rows and between keys
DISPLAY_HEIGHT = 120
KEY_HEIGHT = 110

# ---- shared look, copied from the OptionChoice prefab ------------------------------------
SPRITE = "{fileID: 21300000, guid: c8d2f0157fb1de94b87fd1a11aa1374e, type: 3}"
FONT = "{fileID: 11400000, guid: aae13cdd641b644499de72cb68aee384, type: 2}"
FONT_MATERIAL = "{fileID: -8766518712420215560, guid: aae13cdd641b644499de72cb68aee384, type: 2}"
PPU_MULTIPLIER = 0.14

# Unity UI script guids
IMAGE = "fe87c0e1cc204ed48ad3b37840f39efc"
BUTTON = "4e29b1a8efbd4b44bb3f3716e73f07ff"
TMP = "f4688fdb7df04437aeb418b961361dc5"
VLG = "59f8146938fff824cb5fd77236b75775"
HLG = "30649d3a9faa99c48a7b1166b86bf2a0"
LAYOUT_ELEMENT = "306cc8c2b49d7114eaa3623786fc2126"

_next_id = 5100000000000000000


def new_id():
    global _next_id
    _next_id += 1
    return _next_id


out = []


def emit(text):
    out.append(text.rstrip("\n") + "\n")


def game_object(go_id, name, component_ids, active=1):
    comps = "".join(f"  - component: {{fileID: {c}}}\n" for c in component_ids)
    emit(f"""--- !u!1 &{go_id}
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  serializedVersion: 6
  m_Component:
{comps}  m_Layer: 5
  m_Name: {name}
  m_TagString: Untagged
  m_Icon: {{fileID: 0}}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: {active}""")


def rect_transform(rt_id, go_id, father_id, children, anchor_min, anchor_max, pos, size):
    kids = "".join(f"  - {{fileID: {c}}}\n" for c in children)
    kids = kids if kids else ""
    children_block = f"  m_Children:\n{kids}" if kids else "  m_Children: []\n"
    emit(f"""--- !u!224 &{rt_id}
RectTransform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {go_id}}}
  m_LocalRotation: {{x: 0, y: 0, z: 0, w: 1}}
  m_LocalPosition: {{x: 0, y: 0, z: 0}}
  m_LocalScale: {{x: 1, y: 1, z: 1}}
  m_ConstrainProportionsScale: 0
{children_block}  m_Father: {{fileID: {father_id}}}
  m_LocalEulerAnglesHint: {{x: 0, y: 0, z: 0}}
  m_AnchorMin: {{x: {anchor_min[0]}, y: {anchor_min[1]}}}
  m_AnchorMax: {{x: {anchor_max[0]}, y: {anchor_max[1]}}}
  m_AnchoredPosition: {{x: {pos[0]}, y: {pos[1]}}}
  m_SizeDelta: {{x: {size[0]}, y: {size[1]}}}
  m_Pivot: {{x: 0.5, y: 0.5}}""")


def canvas_renderer(cr_id, go_id):
    emit(f"""--- !u!222 &{cr_id}
CanvasRenderer:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {go_id}}}
  m_CullTransparentMesh: 1""")


def canvas_group(cg_id, go_id):
    emit(f"""--- !u!225 &{cg_id}
CanvasGroup:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {go_id}}}
  m_Enabled: 1
  m_Alpha: 1
  m_Interactable: 1
  m_BlocksRaycasts: 1
  m_IgnoreParentGroups: 0""")


def behaviour_header(comp_id, go_id, guid, class_id):
    return f"""--- !u!114 &{comp_id}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {go_id}}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: {guid}, type: 3}}
  m_Name:
  m_EditorClassIdentifier: {class_id}
"""


def image(img_id, go_id, color):
    r, g, b = color
    emit(behaviour_header(img_id, go_id, IMAGE, "UnityEngine.UI::UnityEngine.UI.Image") + f"""  m_Material: {{fileID: 0}}
  m_Color: {{r: {r}, g: {g}, b: {b}, a: 1}}
  m_RaycastTarget: 1
  m_RaycastPadding: {{x: 0, y: 0, z: 0, w: 0}}
  m_Maskable: 1
  m_OnCullStateChanged:
    m_PersistentCalls:
      m_Calls: []
  m_Sprite: {SPRITE}
  m_Type: 1
  m_PreserveAspect: 0
  m_FillCenter: 1
  m_FillMethod: 4
  m_FillAmount: 1
  m_FillClockwise: 1
  m_FillOrigin: 0
  m_UseSpriteMesh: 0
  m_PixelsPerUnitMultiplier: {PPU_MULTIPLIER}""")


def button(btn_id, go_id, target_graphic_id):
    emit(behaviour_header(btn_id, go_id, BUTTON, "UnityEngine.UI::UnityEngine.UI.Button") + f"""  m_Navigation:
    m_Mode: 3
    m_WrapAround: 0
    m_SelectOnUp: {{fileID: 0}}
    m_SelectOnDown: {{fileID: 0}}
    m_SelectOnLeft: {{fileID: 0}}
    m_SelectOnRight: {{fileID: 0}}
  m_Transition: 1
  m_Colors:
    m_NormalColor: {{r: 1, g: 1, b: 1, a: 1}}
    m_HighlightedColor: {{r: 0.9607843, g: 0.9607843, b: 0.9607843, a: 1}}
    m_PressedColor: {{r: 0.7, g: 0.7, b: 0.7, a: 1}}
    m_SelectedColor: {{r: 0.9607843, g: 0.9607843, b: 0.9607843, a: 1}}
    m_DisabledColor: {{r: 0.78431374, g: 0.78431374, b: 0.78431374, a: 0.5019608}}
    m_ColorMultiplier: 1
    m_FadeDuration: 0.1
  m_SpriteState:
    m_HighlightedSprite: {{fileID: 0}}
    m_PressedSprite: {{fileID: 0}}
    m_SelectedSprite: {{fileID: 0}}
    m_DisabledSprite: {{fileID: 0}}
  m_AnimationTriggers:
    m_NormalTrigger: Normal
    m_HighlightedTrigger: Highlighted
    m_PressedTrigger: Pressed
    m_SelectedTrigger: Selected
    m_DisabledTrigger: Disabled
  m_Interactable: 1
  m_TargetGraphic: {{fileID: {target_graphic_id}}}
  m_OnClick:
    m_PersistentCalls:
      m_Calls: []""")


def tmp_text(tmp_id, go_id, text, font_max, font_min):
    emit(behaviour_header(tmp_id, go_id, TMP, "Unity.TextMeshPro::TMPro.TextMeshProUGUI") + f"""  m_Material: {{fileID: 0}}
  m_Color: {{r: 1, g: 1, b: 1, a: 1}}
  m_RaycastTarget: 1
  m_RaycastPadding: {{x: 0, y: 0, z: 0, w: 0}}
  m_Maskable: 1
  m_OnCullStateChanged:
    m_PersistentCalls:
      m_Calls: []
  m_text: '{text}'
  m_isRightToLeft: 0
  m_fontAsset: {FONT}
  m_sharedMaterial: {FONT_MATERIAL}
  m_fontSharedMaterials: []
  m_fontMaterial: {{fileID: 0}}
  m_fontMaterials: []
  m_fontColor32:
    serializedVersion: 2
    rgba: 4278190080
  m_fontColor: {{r: 0, g: 0, b: 0, a: 1}}
  m_enableVertexGradient: 0
  m_colorMode: 3
  m_fontColorGradient:
    topLeft: {{r: 1, g: 1, b: 1, a: 1}}
    topRight: {{r: 1, g: 1, b: 1, a: 1}}
    bottomLeft: {{r: 1, g: 1, b: 1, a: 1}}
    bottomRight: {{r: 1, g: 1, b: 1, a: 1}}
  m_fontColorGradientPreset: {{fileID: 0}}
  m_spriteAsset: {{fileID: 0}}
  m_tintAllSprites: 0
  m_StyleSheet: {{fileID: 0}}
  m_TextStyleHashCode: -1183493901
  m_overrideHtmlColors: 0
  m_faceColor:
    serializedVersion: 2
    rgba: 4294967295
  m_fontSize: {font_max}
  m_fontSizeBase: 0
  m_fontWeight: 400
  m_enableAutoSizing: 1
  m_fontSizeMin: {font_min}
  m_fontSizeMax: {font_max}
  m_fontStyle: 0
  m_HorizontalAlignment: 2
  m_VerticalAlignment: 512
  m_textAlignment: 65535
  m_characterSpacing: 0
  m_characterHorizontalScale: 1
  m_wordSpacing: 0
  m_lineSpacing: 0
  m_lineSpacingMax: 0
  m_paragraphSpacing: 0
  m_charWidthMaxAdj: 0
  m_TextWrappingMode: 1
  m_wordWrappingRatios: 0
  m_overflowMode: 0
  m_linkedTextComponent: {{fileID: 0}}
  parentLinkedComponent: {{fileID: 0}}
  m_enableKerning: 0
  m_ActiveFontFeatures: 6e72656b
  m_enableExtraPadding: 0
  checkPaddingRequired: 0
  m_isRichText: 1
  m_EmojiFallbackSupport: 1
  m_parseCtrlCharacters: 1
  m_isOrthographic: 1
  m_isCullingEnabled: 0
  m_horizontalMapping: 0
  m_verticalMapping: 0
  m_uvLineOffset: 0
  m_geometrySortingOrder: 0
  m_IsTextObjectScaleStatic: 0
  m_VertexBufferAutoSizeReduction: 0
  m_useMaxVisibleDescender: 1
  m_pageToDisplay: 1
  m_margin: {{x: 0, y: 0, z: 0, w: 0}}
  m_isUsingLegacyAnimationComponent: 0
  m_isVolumetricText: 0
  m_hasFontAssetChanged: 0
  m_baseMaterial: {{fileID: 0}}
  m_maskOffset: {{x: 0, y: 0, z: 0, w: 0}}""")


def layout_group(comp_id, go_id, guid, class_id, padding, spacing, child_alignment,
                 force_expand_width, force_expand_height):
    emit(behaviour_header(comp_id, go_id, guid, class_id) + f"""  m_Padding:
    m_Left: {padding}
    m_Right: {padding}
    m_Top: {padding}
    m_Bottom: {padding}
  m_ChildAlignment: {child_alignment}
  m_Spacing: {spacing}
  m_ChildForceExpandWidth: {force_expand_width}
  m_ChildForceExpandHeight: {force_expand_height}
  m_ChildControlWidth: 1
  m_ChildControlHeight: 1
  m_ChildScaleWidth: 0
  m_ChildScaleHeight: 0
  m_ReverseArrangement: 0""")


def layout_element(comp_id, go_id, preferred_height=-1, flexible_width=-1):
    emit(behaviour_header(comp_id, go_id, LAYOUT_ELEMENT, "UnityEngine.UI::UnityEngine.UI.LayoutElement") + f"""  m_IgnoreLayout: 0
  m_MinWidth: -1
  m_MinHeight: -1
  m_PreferredWidth: -1
  m_PreferredHeight: {preferred_height}
  m_FlexibleWidth: {flexible_width}
  m_FlexibleHeight: -1
  m_LayoutPriority: 1""")


# ---- building the hierarchy ---------------------------------------------------------------

root_go = new_id()
root_rt = new_id()
root_cg = new_id()
root_vlg = new_id()
root_script = new_id()

children_of_root = []   # rect transform ids, in order
buttons = {}            # name -> button component id
display_tmp = None


def make_display():
    global display_tmp
    go, rt, cr, img, le = (new_id() for _ in range(5))
    text_go, text_rt, text_cr, text_tmp = (new_id() for _ in range(4))
    game_object(go, "Display", [rt, cr, img, le])
    rect_transform(rt, go, root_rt, [text_rt], (0.5, 0.5), (0.5, 0.5), (0, 0), (100, 100))
    canvas_renderer(cr, go)
    image(img, go, (0.92, 0.92, 0.92))
    layout_element(le, go, preferred_height=DISPLAY_HEIGHT)

    game_object(text_go, "Value", [text_rt, text_cr, text_tmp])
    rect_transform(text_rt, text_go, rt, [], (0, 0), (1, 1), (0, 0), (-40, 0))
    canvas_renderer(text_cr, text_go)
    tmp_text(text_tmp, text_go, "?", 100, 40)
    display_tmp = text_tmp
    children_of_root.append(rt)


def make_key(name, label, row_rt, flexible_width=None):
    go, rt, cr, img, btn = (new_id() for _ in range(5))
    text_go, text_rt, text_cr, text_tmp = (new_id() for _ in range(4))
    comps = [rt, cr, img, btn]
    le = None
    if flexible_width is not None:
        le = new_id()
        comps.append(le)

    game_object(go, name, comps)
    rect_transform(rt, go, row_rt, [text_rt], (0.5, 0.5), (0.5, 0.5), (0, 0), (100, 100))
    canvas_renderer(cr, go)
    image(img, go, (1, 1, 1))
    button(btn, go, img)
    if le is not None:
        layout_element(le, go, flexible_width=flexible_width)

    game_object(text_go, "Text (TMP)", [text_rt, text_cr, text_tmp])
    rect_transform(text_rt, text_go, rt, [], (0, 0), (1, 1), (0, 0), (0, 0))
    canvas_renderer(text_cr, text_go)
    tmp_text(text_tmp, text_go, label, 60, 25)

    buttons[name] = btn
    return rt


def make_row(name, keys):
    """keys: list of (key name, label, flexible width or None)."""
    go, rt, hlg, le = (new_id() for _ in range(4))
    game_object(go, name, [rt, hlg, le])

    # The row's own rect transform is written after its keys exist, so collect them first.
    key_rts = []
    pending = len(out)
    for key_name, label, flexible in keys:
        key_rts.append(make_key(key_name, label, rt, flexible))

    key_blocks = out[pending:]
    del out[pending:]

    rect_transform(rt, go, root_rt, key_rts, (0.5, 0.5), (0.5, 0.5), (0, 0), (100, 100))
    layout_group(hlg, go, HLG, "UnityEngine.UI::UnityEngine.UI.HorizontalLayoutGroup",
                 padding=0, spacing=SPACING, child_alignment=4, force_expand_width=1, force_expand_height=1)
    layout_element(le, go, preferred_height=KEY_HEIGHT)
    out.extend(key_blocks)
    children_of_root.append(rt)


# Build the rows first (they write their blocks), then the root last so it knows its children.
pending_start = len(out)
make_display()
make_row("Row_Top", [("Key_Clear", "C", None), ("Key_Sign", "+/-", None), ("Key_Delete", "DEL", None)])
make_row("Row_789", [("Key_7", "7", None), ("Key_8", "8", None), ("Key_9", "9", None)])
make_row("Row_456", [("Key_4", "4", None), ("Key_5", "5", None), ("Key_6", "6", None)])
make_row("Row_123", [("Key_1", "1", None), ("Key_2", "2", None), ("Key_3", "3", None)])
make_row("Row_Bottom", [("Key_0", "0", 2), ("Key_Decimal", ".", 1)])
child_blocks = out[pending_start:]
del out[pending_start:]

emit("%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:")
game_object(root_go, PREFAB_NAME, [root_rt, root_cg, root_vlg, root_script])
rect_transform(root_rt, root_go, 0, children_of_root, (0, 0), (1, 1), (0, 0), (0, 0))
canvas_group(root_cg, root_go)
layout_group(root_vlg, root_go, VLG, "UnityEngine.UI::UnityEngine.UI.VerticalLayoutGroup",
             padding=PADDING, spacing=SPACING, child_alignment=1, force_expand_width=1, force_expand_height=0)

digits = "".join(f"  - {{fileID: {buttons['Key_' + str(i)]}}}\n" for i in range(10))
emit(behaviour_header(root_script, root_go, KEYPAD_SCRIPT_GUID,
                      "MathQuest.Runtime::MathQuest.Battle.NumberKeypadPanel")
     + f"""  digitButtons:
{digits}  signButton: {{fileID: {buttons['Key_Sign']}}}
  decimalButton: {{fileID: {buttons['Key_Decimal']}}}
  clearButton: {{fileID: {buttons['Key_Clear']}}}
  deleteButton: {{fileID: {buttons['Key_Delete']}}}
  display: {{fileID: {display_tmp}}}""")
out.extend(child_blocks)

# The TAG line above must come first in the file: emit() put it after nothing, so it already is.
text = "".join(out)

PREFAB_DIR.mkdir(parents=True, exist_ok=True)
prefab_path = PREFAB_DIR / f"{PREFAB_NAME}.prefab"
prefab_path.write_text(text, encoding="utf8", newline="\n")

meta_path = PREFAB_DIR / f"{PREFAB_NAME}.prefab.meta"
if not meta_path.exists():
    meta_path.write_text(
        "fileFormatVersion: 2\n"
        f"guid: {PREFAB_GUID}\n"
        "PrefabImporter:\n"
        "  externalObjects: {}\n"
        "  userData: \n"
        "  assetBundleName: \n"
        "  assetBundleVariant: \n",
        encoding="utf8", newline="\n")

print(f"wrote {prefab_path} ({text.count(chr(10))} lines)")
