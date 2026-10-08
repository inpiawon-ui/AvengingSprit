Set-Location "C:\won\UnityProject\AvengingSprit"
$keys = @('obj_devil_altar','obj_shop_stall','obj_ring_heal','obj_ring_devil','obj_ring_shop','obj_mark_heal','obj_mark_devil','obj_mark_shop','levelupframe','cardpanel_common','cardpanel_rare','cardpanel_epic','cardpanel_legendary','cardpanel_evolution','cardchip_rarity','cardchip_level','shrineframe','shrinehintpill','eventframe','eventrewardpill','eventcostpill','eventacceptbutton','eventdeclinebutton','shopframe','shopgoldpill','shopitemcell','shopitemcell_off','shopleavebutton')
$max = 5
$running = @()
foreach ($k in $keys) {
    while (($running | Where-Object { -not $_.HasExited }).Count -ge $max) { Start-Sleep 5 }
    $p = Start-Process powershell -ArgumentList '-NoProfile','-ExecutionPolicy','Bypass','-Command',"& 'Projects/AVSR/_exchange/order_setc_$k.ps1' *> 'Projects/AVSR/_exchange/log_setc_$k.txt'" -WindowStyle Hidden -PassThru
    $running += $p
    Write-Output "started $k"
    Start-Sleep 3
}
$running | Wait-Process
foreach ($k in $keys) { $f = "Projects/AVSR/_exchange/in/setc_$k.png"; if (Test-Path $f) { "ok $k" } else { "MISSING $k" } }
