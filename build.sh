#!/bin/sh

docker build -t richbitch .
docker tag richbitch stor.highloadcup.ru/travels/$DOCKER_IMAGE
docker push stor.highloadcup.ru/travels/$DOCKER_IMAGE