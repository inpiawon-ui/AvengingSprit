$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$names = @('wpbeam','wpcut','wppulse','wpstate','amland','hsinv','cmwall','gamark','gaexec','hopaim')
$files = @{ wpbeam='wp_beam'; wpcut='wp_cut'; wppulse='wp_pulse'; wpstate='wp_state'; amland='am_land'; hsinv='hs_inv'; cmwall='cm_wall'; gamark='ga_mark'; gaexec='ga_exec'; hopaim='hop_aim' }
foreach ($n in $names) {
  $out = "Projects/AVSR/_exchange/in/$($files[$n]).png"
  if (Test-Path $out) { Write-Output "있음 $n"; continue }
  Write-Output "발주 $n"
  & "Projects/AVSR/_exchange/order_wp_parts.ps1" -Which $n *> "Projects/AVSR/_exchange/log_wp_$n.txt"
  if (Test-Path $out) { Write-Output "받음 $n" } else { Write-Output "실패 $n" }
}
