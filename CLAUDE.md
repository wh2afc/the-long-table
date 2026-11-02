# The Long Table: notes for Claude

Unity 6000.6.3f1, C#. See README.md for the architecture and how to run tests.

## Audio

Full rules, licence status and current picks: [docs/audio/README.md](docs/audio/README.md).
The inventory is [docs/audio/audio-candidates.csv](docs/audio/audio-candidates.csv), rebuilt by
`Tools/Audio/Build-AudioInventory.ps1`.

- Choose audio from the purchased library at `C:\Users\wh2af\OneDrive\Music\Loops` first
  (Q Up Arts, Ultimate SFX for Games BT5/BT15/BT25, SideTrax).
- Before choosing, read the library notes in `C:\Projects\A Hoard of Moments\`:
  `audio-library-search.md`, `audio-search-candidates.txt`, `audio-search-inventory.txt`,
  `audio-sidetrax-inventory.csv`, and `audio-sidetrax-files.csv` (per-file paths, sizes, SHA-256).
- SideTrax: purchased via Humble Bundle, described by the bundle source as "royalty free" (user confirmed).
  Detailed licence terms not independently reviewed.
- Recommend specific files with full paths and reasons. Say whether each pick is filename-based,
  measured, or auditioned.
- Never modify, move or rename the library; keep its licence and readme files.
- Don't reuse audio from installed software, games, commercial albums or audiobooks.
- Don't import audio into Unity unless the user asks. On import: copy only the chosen files, keep the
  originals, and record the source collection and licence in docs/audio.
- Keep docs/audio current whenever picks change or files are imported.
