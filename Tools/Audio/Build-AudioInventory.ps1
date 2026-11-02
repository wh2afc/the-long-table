<#
.SYNOPSIS
    Rebuilds docs/audio/audio-candidates.csv from the candidate list below.
    Reads the source library only (sizes, SHA-256, WAV measurements). Never copies, moves or edits audio.

    To add a candidate: add a row to $candidates and run this script again.
    Evidence values: "filename" (picked from the name and folder), "measured" (WAV data read by Measure-Wav.ps1),
    "auditioned" (someone listened). Nothing here has been auditioned yet.
#>
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$out = Join-Path $root 'docs\audio\audio-candidates.csv'

$L   = 'C:\Users\wh2af\OneDrive\Music\Loops'
$RH  = "$L\QUA_Pop Strings_UK_Complete\07 Rolling Hills 75bpm Amin"
$CH  = "$L\QUA_Pop Strings_UK_Complete\03 Coming Home 120bpm Gmaj"
$ST  = "$L\Sidetrax"
$B25 = "$L\Ultimate SFX for Games BT25"

$QUA = 'Q Up Arts EULA (Q Up Arts ReadMe.pdf in collection): royalty free, single user, non-transferable; games allowed; no standalone redistribution'
$SDT = 'Purchased via Humble Bundle; bundle source describes SideTrax as "royalty free" (user confirmed 2026-11-01); no licence file in archive; detailed terms not independently reviewed'
$UFX = 'Purchased by user; no licence file in archive; purchase terms not independently verified'

