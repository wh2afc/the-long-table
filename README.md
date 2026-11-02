# The Long Table

A cozy cooperative 2D village game about belonging. Unity (C#), PC first.

This repository currently holds the **first vertical slice**: one day on the village green of
Harrow's Rest with Marta, Pip and Tobias. The question the slice answers is whether a player can
feel the difference between sincere and transactional play.

## Running the slice

1. Open the folder in **Unity 6000.6.3f1** (see "Unity version" below).
2. Open `Assets/TheLongTable/Scenes/VillageGreen.unity` and press Play.
   If the scene is missing, use **The Long Table → Rebuild Vertical Slice Scene**.

| Key | Does |
| --- | --- |
| A / D or arrows | Walk along the green |
| E | Notice someone (morning) or spend time with them (midday) |
| 1–9 | Pick an option |
| Space / Enter | Continue dialogue |
| N | Move the day on (morning to midday, midday to dusk) |
| Tab | Show the event log the motive system reads |
| F1 | Hide or show the warmth readout |
| F5 / F6 | Watch a scripted sincere / transactional day |
| R | Live the day again (on the debrief screen) |

A built player also accepts `-tltAutoplay sincere` or `-tltAutoplay transactional`.

## What one day looks like

1. **Notice** (morning): walk up to people and look. Noticing is free, but some options only
   exist if you noticed first. There's no quest marker for Pip.
2. **Act** (midday): 4 attention to spend on Give, Mend, Build or Listen. Tobias turns you away
   the first time. You can come back.
3. **Gather** (dusk): who sits close, who sits at the edge and who stays away comes from the
   motive evaluator. You can tell the table what you did today, or just eat and listen.
4. **The morning after**: Marta's oven is relit, lit for an order, or still cold. Then a
   playtest debrief shows what the evaluator saw.

## How it's built

```
Assets/TheLongTable/
  Scripts/Core/      Pure rules, no scene code (LongTable.Core)
    EventLog         Append-only records: who, what, cost, witnesses, when. Versioned JSON.
    MotiveEvaluator  Rule-based, deterministic: signals -> sincerity -> warmth, trust, seats, oven
    DayLoop          Notice -> Act -> Gather -> (Feast at season end) -> DayEnd, attention budget
    SliceSession     Resources, what is allowed when, writes the log
    Dialogue         Scripted lines picked by context and conditions, each with a stable id
    ScriptedPlays    The two reference days used by tests and autoplay
  Scripts/Game/      Unity side (LongTable.Game)
    SliceBootstrap   The only component in the scene; builds everything else
    SliceDirector    Runs the day; input, autoplay and tests use the same methods
    VillageBuilder   The green, layer by layer, from placeholder shapes
    WorldLook        Time of day + warmth -> sky, saturation, ambient, window and lantern glow
    ParallaxLayer    Camera-relative layer offsets
  Resources/Data/    All tuning and writing, as JSON
    motive_rules.json   signal weights, tiers, act penalties, oven and seat thresholds
    actions.json        what you can do, costs, visibility, rebuffs
    villagers.json      who can repay, who is overlooked, positions, colours
    dialogue.json       every line, keyed for future localization
    slice_config.json   attention, starting bread and coin, look range
  Tests/EditMode     31 tests: evaluator, day loop, session rules, data validation, save round trip
  Tests/PlayMode     Plays both reference days in the real scene, checks the screen differs
```

### The motive evaluator

It reads only the event log. Six signals, each scored 0 (transactional) to 1 (sincere):

| Signal | Reads |
| --- | --- |
| `private_help` | share of acts nobody witnessed |
| `no_reminding` | acts you didn't point out afterwards |
| `cannot_repay` | acts for people who can't repay or are overlooked |
| `stays_through_rebuff` | coming back after being turned away |
| `no_reward_asked` | acts that asked nothing back |
| `noticed_first` | acts that followed noticing that person |

Sincerity is the weighted mean of the signals that have evidence. Warmth is starting warmth plus
each act's base warmth scaled by sincerity, minus a penalty per reminder. Trust per villager also
judges each act: a loud rescue costs the overlooked their pride, paying for something counts for
little, and coming back after a rebuff counts for more.

With the shipped numbers the two reference days, each with four acts, come out as:

| | Sincere | Transactional |
| --- | --- | --- |
| Sincerity | 0.96 | 0.28 |
| Warmth | 44.5 (Open) | 20.1 (Guarded) |
| At the table | all three close | Marta at the edge, Pip and Tobias stay away |
| Marta's oven | Relit | Polite |

## Tests

From the Unity Test Runner, or in batch mode:

```
Unity -batchmode -projectPath . -runTests -testPlatform EditMode -testResults TestResults/editmode.xml
Unity -batchmode -projectPath . -runTests -testPlatform PlayMode -testResults TestResults/playmode.xml
```

The PlayMode test needs a graphics device (don't pass `-nographics`). It saves frames of both days
to `Screenshots/`, which git ignores.

## Choices made for the slice (worth revisiting)

- **Placeholder art is generated in code.** It's flat shapes tinted by `WorldLook`, so there are no
  binary assets yet. Painted layers replace the shapes later; the systems don't change.
- **Lighting uses sprite tinting on the built-in renderer, not URP 2D lights.** The design doc names
  URP 2D lights. Tinting was quicker to make reliable for the slice. `WorldLook` is the single place
  to swap.
- **Dialogue is in JSON, not Yarn Spinner or Ink.** The doc says to check those tools' Unity support
  and licences first. Line ids are stable, so lines can move into a dialogue tool or the Unity
  Localization package later.
- **No audio yet.** The growing folk score, including the new instrument when the oven relights,
  is not in the slice. Candidates from the purchased audio library are chosen but not imported; see
  [docs/audio](docs/audio/README.md).
- **The warmth number is on screen** because the slice brief asks for it. The design keeps it
  hidden in the shipped game (F1 hides it).

## Unity version

Built and tested on **6000.6.3f1**, the newest editor installed on the dev machine. The design doc
says to stay on one LTS release for the whole production, so confirm whether this version is LTS and
choose the production version before anything big gets built on it.

## Repository

- Unity `.gitignore` (Library, Temp, Logs, UserSettings, builds and generated project files ignored).
- Git LFS for art, audio, video and fonts (see `.gitattributes`). Run `git lfs install` once per machine.
- Unity YAML files are set to merge with `unityyamlmerge`. To use it, point git at Unity's
  `UnityYAMLMerge` tool in your git config.

## Still open

- Voice approach
- Language list
- Unity licence tier, to recheck before launch
"# the-long-table" 
