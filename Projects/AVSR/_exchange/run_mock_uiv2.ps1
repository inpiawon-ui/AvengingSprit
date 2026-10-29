Set-Location "C:\won\UnityProject\AvengingSprit"
$keys = @('mock_uiv2_a_home','mock_uiv2_a_growth','mock_uiv2_a_summon','mock_uiv2_a_flow','mock_uiv2_b_home','mock_uiv2_b_growth','mock_uiv2_b_summon','mock_uiv2_b_flow','mock_uiv2_c_home','mock_uiv2_c_growth','mock_uiv2_c_summon','mock_uiv2_c_flow','mock_uiv2_d_home','mock_uiv2_d_growth','mock_uiv2_d_summon','mock_uiv2_d_flow')
for ($i = 0; $i -lt $keys.Count; $i += 3) {
    $batch = $keys[$i..([Math]::Min($i + 2, $keys.Count - 1))]
    $running = @()
    foreach ($k in $batch) {
        $p = Start-Process powershell -ArgumentList '-NoProfile','-ExecutionPolicy','Bypass','-Command',"& 'Projects/AVSR/_exchange/order_$k.ps1' *> 'Projects/AVSR/_exchange/log_$k.txt'" -WindowStyle Hidden -PassThru
        $running += $p
        Write-Output "started $k"
        Start-Sleep 3
    }
    $running | Wait-Process
}
foreach ($k in $keys) { $f = "Projects/AVSR/_exchange/in/$k.png"; if (Test-Path $f) { "ok $k" } else { "MISSING $k" } }
