#!/bin/sh

docker build -t richbitch .
docker tag richbitch stor.highloadcup.ru/travels/first_mouse
docker push stor.highloadcup.ru/travels/first_mouse