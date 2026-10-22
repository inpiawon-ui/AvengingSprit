param(
    [Parameter(Mandatory = $true)][string[]]$Sheets,
    # 발주문(order_<이름>.ps1)과 납품(in/<이름>.png)이 있는 교환 폴더
    [string]$Exchange = "Projects/$env:PROJ_CODE/_exchange",
    [int]$Tries = 5,
    [int]$WaitSeconds = 120
)
# 코덱스 그림 발주를 **한 장씩 차례로** 돌린다. 이미 받은 것은 건너뛴다.
# ⚠ 이 실행기는 **한 줄로만** 돌린다 — 두 줄을 동시에 돌리면 겹치는 순간 429(짧은 시간 요청 몰림)가 난다(AVSR 2026-10-22, 24장 중 5장).
# 429 는 사용량 한도가 아니다. 못 받았으면 2분 쉬고 다시(최대 5번).
# 쓰기: powershell -File Tools/harness/run_codex_orders.ps1 -Sheets card_icon_c014,card_icon_c015 [-Exchange Projects/XYZ/_exchange]
$ex = (Resolve-Path $Exchange).Path
$empty = Join-Path $env:TEMP 'codex_empty_stdin.txt'
if (-not (Test-Path $empty)) { Set-Content -Path $empty -Value '' -Encoding utf8 }
foreach ($s in ($Sheets -join ',').Split(',')) {
    $s = $s.Trim()
    if (-not $s) { continue }
    if (Test-Path "$ex\in\$s.png") { "[ORDER] $s 이미 있음"; continue }
    if (-not (Test-Path "$ex\order_$s.ps1")) { "[ORDER] $s fail — 발주문 order_$s.ps1 이 없다"; continue }
    for ($try = 1; $try -le $Tries; $try++) {
        cmd /c "powershell -NoProfile -ExecutionPolicy Bypass -File `"$ex\order_$s.ps1`" < `"$empty`" > `"$ex\log_$s.txt`" 2>&1"
        if (Test-Path "$ex\in\$s.png") { break }
        Start-Sleep -Seconds $WaitSeconds
    }
    "[ORDER] $s $(if (Test-Path "$ex\in\$s.png") {'ok'} else {'fail — log_' + $s + '.txt 를 본다'})"
}
