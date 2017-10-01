#!/bin/sh

# dotnet run -c release --project hlcup ../hlcupdocs/data/TRAIN/data 8080 &
# SERVER_PID=$!

for i in {1..3}; do
    docker run -v $(pwd)/../hlcupdocs/:/var/loadtest --net host -it --rm direvius/yandex-tank -c load/load_$i.ini
done

# kill -s 9 $SERVER_PID

