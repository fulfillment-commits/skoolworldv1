@echo off
setlocal

cd /d "%~dp0"

echo Deploying Unity WebGL build from:
echo %cd%
echo.

netlify deploy --no-build --prod --dir .
set DEPLOY_EXIT_CODE=%ERRORLEVEL%

echo.
if "%DEPLOY_EXIT_CODE%"=="0" (
    echo Netlify deploy completed successfully.
) else (
    echo Netlify deploy failed with exit code %DEPLOY_EXIT_CODE%.
)

pause
exit /b %DEPLOY_EXIT_CODE%
