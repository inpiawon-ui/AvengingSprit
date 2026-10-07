$env:OPENAI_API_KEY = $null
Set-Location "C:\won\UnityProject\AvengingSprit"
$names = @('glveil','glray','glmote','glring','glrim','hxcrit','hxweap','hxpow','hxmagic','hxline','hxslash','hxblunt')
foreach ($n in $names) {
  $file = $n.Substring(0,2) + '_' + $n.Substring(2)
  $out = "Projects/AVSR/_exchange/in/$file.png"
  if (Test-Path $out) { Write-Output "있음 $n"; continue }
  Write-Output "발주 $n"
  & "Projects/AVSR/_exchange/order_glhx_parts.ps1" -Which $n *> "Projects/AVSR/_exchange/log_glhx_$n.txt"
  if (Test-Path $out) { Write-Output "받음 $n" } else { Write-Output "실패 $n" }
}
