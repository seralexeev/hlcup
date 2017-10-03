#!/bin/sh

unzip /tmp/data/data.zip -d /data
cp /tmp/data/options.txt /data

warmup () {
    sleep 30
    curl -s -o /dev/null http://127.0.0.1/users/1
    curl -s -o /dev/null http://127.0.0.1/visits/1
    curl -s -o /dev/null http://127.0.0.1/locations/1
    curl -s -o /dev/null http://127.0.0.1/locations/1/avg?gender=m
    curl -s -o /dev/null http://127.0.0.1/users/1/visits?toDistance=13
}

warmup & dotnet hlcup.dll
