Set-Location "C:\won\UnityProject\AvengingSprit"
$keys = @('levelup4','altar3','shop3','clear3','room_heal3','room_devil3','room_shop3')
$max = 4
$running = @()
foreach ($k in $keys) {
    while (($running | Where-Object { -not $_.HasExited }).Count -ge $max) { Start-Sleep 5 }
    $p = Start-Process powershell -ArgumentList '-NoProfile','-ExecutionPolicy','Bypass','-Command',"& 'Projects/AVSR/_exchange/review_fx_story_$k.ps1' *> 'Projects/AVSR/_exchange/log_review_$k.txt'" -WindowStyle Hidden -PassThru
    $running += $p
    Write-Output "started $k"
    Start-Sleep 3
}
$running | Wait-Process
foreach ($k in $keys) { $f = "Projects/AVSR/_exchange/review_fx_story_$k.md"; if (Test-Path $f) { "ok $k" } else { "MISSING $k" } }
