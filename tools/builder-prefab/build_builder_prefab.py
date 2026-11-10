"""
Generates Prefabs/ExpressionBuilderHolder.prefab: the expression builder layout.

Run:  python tools/builder-prefab/build_builder_prefab.py

The output is an ordinary Unity prefab; open it in Unity to restyle anything. The slots and the
tiles are not in the prefab: ExpressionBuilderPanel spawns them at runtime from the existing
ExpressionSlot and ExpressionTile prefabs, so restyle those two to restyle the pieces.

Layout (all sizes in canvas units):
    SlotContainer   a row of slots, one per slot of the answer         height SLOT_ROW_HEIGHT
    TileContainer   the pool of tiles in a grid, on a light background  (drop a tile here to take it back)
    DragLayer       covers everything and is ignored by the layout; the dragged tile lives here
"""

from pathlib import Path

PREFAB_DIR = Path(__file__).resolve().parents[2] / "Math Quest/Assets/Math Quest/Prefabs"
PREFAB_NAME = "ExpressionBuilderHolder"
PREFAB_GUID = "19eaa27a5372428d9a5ef71dc8f837ef"

# ExpressionBuilderPanel.cs script guid (Scripts/Battle/ExpressionBuilderPanel.cs.meta)
PANEL_SCRIPT_GUID = "1ffe2558704a4eb7afecb4e67e2c3a5e"

# Existing prefabs the panel spawns pieces from: component id inside the prefab, and its guid.
SLOT_PREFAB = "{fileID: 6200000010, guid: 876cadda11c746f3b2edac0f9389024f, type: 3}"
TILE_PREFAB = "{fileID: 6100000010, guid: 6fb7f21d818347de946dfa222008818c, type: 3}"

# ---- layout numbers: change these and rerun ----------------------------------------------
PADDING = 20            # space around everything inside the holder
SPACING = 20            # space between the slot row and the pool
SLOT_SPACING = 15       # space between slots
SLOT_ROW_HEIGHT = 160
POOL_PADDING = 15
TILE_CELL = (260, 130)  # size of one tile cell in the pool
TILE_SPACING = (15, 15)
TILE_COLUMNS = 3

# ---- shared look, copied from the OptionChoice prefab --------------------------------------
SPRITE = "{fileID: 21300000, guid: c8d2f0157fb1de94b87fd1a11aa1374e, type: 3}"
PPU_MULTIPLIER = 0.14

IMAGE = "fe87c0e1cc204ed48ad3b37840f39efc"
VLG = "59f8146938fff824cb5fd77236b75775"
HLG = "30649d3a9faa99c48a7b1166b86bf2a0"
GRID = "8a8695521f0d02e499659fee002a26c2"
LAYOUT_ELEMENT = "306cc8c2b49d7114eaa3623786fc2126"

_next_id = 5200000000000000000


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


def grid_layout_group(comp_id, go_id, padding, cell, spacing, columns):
    emit(behaviour_header(comp_id, go_id, GRID, "UnityEngine.UI::UnityEngine.UI.GridLayoutGroup") + f"""  m_Padding:
    m_Left: {padding}
    m_Right: {padding}
    m_Top: {padding}
    m_Bottom: {padding}
  m_ChildAlignment: 1
  m_StartCorner: 0
  m_StartAxis: 0
  m_CellSize: {{x: {cell[0]}, y: {cell[1]}}}
  m_Spacing: {{x: {spacing[0]}, y: {spacing[1]}}}
  m_Constraint: 1
  m_ConstraintCount: {columns}""")


def layout_element(comp_id, go_id, preferred_height=-1, ignore_layout=0):
    emit(behaviour_header(comp_id, go_id, LAYOUT_ELEMENT, "UnityEngine.UI::UnityEngine.UI.LayoutElement") + f"""  m_IgnoreLayout: {ignore_layout}
  m_MinWidth: -1
  m_MinHeight: -1
  m_PreferredWidth: -1
  m_PreferredHeight: {preferred_height}
  m_FlexibleWidth: -1
  m_FlexibleHeight: -1
  m_LayoutPriority: 1""")


# ---- hierarchy ------------------------------------------------------------------------------

root_go, root_rt, root_cg, root_vlg, root_script = (new_id() for _ in range(5))

slot_go, slot_rt, slot_hlg, slot_le = (new_id() for _ in range(4))
tile_go, tile_rt, tile_cr, tile_img, tile_grid = (new_id() for _ in range(5))
drag_go, drag_rt, drag_le = (new_id() for _ in range(3))

emit("%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:")

game_object(root_go, PREFAB_NAME, [root_rt, root_cg, root_vlg, root_script])
rect_transform(root_rt, root_go, 0, [slot_rt, tile_rt, drag_rt], (0, 0), (1, 1), (0, 0), (0, 0))
canvas_group(root_cg, root_go)
layout_group(root_vlg, root_go, VLG, "UnityEngine.UI::UnityEngine.UI.VerticalLayoutGroup",
             padding=PADDING, spacing=SPACING, child_alignment=1, force_expand_width=1, force_expand_height=0)
emit(behaviour_header(root_script, root_go, PANEL_SCRIPT_GUID,
                      "MathQuest.Runtime::MathQuest.Battle.ExpressionBuilderPanel") + f"""  slotContainer: {{fileID: {slot_rt}}}
  tileContainer: {{fileID: {tile_rt}}}
  slotPrefab: {SLOT_PREFAB}
  tilePrefab: {TILE_PREFAB}
  dragLayer: {{fileID: {drag_rt}}}
  ghostOffset: {{x: 0, y: 90}}
  emptySlotColor: {{r: 0.78, g: 0.78, b: 0.78, a: 1}}
  filledSlotColor: {{r: 1, g: 1, b: 1, a: 1}}
  lockedSlotColor: {{r: 0.7, g: 0.85, b: 1, a: 1}}
  hoverSlotColor: {{r: 0.7, g: 1, b: 0.7, a: 1}}""")

# the row of slots
game_object(slot_go, "SlotContainer", [slot_rt, slot_hlg, slot_le])
rect_transform(slot_rt, slot_go, root_rt, [], (0.5, 0.5), (0.5, 0.5), (0, 0), (100, 100))
layout_group(slot_hlg, slot_go, HLG, "UnityEngine.UI::UnityEngine.UI.HorizontalLayoutGroup",
             padding=0, spacing=SLOT_SPACING, child_alignment=4, force_expand_width=1, force_expand_height=1)
layout_element(slot_le, slot_go, preferred_height=SLOT_ROW_HEIGHT)

# the pool of tiles; its background is also where a tile is dropped to take it back
game_object(tile_go, "TileContainer", [tile_rt, tile_cr, tile_img, tile_grid])
rect_transform(tile_rt, tile_go, root_rt, [], (0.5, 0.5), (0.5, 0.5), (0, 0), (100, 100))
canvas_renderer(tile_cr, tile_go)
image(tile_img, tile_go, (0.86, 0.86, 0.9))
grid_layout_group(tile_grid, tile_go, POOL_PADDING, TILE_CELL, TILE_SPACING, TILE_COLUMNS)

# the layer the dragged tile moves in: covers the panel, ignored by the layout
game_object(drag_go, "DragLayer", [drag_rt, drag_le])
rect_transform(drag_rt, drag_go, root_rt, [], (0, 0), (1, 1), (0, 0), (0, 0))
layout_element(drag_le, drag_go, ignore_layout=1)

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
