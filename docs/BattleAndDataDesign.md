# Battle and Data Design (Proposal)

Status: **design only, nothing here is built yet.** Items marked **Open** need a decision from the developer or the client.

## Battle flow

The battle is turn-based and answer-driven. The player makes no attack/defend choices; answering is the action.

1. Optional **start dialogue** plays (the question is not shown yet).
2. The enemy picks a random question from the Act's pool and shows it.
3. The player answers using the input method that question requires.
4. **Correct:** optional *encourage* line, then the player attacks and the enemy loses 1 HP.
5. **Wrong:** optional *taunt* line, then the *solution* dialogue (always shown), then the enemy attacks and the player loses 1 HP.
6. Repeat from step 2 until a side reaches 0 HP.
7. Enemy at 0 HP: optional **end dialogue**, then rewards and the result screen. Player at 0 HP: the level must be replayed.

Current client direction: player and enemy each have 3 HP, so a fight is 3 to 5 questions. Optional dialogue lists that are empty are simply skipped.

State sketch: `StartDialogue -> ShowQuestion -> WaitForAnswer -> (Correct: Encourage -> PlayerAttack | Wrong: Taunt -> Solution -> EnemyAttack) -> CheckHP -> NextQuestion | Victory | Defeat`.

The battle never changes between Acts. Only the enemy, the question, the input method, and the HP rules come from data.

## Resources

"Resource" is the project's term for an authored data type (a ScriptableObject class). The pattern is borrowed from the KOM balancing model in simplified form: one class per type, one folder per type, numbered instances, and references between resources so shared values live in one place. Do not name a folder `Resources` inside `Assets`, because Unity treats that name specially.

| Resource | Holds |
|---|---|
| `ActResource` | Act number, island art, battle background, ordered levels |
| `LevelResource` | Act, number (1-10), boss flag, enemy, reference to combat rules, start and end dialogue |
| `EnemyResource` | sprite, encourage and taunt line pools, optional HP |
| `CombatRulesResource` | player HP, enemy HP; shared by many levels, bosses get their own |
| `QuestionResource` | Act, topic, difficulty, question text, answer, solution, optional per-question taunt |

Where dialogue lives:

| Dialogue | Lives on | Why |
|---|---|---|
| Start and end | the level | once per fight, not once per question |
| Encourage and taunt | the enemy, with an optional per-question override | enemies get personality and the research group does not write thousands of taunts |
| Solution | the question | supplied by the research group |

Naming example: `QuestionResource_Act1_1`.

Design choices:

- The answer is a field **inside** `QuestionResource`, not a separate asset. A separate answer asset would double the file count and is only worthwhile if answers were shared between questions, which they are not. **Open:** the developer may still prefer a separate `AnswerResource`.
- The Act is stored once, on the question. "Which input methods an Act allows" is an optional validation rule, not a data structure.
- Multiple choice stores the options and the index of the correct one, and the game shuffles the order at runtime.

## Answer input methods

The input method belongs to the question, not the Act. Each method is one UI prefab; the Battle scene reads the question's type, spawns the matching prefab, and receives the submitted answer. Every method uses an **exact comparison**, so no algebra parser is needed.

| Method | Tier | Used for | Checked by |
|---|---|---|---|
| Multiple choice (2-4 options) | 1 | all Acts | option index |
| Number keypad (custom on-screen keypad) | 1 | evaluating, remainders | number, plus alternate accepted answers |
| Fill in the blanks | 1 | expanding, factoring, division, binomial coefficients | each slot value |
| Tap to select inside an expression | 2 | components of expressions, like terms | selected token(s) |
| Tile builder | 2 | translating verbal phrases | tile sequence |

Tier 1 is built first; Tier 2 is added only if time allows. Rows already authored stay valid because every row carries a `type`.

Notes: the system keyboard covers about half the screen in landscape, so use a custom keypad. Every method needs a way to display exponents and fractions.

## Spreadsheet for the research group

Source of truth for questions is a spreadsheet, one tab per answer method, converted to `QuestionResource` assets by an importer (not built yet). Roughly 3,000 questions are planned (about 600 per Act), which is too many to enter by hand in the Inspector.

Shared columns on every tab:

| Column | Notes |
|---|---|
| ID | unique and never reused, e.g. `A1-0001` |
| Act | 1-5 |
| Topic | one of the Act's three topics |
| Difficulty | for example 1-3, and whether boss levels may use it |
| Question | text shown to the player |
| Solution | explanation delivered after a mistake |
| Taunt (optional) | per-question override |
| Status | draft, reviewed, approved |

Per-method columns:

| Tab | Extra columns |
|---|---|
| Multiple choice | Correct answer, Wrong 1, Wrong 2, Wrong 3 |
| Number keypad | Answer, Alternate answers |
| Fill in the blanks | Template with markers like `3(x + 4) = {1}x + {2}`, then Blank 1 to Blank 4 |
| Tap to select (Tier 2) | Expression tokens separated by `\|`, correct token(s) |
| Tile builder (Tier 2) | Tile pool including decoys, correct sequence |

## Open questions

- How is math typed in the sheet (for example `x^2`, `{a}/{b}`)? Needs a small written convention with examples.
- What counts as the same keypad answer (fraction vs decimal, negative sign format)?
- One fight per level, or several? About 600 questions per Act is far more than one fight per level would use.
- Do bosses get more HP, harder question types, or both?
- Target length of solution text on a landscape phone screen.
- Should a missed question be re-queued later in the Act? (Optional extra, not requested.)
- Final scope for the prototype Act and the acceptance checklist (December 2026 title defense).
- Separate `AnswerResource`, or the answer nested inside `QuestionResource`?
