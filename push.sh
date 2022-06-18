echo replacing frontend dir with the one from Debug
cp -R bin/Debug/net6.0/frontend frontend

read -p "Changelog: " changelog
echo Pushing to GitHub

git add *
git commit -m "$changelog"
id=$(git rev-parse HEAD)
git push origin main

echo Pushed to GitHub

echo Commit id $id

cd bin/Debug/net6.0/
echo Deleting existing update zip
rm net6.0.zip

echo Creating new update zip
7z a net6.0.zip *.dll *.pdb *.exe *.json frontend ref runtimes

echo Created update zip
echo Changelog:
echo .
echo $changelog\nFull changes: https://github.com/ComputerElite/OculusDB/commit/$id

#start "" https://oculusdb.rui2015.me/admin
cd..
cd..
cd..