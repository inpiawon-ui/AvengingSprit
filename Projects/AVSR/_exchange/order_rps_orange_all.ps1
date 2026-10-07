$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
foreach ($n in @('arrows','tri_force','tri_blade','tri_magic')) {
  Write-Output "order $n"
  & "Projects/AVSR/_exchange/order_rps_orange.ps1" -Which $n *> "Projects/AVSR/_exchange/log_rpso_$n.txt"
  Write-Output "done $n"
}
