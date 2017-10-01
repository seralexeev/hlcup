#!/bin/sh

unzip /tmp/data/data.zip -d /data
cp /tmp/data/options.txt /data
dotnet hlcup.dll
