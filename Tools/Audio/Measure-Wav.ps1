<#
.SYNOPSIS
    Reads WAV files and reports format, length, loudness and how much of the file is active.
    Read-only: it never writes to or moves the source audio. This measures audio; it is not a listening test.

.EXAMPLE
    .\Measure-Wav.ps1 -Path "C:\...\track.wav","C:\...\other.wav" | Format-Table
#>
param(
    [Parameter(Mandatory)][string[]]$Path,
    [double]$ActiveThresholdDb = -40.0
)

function Measure-One([string]$file) {
    $bytes = [System.IO.File]::ReadAllBytes($file)
    if ([System.Text.Encoding]::ASCII.GetString($bytes, 0, 4) -ne 'RIFF') { throw "Not a RIFF WAV: $file" }

    $pos = 12; $fmt = $null; $dataOff = -1; $dataLen = 0
    while ($pos + 8 -le $bytes.Length) {
        $id = [System.Text.Encoding]::ASCII.GetString($bytes, $pos, 4)
        $len = [BitConverter]::ToUInt32($bytes, $pos + 4)
        if ($id -eq 'fmt ') {
            $fmt = @{
                Format   = [BitConverter]::ToUInt16($bytes, $pos + 8)
                Channels = [BitConverter]::ToUInt16($bytes, $pos + 10)
                Rate     = [BitConverter]::ToUInt32($bytes, $pos + 12)
                Bits     = [BitConverter]::ToUInt16($bytes, $pos + 22)
            }
            if ($fmt.Format -eq 0xFFFE) { $fmt.Format = [BitConverter]::ToUInt16($bytes, $pos + 32) }
        } elseif ($id -eq 'data') {
            $dataOff = $pos + 8; $dataLen = [Math]::Min([int64]$len, [int64]($bytes.Length - $dataOff)); break
        }
        $pos += 8 + $len + ($len % 2)
    }
    if (-not $fmt -or $dataOff -lt 0) { throw "Missing fmt or data chunk: $file" }

    $bps = $fmt.Bits / 8; $frame = $bps * $fmt.Channels
    $frames = [int64][Math]::Floor($dataLen / $frame)
    $seconds = $frames / $fmt.Rate

    # Sample every Nth frame (mono mix of channels) to keep this fast on long files.
    $step = [Math]::Max(1, [int]($fmt.Rate / 4000))
    $win = [int]($fmt.Rate / $step)  # about one second of sampled frames
    $peak = 0.0; $sumSq = 0.0; $n = 0; $winSq = 0.0; $winN = 0; $activeWins = 0; $totalWins = 0
    for ($f = 0; $f -lt $frames; $f += $step) {
        $o = $dataOff + $f * $frame; $v = 0.0
        for ($c = 0; $c -lt $fmt.Channels; $c++) {
            $p = $o + $c * $bps
            switch ($fmt.Bits) {
                16 { $s = [BitConverter]::ToInt16($bytes, $p) / 32768.0 }
                # Cast to [int] first: shifting a [byte] in PowerShell overflows silently.
                24 { $i = [int]$bytes[$p] -bor ([int]$bytes[$p+1] -shl 8) -bor ([int]$bytes[$p+2] -shl 16); if ($i -band 0x800000) { $i -= 0x1000000 }; $s = $i / 8388608.0 }
                32 { if ($fmt.Format -eq 3) { $s = [BitConverter]::ToSingle($bytes, $p) } else { $s = [BitConverter]::ToInt32($bytes, $p) / 2147483648.0 } }
                default { throw "Unsupported bit depth $($fmt.Bits): $file" }
            }
            $v += $s
        }
        $v /= $fmt.Channels
        $a = [Math]::Abs($v); if ($a -gt $peak) { $peak = $a }
        $sumSq += $v * $v; $n++
        $winSq += $v * $v; $winN++
        if ($winN -ge $win) {
            $totalWins++
            if ((10 * [Math]::Log10(($winSq / $winN) + 1e-12)) -gt $ActiveThresholdDb) { $activeWins++ }
            $winSq = 0.0; $winN = 0
        }
    }
    $rms = [Math]::Sqrt($sumSq / [Math]::Max(1, $n))
    [pscustomobject]@{
        File      = [System.IO.Path]::GetFileName($file)
        Seconds   = [Math]::Round($seconds, 1)
        Format    = "$($fmt.Rate) Hz / $($fmt.Bits)-bit / $($fmt.Channels) ch"
        PeakDb    = [Math]::Round(20 * [Math]::Log10($peak + 1e-12), 1)
        RmsDb     = [Math]::Round(20 * [Math]::Log10($rms + 1e-12), 1)
        ActivePct = if ($totalWins) { [Math]::Round(100.0 * $activeWins / $totalWins) } else { 100 }
        Path      = $file
    }
}

foreach ($p in $Path) {
    try { Measure-One $p } catch { [pscustomobject]@{ File = [System.IO.Path]::GetFileName($p); Seconds = $null; Format = "ERROR: $($_.Exception.Message)"; Path = $p } }
}
