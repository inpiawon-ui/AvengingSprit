Set-Location "C:\won\UnityProject\AvengingSprit"
$keys = @('cards2','cards3','shrine1','shrine2','shop1')
$ps = foreach ($k in $keys) {
    Start-Process powershell -ArgumentList '-NoProfile','-ExecutionPolicy','Bypass','-Command',"& 'Projects/AVSR/_exchange/order_setc_icons_$k.ps1' *> 'Projects/AVSR/_exchange/log_setc_icons_$k.txt'" -WindowStyle Hidden -PassThru
    Start-Sleep 3
}
$ps | Wait-Process
foreach ($k in $keys) { $f = "Projects/AVSR/_exchange/in/setc_icons_$k.png"; if (Test-Path $f) { "ok $k" } else { "MISSING $k" } }
