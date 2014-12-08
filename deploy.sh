#!/bin/sh

git pull;
xbuild;
mono bin/Debug/udpServer.exe&;