# Id, Role, Moment, Collection, Licence, Path, Why
$candidates = @(
    # --- Adaptive score: one tune, layers added as warmth rises ---
    @('mus-rh-vln1','music-layer','Whole day, base layer','Q Up Arts Pop Strings UK / Rolling Hills 75bpm Am',$QUA,"$RH\PSUK_75bpm_Rolling Hills_Violin 1 Section_Amin.wav",'The "single fiddle" the score starts from. Separate stem, same length as the others, so layers stay in sync.'),
    @('mus-rh-vln2','music-layer','Oven relit (the new instrument)','Q Up Arts Pop Strings UK / Rolling Hills 75bpm Am',$QUA,"$RH\PSUK_75bpm_Rolling Hills_Violin 2 Section_Amin.wav",'Fade in when Marta relights the oven: the doc''s "music gains a single new instrument".'),
    @('mus-rh-viola','music-layer','Warmth tier Thawing','Q Up Arts Pop Strings UK / Rolling Hills 75bpm Am',$QUA,"$RH\PSUK_75bpm_Rolling Hills_Viola Section_Amin.wav",'Middle voice for a warming village.'),
    @('mus-rh-cello','music-layer','Warmth tier Open / Gather','Q Up Arts Pop Strings UK / Rolling Hills 75bpm Am',$QUA,"$RH\PSUK_75bpm_Rolling Hills_Cello Section_Amin.wav",'Low warmth under the table scene when the village is open.'),
    @('mus-rh-piano','music-layer','Optional colour','Q Up Arts Pop Strings UK / Rolling Hills 75bpm Am',$QUA,"$RH\PSUK_75bpm_Rolling Hills_Piano_Amin.wav",'Possible extra layer. Piano may pull away from the folk palette; listen first.'),
    @('mus-rh-full','music-reference','Reference mix','Q Up Arts Pop Strings UK / Rolling Hills 75bpm Am',$QUA,"$RH\PSUK_75bpm_Rolling Hills_FULL_Amin.wav",'Full mix, to hear what all layers together sound like. Includes drums and synth stems not planned for the game.'),
    @('mus-ch-vln1','music-layer','Morning after (warm alternative)','Q Up Arts Pop Strings UK / Coming Home 120bpm G',$QUA,"$CH\PSUK_120bpm_Coming Home_Violin 1 Section_Gmaj.wav",'Major key and the title fits the payoff. Sparser than Rolling Hills (silent about a quarter of the time).'),
    @('mus-ch-vln2','music-layer','Morning after (warm alternative)','Q Up Arts Pop Strings UK / Coming Home 120bpm G',$QUA,"$CH\PSUK_120bpm_Coming Home_Violin 2 Section_Gmaj.wav",'Second violin layer for the same idea.'),
    @('mus-ch-cello','music-layer','Morning after (warm alternative)','Q Up Arts Pop Strings UK / Coming Home 120bpm G',$QUA,"$CH\PSUK_120bpm_Coming Home_Cello Section_Gmaj.wav",'Low layer for the same idea.'),
    @('mus-ch-full','music-reference','Reference mix','Q Up Arts Pop Strings UK / Coming Home 120bpm G',$QUA,"$CH\PSUK_120bpm_Coming Home_FULL_Gmaj.wav",'Full mix for reference.'),

    # --- Whole tracks (no stems): title, menus, alternatives ---
    @('mus-st-emptychair','music-track','Title screen / end of slice','SideTrax 10 Themes',$SDT,"$ST\musicpack10_themes\10 Themes\WAV\Empty chair.wav",'Title matches the game''s central image. Single mix, not stems.'),
    @('mus-st-pastoral','music-track','Morning (alternative)','SideTrax 95 Pastoral',$SDT,"$ST\75pastoralmusic\95 Pastoral\WAV\Pastoral.wav",'Collection and title suggest pastoral. 24-bit master.'),
    @('mus-st-remember','music-track','Morning after, cold or polite','SideTrax 95 Pastoral',$SDT,"$ST\75pastoralmusic\95 Pastoral\WAV\Rememberances.wav",'Title suggests something wistful for a village that did not open up.'),
    @('mus-st-flames','music-track','Dusk / oven','SideTrax 102 Placid 03',$SDT,"$ST\82placid_issue03\102 Placid 03\WAV\Flickering flames.wav",'Placid collection; title ties to the oven and lanterns.'),
    @('mus-st-duet','music-track','Gather at the table','SideTrax 102 Placid 03',$SDT,"$ST\82placid_issue03\102 Placid 03\WAV\Guitar duet in E major.wav",'Two players together, major key: people at one table.'),
    @('mus-st-morning','music-track','Morning, under dialogue','SideTrax 48 Fretboard',$SDT,"$ST\pack48_fretboard\48 Fretboard\WAV\Morning.wav",'Guitar collection. Measures about 15 dB quieter than the others, so it may sit well under text.'),
    @('mus-st-dawn66','music-track','Morning (alternative)','SideTrax 66 Acoustic pack',$SDT,"$ST\pack06_acousticmusic\66 Acoustic pack\WAV\Dawn.wav",'Acoustic collection, morning title.'),
    @('mus-st-dawn86','music-track','Morning (alternative)','SideTrax 86 Acoustic 02',$SDT,"$ST\66acousticmusic_issue02\86 Acoustic 02\WAV\Dawn.wav",'Second acoustic "Dawn"; a different track from the one above.'),
    @('mus-st-ukejig','music-track','Village life, light moments','SideTrax 13 Country dreams',$SDT,"$ST\musicpack13_countrydreams\13 Country dreams\WAV\Uke jig.wav",'A jig is folk-adjacent. Loud master; may be too lively.'),

    # --- Interface ---
    @('ui-advance','sfx-ui','Continue dialogue','Ultimate SFX BT25 / Ui Clicks',$UFX,"$B25\Ui Clicks\FL_GFX_UI_Click_02.wav",'Short click (0.5 s). One of 30; pick the softest-sounding by ear.'),
    @('ui-select','sfx-ui','Pick an option','Ultimate SFX BT25 / Ui Clicks',$UFX,"$B25\Ui Clicks\FL_GFX_UI_Click_01.wav",'Quieter sibling of the advance click.'),
    @('ui-menu-open','sfx-ui','Options menu opens','Ultimate SFX BT25 / Cozy Mechanics',$UFX,"$B25\Cozy Mechanics\FL_GFX_Cozy_Mechanic_Inventory_Open.wav",'From the "cozy" set, so likely softer than generic UI. Same file exists in BT5.'),
    @('ui-menu-close','sfx-ui','Options menu closes','Ultimate SFX BT25 / Cozy Mechanics',$UFX,"$B25\Cozy Mechanics\FL_GFX_Cozy_Mechanic_Inventory_Close.wav",'Pair for the above.'),
    @('ui-notice','sfx-ui','You noticed someone','Ultimate SFX BT25 / Notification SFX',$UFX,"$B25\Notification SFX\FL_GFX_Notification_Sfx_02.wav",'Quietest of the three notifications measured. Should feel like a small realisation, not a quest alert.'),
    @('ui-phase','sfx-transition','Morning to midday, midday to dusk','Ultimate SFX BT25 / Dreamy Whooshes',$UFX,"$B25\Dreamy Whooshes\FL_GFX_Motion_Dreamy_Whoosh_02.wav",'2 s soft transition.'),
    @('ui-night','sfx-transition','Night passes (fade to black)','Ultimate SFX BT25 / Dreamy Whooshes',$UFX,"$B25\Dreamy Whooshes\FL_GFX_Motion_Dreamy_Whoosh_01.wav",'8 s, long enough to cover the fade.'),

    # --- Actions and world ---
    @('sfx-satchel','sfx-foley','Taking bread from the satchel','Ultimate SFX BT25 / Clothing, Fabric & Materials',$UFX,"$B25\Clothing, Fabric & Materials\FL_GFX_Clothing_Fabric_Material_Foley_Bag_Open.wav",'Bag opening for the player''s satchel.'),
    @('sfx-bread-cloth','sfx-foley','Bread left for Pip in a cloth','Ultimate SFX BT25 / Clothing, Fabric & Materials',$UFX,"$B25\Clothing, Fabric & Materials\FL_GFX_Clothing_Fabric_Material_Foley_Drop_Material_01.wav",'Soft cloth set down. Quiet on purpose: nobody should hear it.'),
    @('sfx-scrap','sfx-foley','Sorting Tobias''s scrap pile','Ultimate SFX BT25 / Cozy Mechanics',$UFX,"$B25\Cozy Mechanics\FL_GFX_Cozy_Mechanic_Dig_Stack_Drop_01.wav",'Stack-and-drop; five variants (_01 to _05) to avoid repetition. Not metallic; check by ear.'),
    @('sfx-flue','sfx-foley','Clearing the oven flue','Ultimate SFX BT25 / Kitchen, Cooking',$UFX,"$B25\Kitchen, Cooking\FL_GFX_Home_Fx_Kitchen_Cooking_Rub_01.wav",'Scrubbing texture. Filename guess only.'),
    @('sfx-door-closed','sfx-foley','Polite morning: "Not hard. Just closed."','Ultimate SFX BT25 / Doors',$UFX,"$B25\Doors\FL_GFX_Home_Fx_Door_Soft_01.wav",'Soft door close; the line asks for exactly that.'),
    @('sfx-lantern','sfx-foley','Lanterns lit at dusk','Ultimate SFX BT25 / Foley',$UFX,"$B25\Foley\FL_GFX_Home_Foley_Lighter_01.wav",'A modern lighter may sound wrong for the setting; listen first.'),
    @('sfx-plates','sfx-foley','Eating at the table','Ultimate SFX BT25 / Foley',$UFX,"$B25\Foley\FL_GFX_Home_Foley_Plates.wav",'Table ambience during Gather.'),
    @('sfx-spoon','sfx-foley','Eating at the table','Ultimate SFX BT25 / Foley',$UFX,"$B25\Foley\FL_GFX_Home_Foley_Spoon.wav",'Small detail sound for the meal.')
)

