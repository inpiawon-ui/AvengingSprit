Set-Location "C:\won\UnityProject\AvengingSprit"
$keys = @('tue','wed','thu','fri','sat','sun')
$running = @()
foreach ($k in $keys) {
    $p = Start-Process powershell -ArgumentList '-NoProfile','-ExecutionPolicy','Bypass','-Command',"& 'C:\won\UnityProject\AvengingSprit\Projects\AVSR\_exchange\order_night_day_$k.ps1' *> 'C:\won\UnityProject\AvengingSprit\Projects\AVSR\_exchange\log_night_day_$k.txt'" -WindowStyle Hidden -PassThru
    $running += $p
    Start-Sleep 3
}
$running | Wait-Process
foreach ($k in $keys) { $f = "C:\won\UnityProject\AvengingSprit\Projects\AVSR\_exchange\in\night_day_$k.png"; if (Test-Path $f) { "받음 $k" } else { "없음 $k" } }
