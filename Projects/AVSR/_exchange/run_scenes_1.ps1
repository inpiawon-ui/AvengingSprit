Set-Location "C:\won\UnityProject\AvengingSprit"
$keys = @('scene_s01_lobby','scene_s02_daily','scene_s03_chest','scene_s04_currency','scene_s05_confirm','scene_s06_noGem','scene_s07_reward','scene_s12_result1','scene_s13_result10')
$running = @()
foreach ($k in $keys) {
    $p = Start-Process powershell -ArgumentList '-NoProfile','-ExecutionPolicy','Bypass','-Command',"& 'Projects/AVSR/_exchange/order_$k.ps1' *> 'Projects/AVSR/_exchange/log_$k.txt'" -WindowStyle Hidden -PassThru
    $running += $p
    Write-Output "started $k"
    Start-Sleep 3
}
$running | Wait-Process
foreach ($k in $keys) { $f = "Projects/AVSR/_exchange/in/$k.png"; if (Test-Path $f) { "ok $k" } else { "MISSING $k" } }
