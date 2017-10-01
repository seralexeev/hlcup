#!/bin/sh

docker build -t richbitch .
docker tag richbitch stor.highloadcup.ru/travels/modern_cat
docker push stor.highloadcup.ru/travels/modern_cat