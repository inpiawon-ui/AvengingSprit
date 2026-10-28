Set-Location "C:\won\UnityProject\AvengingSprit"
$keys = @('mock_simple_obj_ch7','mock_simple_obj_ch8','mock_simple_obj_ch9','mock_simple_obj_ch10','mock_simple_obj_ch11','mock_simple_obj_ch12')
$running = @()
foreach ($k in $keys) {
    $p = Start-Process powershell -ArgumentList '-NoProfile','-ExecutionPolicy','Bypass','-Command',"& 'Projects/AVSR/_exchange/order_$k.ps1' *> 'Projects/AVSR/_exchange/log_$k.txt'" -WindowStyle Hidden -PassThru
    $running += $p
    Write-Output "started $k"
    Start-Sleep 3
}
$running | Wait-Process
foreach ($k in $keys) { $f = "Projects/AVSR/_exchange/in/$k.png"; if (Test-Path $f) { "ok $k" } else { "MISSING $k" } }
