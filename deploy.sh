echo replacing Release dir with frontend
cp -R ./ bin/Release/net6.0/frontend 

read -p "Changelog: " changelog
echo Pushing to GitHub

git add *
git commit -m "$changelog"
id=$(git rev-parse HEAD)
git push origin main

echo Pushed to GitHub

echo Commit id $id

cd bin/Release/net6.0/
echo Deleting existing update zip
rm net6.0.zip

echo Creating new update zip
7z a net6.0.zip *.dll *.pdb *.exe *.json frontend ref runtimes *.txt

echo Created update zip
echo Changelog:
echo .
echo $changelog\\nFull changes: https://github.com/ComputerElite/OculusDB/commit/$id

google-chrome "https://manage.rui2015.me/admin"
cd ..
cd ..
cd ..