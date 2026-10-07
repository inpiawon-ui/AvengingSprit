$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
New-Item -ItemType Directory -Force "Projects/AVSR/_exchange/in/_rejected" | Out-Null
foreach ($n in @('glveil','glray','glmote','glring')) {
  $file = $n.Substring(0,2) + '_' + $n.Substring(2)
  $out = "Projects/AVSR/_exchange/in/$file.png"
  if (Test-Path $out) { Move-Item $out "Projects/AVSR/_exchange/in/_rejected/$file.v1.png" -Force }
  Write-Output "발주 $n"
  & "Projects/AVSR/_exchange/order_gl_parts_v2.ps1" -Which $n *> "Projects/AVSR/_exchange/log_glv2_$n.txt"
  if (Test-Path $out) { Write-Output "받음 $n" } else { Write-Output "실패 $n" }
}
