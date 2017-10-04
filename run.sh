#!/bin/sh

unzip /tmp/data/data.zip -d /data
cp /tmp/data/options.txt /data

warmup () {
    sleep 30
    curl -s -o /dev/null http://127.0.0.1/users/1
    curl -s -o /dev/null http://127.0.0.1/locations/1
    curl -s -o /dev/null http://127.0.0.1/visists/1
    curl -s -o /dev/null http://127.0.0.1/users/1/visits?toDistance=13
    curl -s -o /dev/null http://127.0.0.1/locations/1/avg?gender=m
    curl -s -o /dev/null -H "Content-Type: application/json" -X POST -d '{"id":"0"}' http://127.0.0.1/users/new
    curl -s -o /dev/null -H "Content-Type: application/json" -X POST -d '{"id":"0"}' http://127.0.0.1/users/1
}

warmup & dotnet hlcup.dll
