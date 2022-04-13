@echo off

set /p changelog=Changelog: 
echo Pushing to GitHub

git add *
git commit -m "%changelog%"
for /f %%i in ('git rev-parse HEAD') do set id=%%i
git push origin main

echo Pushed to GitHub

echo Commit id %id%
echo Deleting existing update zip
del "bin\Debug\net6.0\net6.0.zip"

echo Creating new update zip
7z a "bin\Debug\net6.0\net6.0.zip" "bin\Debug\net6.0\*.dll" "bin\Debug\net6.0\*.pdb" "bin\Debug\net6.0\*.exe" "bin\Debug\net6.0\*.json" "bin\Debug\net6.0\frontend" "bin\Debug\net6.0\ref" "bin\Debug\net6.0\runtimes"

echo Created update zip
echo Changelog:
echo.
echo %changelog%\nFull changes: https://github.com/ComputerElite/OculusDB/commit/18fc696eeb74f8e7a4df03b4596501f02d2f2989

start "" https://oculusdb.rui2015.me/admin