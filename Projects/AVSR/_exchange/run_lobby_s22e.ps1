Set-Location "C:\won\UnityProject\AvengingSprit"
$keys = @('lobby_backdrop_s22e','top_band_s22e')
$running = @()
foreach ($k in $keys) {
    $p = Start-Process powershell -ArgumentList '-NoProfile','-ExecutionPolicy','Bypass','-Command',"& 'Projects/AVSR/_exchange/order_$k.ps1' *> 'Projects/AVSR/_exchange/log_$k.txt'" -WindowStyle Hidden -PassThru
    $running += $p
    Start-Sleep 3
}
$running | Wait-Process
foreach ($f in @('lobby_backdrop_s22e','fixed_top_band_s22e')) { if (Test-Path "Projects/AVSR/_exchange/in/$f.png") { "ok $f" } else { "MISSING $f" } }
