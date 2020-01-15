#!/bin/bash

unlink program
dotnet publish -c App -r ubuntu.16.10-x64
ln -s ./bin/App/netcoreapp2.2/ubuntu.16.10-x64/MCTOPP program
# dotnet publish -c App -r win10-x64
