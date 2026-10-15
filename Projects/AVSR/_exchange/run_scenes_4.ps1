Set-Location "C:\won\UnityProject\AvengingSprit"
$keys = @('scene_s22_lobbyFree','scene_s23_lobbyGuide','scene_s24_cardSheet','scene_s25_buttonSheet')
$running = @()
foreach ($k in $keys) {
    $p = Start-Process powershell -ArgumentList '-NoProfile','-ExecutionPolicy','Bypass','-Command',"& 'Projects/AVSR/_exchange/order_$k.ps1' *> 'Projects/AVSR/_exchange/log_$k.txt'" -WindowStyle Hidden -PassThru
    $running += $p
    Write-Output "started $k"
    Start-Sleep 3
}
$running | Wait-Process
foreach ($k in $keys) { $f = "Projects/AVSR/_exchange/in/$k.png"; if (Test-Path $f) { "ok $k" } else { "MISSING $k" } }
