echo copying executables
cd frontend
pnpm build
cd ..
cp -r frontend/dist/* ~/rider/OculusDB/OculusDB/bin/Debug/net6.0/frontend

cp -r ~/rider/OculusDB/OculusDB/bin/Debug/net6.0/* ~/testing/OculusDB/frontend
cp -r ~/rider/OculusDB/OculusDB/bin/Debug/net6.0/* ~/testing/OculusDB/node
cp -r ~/rider/OculusDB/OculusDB/bin/Debug/net6.0/* ~/testing/OculusDB/master

echo updating configs
cp -r ~/testing/OculusDB/masterData/* ~/testing/OculusDB/master/data/
cp -r ~/testing/OculusDB/nodeData/* ~/testing/OculusDB/node/data/
cp -r ~/testing/OculusDB/frontendData/* ~/testing/OculusDB/frontend/data/

echo starting up
tmux kill-session -t OculusDB
tmux new-session -d -s "OculusDB"\; split-window -h \; split-window -v

tmux send-keys -t "OculusDB":0.0 "cd ~/testing/OculusDB/master/" Enter
tmux send-keys -t "OculusDB":0.0 "dotnet OculusDB.dll" Enter
tmux send-keys -t "OculusDB":0.1 "cd ~/testing/OculusDB/frontend/" Enter
tmux send-keys -t "OculusDB":0.1 "dotnet OculusDB.dll" Enter
sleep 2.5
tmux send-keys -t "OculusDB":0.2 "cd ~/testing/OculusDB/node/" Enter
tmux send-keys -t "OculusDB":0.2 "dotnet OculusDB.dll --lo --fs 2453152771391571" Enter
tmux attach -t OculusDB
