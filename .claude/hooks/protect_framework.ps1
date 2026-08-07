# ─── 프레임워크 수정 허용 이메일 목록 ───────────────────────
$FrameworkOwnerEmails = @(
	'dev@wastudio.kr'
	'wastudio26@gmail.com'
	'heaven1n@wastudio.kr'
	'heaven12n@nate.com'
	'heaven12n@gmail.com'
	'heaven1201n@gmail.com'
)
# ──────────────────────────────────────────────────────────

$gitEmail = (git config user.email 2>$null).Trim()
if ($FrameworkOwnerEmails -contains $gitEmail) { exit 0 }

$raw = [Console]::In.ReadToEnd()

# Edit / Write 툴: file_path 필드 검사
if ($raw -match '"file_path"\s*:\s*"[^"]*Assets[/\\]GameFramework[/\\]') {
    $match = [regex]::Match($raw, '"file_path"\s*:\s*"([^"]*Assets[/\\]GameFramework[/\\][^"]*)"')
    $path = if ($match.Success) { $match.Groups[1].Value } else { "(경로 파싱 실패)" }
    Write-Host "ERROR: Assets/GameFramework/ 수정 금지 — 프레임워크 코어는 절대 수정하지 않습니다." -ForegroundColor Red
    Write-Host "차단된 경로: $path" -ForegroundColor Yellow
    exit 2
}

# Bash 툴: command 필드에서 GameFramework 경로 포함 여부 검사
if ($raw -match '"command"\s*:') {
    $cmdMatch = [regex]::Match($raw, '"command"\s*:\s*"((?:[^"\\]|\\.)*)"')
    if ($cmdMatch.Success) {
        $cmd = $cmdMatch.Groups[1].Value
        # 쓰기성 명령(cp, mv, Copy-Item, Move-Item, sed, tee, Out-File, Set-Content, Write-Output >)과
        # GameFramework 경로가 함께 등장하면 차단
        $hasWriteCmd = $cmd -match '\b(cp|mv|Copy-Item|Move-Item|New-Item|sed|tee|scp|rsync)\b' `
                    -or $cmd -match '(Out-File|Set-Content|Add-Content|Write-Output\s.*>|>>?)' `
                    -or $cmd -match '\brm\b|\bRemove-Item\b'
        $hasGFPath   = $cmd -match 'Assets[/\\]GameFramework[/\\]'
        if ($hasWriteCmd -and $hasGFPath) {
            Write-Host "ERROR: Assets/GameFramework/ 수정 금지 — Bash 명령으로 프레임워크를 수정하려 했습니다." -ForegroundColor Red
            Write-Host "차단된 명령: $($cmd.Substring(0, [Math]::Min(120, $cmd.Length)))" -ForegroundColor Yellow
            exit 2
        }
    }
}
