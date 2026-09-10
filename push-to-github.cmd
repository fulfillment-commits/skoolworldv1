@echo off
setlocal EnableExtensions EnableDelayedExpansion

set "BUILD_DIR=%~dp0"
for %%I in ("%BUILD_DIR%\..\..") do set "REPO_ROOT=%%~fI"

echo.
echo Unity WebGL GitHub deploy
echo Build folder: %BUILD_DIR%
echo Repo root:    %REPO_ROOT%
echo.

cd /d "%REPO_ROOT%" || exit /b 1

git lfs install --local || exit /b 1

echo Staging Unity WebGL build files...
git add .gitignore .gitattributes SetterlunUniversity\build || exit /b 1

git diff --cached --quiet
if errorlevel 1 (
  echo Creating source commit...
  git commit -m "Update Unity WebGL build" || exit /b 1
) else (
  echo No source commit needed. The build folder is already committed.
)

for /f "tokens=1" %%H in ('git ls-remote --heads github master') do set "REMOTE_MASTER=%%H"
if defined REMOTE_MASTER (
  set "LEASE_ARG=--force-with-lease=master:!REMOTE_MASTER!"
) else (
  set "LEASE_ARG="
)

set "TEMP_INDEX=%TEMP%\setterlun-deploy-index-%RANDOM%-%RANDOM%"
if exist "%TEMP_INDEX%" del /f /q "%TEMP_INDEX%"

echo Creating deploy-only master commit with index.html at repo root...
set "GIT_INDEX_FILE=%TEMP_INDEX%"
git read-tree HEAD:SetterlunUniversity/build || goto fail
for /f "delims=" %%T in ('git write-tree') do set "DEPLOY_TREE=%%T"
for /f "delims=" %%C in ('git commit-tree !DEPLOY_TREE! -p refs/heads/master -m "Deploy Unity WebGL build at root"') do set "DEPLOY_COMMIT=%%C"
set "GIT_INDEX_FILE="

if not defined DEPLOY_COMMIT goto fail

git update-ref refs/heads/master !DEPLOY_COMMIT! || exit /b 1

echo Pushing deploy-only master to GitHub...
git push !LEASE_ARG! github master || exit /b 1

if exist "%TEMP_INDEX%" del /f /q "%TEMP_INDEX%"

echo.
echo Done. GitHub master now has index.html at the repository root.
pause
exit /b 0

:fail
set "GIT_INDEX_FILE="
if exist "%TEMP_INDEX%" del /f /q "%TEMP_INDEX%"
echo.
echo Failed. Check the Git error above.
pause
exit /b 1
