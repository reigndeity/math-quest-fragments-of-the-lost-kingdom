using System;
using System.Collections.Generic;
using UnityEngine;

namespace MathQuest.Data
{
    /// <summary>One piece the player can place, for example a fraction chunk or an operator.</summary>
    [Serializable]
    public sealed class ExpressionTile
    {
        [Tooltip("Stable identifier used by slots and arrangements, for example frac or plus.")]
        public string id;

        [Tooltip("What the tile shows, in TEXDraw syntax, for example $+$ or $\\frac{a}{b}$.")]
        public string math;
    }

    /// <summary>One position in the expression being built.</summary>
    [Serializable]
    public sealed class ExpressionSlot
    {
        [Tooltip("Leave empty for the player to fill. Otherwise the id of the tile locked into this slot.")]
        public string prefilledTileId;
    }

    /// <summary>One accepted solution: a tile id for every slot, in slot order.</summary>
    [Serializable]
    public sealed class ExpressionArrangement
    {
        [Tooltip("Tile id for every slot in order, including prefilled slots.")]
        public List<string> tileIds = new();
    }

    /// <summary>
    /// Answer data for a question answered by placing tiles into slots. Tiles can be
    /// operators or whole expression chunks; decoys are tiles no arrangement uses.
    /// </summary>
    [CreateAssetMenu(
        fileName = "AnswerResource_Act1_3",
        menuName = "Math Quest/Resources/Expression Builder Answer Resource")]
    public sealed class ExpressionBuilderAnswerResource : AnswerResource
    {
        [SerializeField] private List<ExpressionTile> tiles = new();
        [SerializeField] private List<ExpressionSlot> slots = new();

        [Tooltip("Every arrangement that counts as correct, for example both orders of an addition.")]
        [SerializeField] private List<ExpressionArrangement> acceptedArrangements = new();

        public override AnswerMethod Method => AnswerMethod.ExpressionBuilder;
        public IReadOnlyList<ExpressionTile> Tiles => tiles;
        public IReadOnlyList<ExpressionSlot> Slots => slots;

        public bool TryGetTile(string tileId, out ExpressionTile tile)
        {
            foreach (ExpressionTile candidate in tiles)
            {
                if (candidate != null && candidate.id == tileId)
                {
                    tile = candidate;
                    return true;
                }
            }

            tile = null;
            return false;
        }

        /// <summary>True when the placed tile ids (one per slot, in order) match an accepted arrangement.</summary>
        public bool IsCorrect(IReadOnlyList<string> placedTileIds)
        {
            if (placedTileIds.Count != slots.Count)
                return false;

            foreach (ExpressionArrangement arrangement in acceptedArrangements)
            {
                if (arrangement.tileIds.Count != placedTileIds.Count)
                    continue;

                bool matches = true;
                for (int i = 0; i < placedTileIds.Count; i++)
                {
                    if (arrangement.tileIds[i] != placedTileIds[i])
                    {
                        matches = false;
                        break;
                    }
                }

                if (matches)
                    return true;
            }

            return false;
        }
    }
}
