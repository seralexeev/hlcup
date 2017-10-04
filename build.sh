#!/bin/sh

docker build -t richbitch .
docker tag richbitch stor.highloadcup.ru/travels/helpful_shark
docker push stor.highloadcup.ru/travels/helpful_shark