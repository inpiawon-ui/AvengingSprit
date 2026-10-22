Set-Location "C:\won\UnityProject\AvengingSprit"
foreach ($k in @("mock_summon_lab_free_peak","mock_summon_lab_free_idle","mock_summon_lab_guide_peak","mock_summon_lab_guide_idle")) {
    for ($try = 0; $try -lt 3; $try++) {
        powershell -NoProfile -ExecutionPolicy Bypass -File "Projects/AVSR/_exchange/order_$k.ps1" *> "Projects/AVSR/_exchange/log_$k.txt"
        if (Test-Path "Projects/AVSR/_exchange/in/$k.png") { break }
        Start-Sleep 120
    }
    if (Test-Path "Projects/AVSR/_exchange/in/$k.png") { "ok $k" } else { "MISSING $k" }
}
