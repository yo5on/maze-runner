# Publishes a Unity Web build to the gh-pages branch (served by GitHub Pages).
#
# Usage (from the repository root, after building with
# "Maze Runner > Build Web (GitHub Pages)" or the command line build):
#   powershell -ExecutionPolicy Bypass -File tools/deploy-gh-pages.ps1 [-BuildDir Builds/WebGL]
#
# The build is committed only to gh-pages, never to main. Existing gh-pages
# history is kept: each deploy is a normal (non-force) commit on top of it.

param(
    [string]$BuildDir = "Builds/WebGL",
    [string]$Remote = "origin",
    [string]$Branch = "gh-pages"
)

# git writes progress to stderr; rely on $LASTEXITCODE instead of treating stderr as fatal.
$ErrorActionPreference = "Continue"
$repoRoot = (git rev-parse --show-toplevel).Trim()
Set-Location $repoRoot

$buildPath = Resolve-Path $BuildDir
foreach ($required in @("index.html", "Build")) {
    if (-not (Test-Path (Join-Path $buildPath $required))) {
        throw "No Web build found at '$buildPath' (missing $required). Build the project first."
    }
}

$sourceCommit = (git rev-parse --short HEAD).Trim()
$worktree = Join-Path ([System.IO.Path]::GetTempPath()) ("maze-runner-pages-" + [guid]::NewGuid().ToString("N").Substring(0, 8))

git ls-remote --exit-code --heads $Remote $Branch | Out-Null
$remoteHasBranch = ($LASTEXITCODE -eq 0)
if ($remoteHasBranch) {
    git fetch $Remote $Branch
    if ($LASTEXITCODE -ne 0) { throw "git fetch failed" }
}

try {
    if ($remoteHasBranch) {
        git worktree add -B $Branch $worktree "$Remote/$Branch"
        # Clear the previous build from the worktree (tracked files only; the worktree is fresh).
        # mobile-test/ is a separately deployed test build; keep it.
        Get-ChildItem -Force $worktree | Where-Object { $_.Name -notin @(".git", "mobile-test") } | Remove-Item -Recurse -Force
    } else {
        git worktree add --orphan -b $Branch $worktree
    }
    if ($LASTEXITCODE -ne 0) { throw "git worktree add failed" }

    Copy-Item -Recurse -Force (Join-Path $buildPath "*") $worktree
    # Serve files as-is (no Jekyll processing).
    New-Item -ItemType File -Force (Join-Path $worktree ".nojekyll") | Out-Null

    git -C $worktree add -A
    git -C $worktree diff --cached --quiet
    if ($LASTEXITCODE -eq 0) {
        Write-Host "Build is identical to what is already on $Branch; nothing to deploy."
        return
    }
    git -C $worktree commit -m "Deploy Web build from $sourceCommit"
    if ($LASTEXITCODE -ne 0) { throw "git commit failed" }
    git -C $worktree push $Remote "${Branch}:${Branch}"
    if ($LASTEXITCODE -ne 0) { throw "git push failed" }
    Write-Host "Deployed to $Remote/$Branch. Site: https://yo5on.github.io/maze-runner/"
}
finally {
    if (Test-Path $worktree) { git worktree remove --force $worktree }
}
