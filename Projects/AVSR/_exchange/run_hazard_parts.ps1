Set-Location "C:\won\UnityProject\AvengingSprit"
$keys = @('fx_bomb_reticle','fx_bomb_blast','fx_bomb_shell','obj_neon_cable','fx_neon_arc','fx_gas_cloud','fx_gas_vent')
$running = @()
foreach ($k in $keys) {
    $p = Start-Process powershell -ArgumentList '-NoProfile','-ExecutionPolicy','Bypass','-Command',"& 'Projects/AVSR/_exchange/order_$k.ps1' *> 'Projects/AVSR/_exchange/log_$k.txt'" -WindowStyle Hidden -PassThru
    $running += $p
    Start-Sleep 3
}
$running | Wait-Process
foreach ($k in $keys) { $f = "Projects/AVSR/_exchange/in/$k.png"; if (Test-Path $f) { "ok $k" } else { "MISSING $k" } }