$measure = Join-Path $PSScriptRoot 'Measure-Wav.ps1'
$rows = foreach ($c in $candidates) {
    $id, $role, $moment, $collection, $licence, $path, $why = $c
    $exists = Test-Path -LiteralPath $path
    $m = if ($exists) { & $measure -Path $path } else { $null }
    [pscustomobject]@{
        Id            = $id
        Role          = $role
        Moment        = $moment
        Collection    = $collection
        File          = Split-Path -Leaf $path
        FullPath      = $path
        Bytes         = if ($exists) { (Get-Item -LiteralPath $path).Length } else { '' }
        SHA256        = if ($exists) { (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash } else { 'MISSING' }
        Seconds       = $m.Seconds
        Format        = $m.Format
        PeakDb        = $m.PeakDb
        RmsDb         = $m.RmsDb
        ActivePct     = $m.ActivePct
        Evidence      = 'filename + measured'
        Status        = 'candidate (not imported)'
        LicenceStatus = $licence
        Why           = $why
    }
}

New-Item -ItemType Directory -Force (Split-Path $out) | Out-Null
$rows | Export-Csv -LiteralPath $out -NoTypeInformation -Encoding UTF8
"Wrote $($rows.Count) candidates to $out ($(@($rows | Where-Object SHA256 -eq 'MISSING').Count) missing)"
