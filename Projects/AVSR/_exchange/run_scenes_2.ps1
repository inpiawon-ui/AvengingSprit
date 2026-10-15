Set-Location "C:\won\UnityProject\AvengingSprit"
$keys = @('scene_s14_rates','scene_s15_history','scene_s16_pass','scene_s17_mission','scene_s18_premium','scene_s09_fx1','scene_s10_fx10','scene_s11_fxSpecial')
$running = @()
foreach ($k in $keys) {
    $p = Start-Process powershell -ArgumentList '-NoProfile','-ExecutionPolicy','Bypass','-Command',"& 'Projects/AVSR/_exchange/order_$k.ps1' *> 'Projects/AVSR/_exchange/log_$k.txt'" -WindowStyle Hidden -PassThru
    $running += $p
    Write-Output "started $k"
    Start-Sleep 3
}
$running | Wait-Process
foreach ($k in $keys) { $f = "Projects/AVSR/_exchange/in/$k.png"; if (Test-Path $f) { "ok $k" } else { "MISSING $k" } }
