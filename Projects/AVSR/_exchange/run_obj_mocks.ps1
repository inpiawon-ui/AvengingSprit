Set-Location "C:\won\UnityProject\AvengingSprit"
$keys = @('heal_shrine','devil_altar','shop_stall')
$ps = foreach ($k in $keys) {
    Start-Process powershell -ArgumentList '-NoProfile','-ExecutionPolicy','Bypass','-Command',"& 'Projects/AVSR/_exchange/order_obj_mock_$k.ps1' *> 'Projects/AVSR/_exchange/log_obj_mock_$k.txt'" -WindowStyle Hidden -PassThru
    Start-Sleep 3
}
$ps | Wait-Process
foreach ($k in $keys) { $f = "Projects/AVSR/_exchange/in/mock_obj_$($k)_v1.png"; if (Test-Path $f) { "ok $k" } else { "MISSING $k" } }
