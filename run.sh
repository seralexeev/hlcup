#!/bin/sh

mkdrir /data
unzip /tmp/data/data.zip -d /data
cp /tmp/data/options.txt /data

warmup () {
    sleep 30

    for i in {1..10}; do
        curl -s -o /dev/null http://127.0.0.1/users/$1
        curl -s -o /dev/null http://127.0.0.1/locations/$1
        curl -s -o /dev/null http://127.0.0.1/visists/$1
        curl -s -o /dev/null http://127.0.0.1/users/$1/visits?toDistance=13
        curl -s -o /dev/null http://127.0.0.1/locations/$1/avg?gender=m
        curl -s -o /dev/null -H "Content-Type: application/json" -X POST -d '{"id":"0"}' http://127.0.0.1/users/new
        curl -s -o /dev/null -H "Content-Type: application/json" -X POST -d '{"id":"0"}' http://127.0.0.1/users/$1
    done

    curl -s -o /dev/null http://127.0.0.1/users/10000000
    curl -s -o /dev/null http://127.0.0.1/users/100000000000000
}

warmup & dotnet hlcup.dll
