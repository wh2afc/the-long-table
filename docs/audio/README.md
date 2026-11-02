# Audio notes

Audio for The Long Table comes from the user's own purchased library first. Nothing has been imported
into Unity yet; every file below is a **candidate**, recorded in [`audio-candidates.csv`](audio-candidates.csv)
with its full path, size, SHA-256, measurements and licence status.

Last updated: 2026-11-01.

## Rules for this project

- Prefer the existing library at `C:\Users\wh2af\OneDrive\Music\Loops` for music, ambience, loops, UI,
  impacts and transitions.
- Leave the library exactly as it is: no moving, renaming or editing, and keep every licence and readme file.
- Import into Unity only when the user asks. On import: copy only the chosen files, keep the originals, and
  record the source collection and licence information here and in the CSV.
- Don't reuse audio from installed software, games, commercial albums or audiobooks.
- Say how each pick was judged:
  - **filename**: chosen from the file and folder name.
  - **measured**: WAV data read with [`Tools/Audio/Measure-Wav.ps1`](../../Tools/Audio/Measure-Wav.ps1)
    (length, format, peak and RMS level, share of one-second windows above −40 dBFS).
  - **auditioned**: someone listened to it.

  **No file has been auditioned yet.** Everything is filename + measured.

## Sources and licences

| Source | What it is | Licence status |
|---|---|---|
| Q Up Arts (Pop Strings UK and others) | Loop and stem packs | EULA in `Q Up Arts ReadMe.pdf` (read 2026-11-01): royalty free, single user, non-transferable, lifetime. Games are named as an allowed use. Not allowed: claiming the music as your own, selling it in a store, or redistributing it except inside the project. |
| SideTrax (74 collections, 2,314 audio files) | Finished music tracks, as WAV, MP3 and AAC | Purchased through Humble Bundle, whose source describes the files as "royalty free" (user confirmed 2026-11-01). No licence, readme or terms file in the archives. Detailed terms not independently reviewed. |
| Ultimate SFX for Games BT5 / BT15 / BT25 | Sound effects and music kits | Purchased by the user. No licence file in the archives. Purchase terms not independently verified. BT5's Cozy Mechanics files are byte-identical to BT25's; the CSV cites the BT25 copy. |

The SideTrax file manifest, with archive-relative paths, sizes and SHA-256 hashes, is kept with the A Hoard
of Moments project (`audio-sidetrax-files.csv`). All nine SideTrax candidates here match that manifest.

## The plan for the slice

### Music: one tune that grows

The design asks for a folk score that "begins with a single fiddle" and, when Marta's oven relights,
"gains a single new instrument". Q Up Arts **Pop Strings UK** ships each tune as separate parts
(stems), each part's files the same length and recorded to a click, so the parts can play together
and be faded in and out:

| Layer | When it plays | File |
|---|---|---|
| Violin 1 | Always (the single fiddle) | `Rolling Hills_Violin 1 Section_Amin.wav` |
| Viola | Village reaches Thawing | `Rolling Hills_Viola Section_Amin.wav` |
| Cello | Village reaches Open, and at Gather | `Rolling Hills_Cello Section_Amin.wav` |
| Violin 2 | Marta's oven relights (the new instrument) | `Rolling Hills_Violin 2 Section_Amin.wav` |

All four measure 87.2 s, 44.1 kHz/16-bit stereo, around −20 to −23 dB RMS, active about 92% of the time.
Rolling Hills is 75 bpm in A minor: slow and a little wistful, which suits a village that starts out muted.
**Coming Home** (120 bpm, G major) has the same kind of stems and is a warmer alternative for the morning
after; its string parts are silent about a quarter of the time.

To check before building on it: the tempo, the key and how "folk" the string sections sound. Each folder
also has `loops\` section files for tighter looping.

### Whole tracks (no stems)

For the title screen, menus, or if the stem plan doesn't feel right:

- **Empty chair** (SideTrax Themes, 157 s): the title matches the game's central image.
- **Guitar duet in E major** (Placid 03): two players together, for the table scene.
- **Flickering flames** (Placid 03): dusk and the oven.
- **Morning** (Fretboard): measures about 15 dB quieter than the rest, so it may sit well under dialogue.
- **Pastoral**, **Rememberances** (95 Pastoral), two different tracks called **Dawn** (Acoustic packs),
  **Uke jig** (Country dreams): alternatives, listed in the CSV with reasons.

### Sound effects

| Moment | Candidate | Note |
|---|---|---|
| Continue dialogue / pick an option | BT25 Ui Clicks `_02` / `_01` | 30 variants to choose from by ear |
| Options menu opens and closes | Cozy Mechanics `Inventory_Open` / `_Close` | From the "cozy" set |
| You noticed someone | Notification SFX `_02` | Quietest of three measured. Should feel like a small realisation, not a quest alert. |
| Phase change / night passes | Dreamy Whooshes `_02` (2 s) / `_01` (8 s) | `_01` is long enough to cover the fade |
| Bread from the satchel, left in a cloth | Clothing `Bag_Open`, `Drop_Material_01` | Quiet on purpose |
| Sorting Tobias's scrap | Cozy Mechanics `Dig_Stack_Drop_01`–`05` | Not metallic; check |
| Clearing the flue | Kitchen `Rub_01` | Filename guess |
| "The door closes. Not hard. Just closed." | Doors `Door_Soft_01` | |
| Lanterns lit | Foley `Lighter_01` | A modern lighter may sound wrong |
| Eating at the table | Foley `Plates`, `Spoon` | |

## Gaps: nothing suitable found in the library

A keyword search of all 5,592 audio files found nothing usable for:

- **Nature ambience**: morning birds, dusk crickets, wind over the valley.
- **Fire**: oven crackle, forge coals.
- **The forge**: anvil and hammer.
- **Footsteps** on mud and grass.
- **Village murmur** at the table.
- **Coins** that suit the setting (only 8-bit coins exist).

These need another source, such as a field-recording pack, a commission, or recording them yourself.

## Imported into Unity

None yet.
