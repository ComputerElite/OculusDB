@echo off
echo replacing frontend dir with the one from Release
xcopy /Y /E /H /C /I frontend "bin\Release\net6.0\frontend"

set /p changelog=Changelog: 
echo Pushing to GitHub

git add *
git commit -m "%changelog%"
for /f %%i in ('git rev-parse HEAD') do set id=%%i
git push origin main

echo Pushed to GitHub

echo Commit id %id%
start "" https://manage.rui2015.me/

cd bin\Release\net6.0\
echo Deleting existing update zip
del net6.0.zip
echo %id% > commit.txt

echo Creating new update zip
7z a net6.0.zip *.dll *.pdb *.exe *.json *.txt frontend ref runtimes

echo Created update zip
cd..
cd..
cd..