FROM ubuntu:18.04

ENV TERM linux
ENV DEBIAN_FRONTEND noninteractive

RUN apt-get update && apt-get install -y \
    software-properties-common \
    build-essential \
    gcc \
    curl \
    libssl-dev \
    wget \
    make

RUN curl -sL https://deb.nodesource.com/setup_10.x | bash -
RUN apt install nodejs -y

RUN wget -q https://packages.microsoft.com/config/ubuntu/18.04/packages-microsoft-prod.deb
RUN dpkg -i packages-microsoft-prod.deb

RUN add-apt-repository universe
RUN apt-get install apt-transport-https -y
RUN apt-get update
RUN apt-get install dotnet-sdk-2.2 -y

RUN mkdir /var/app
WORKDIR ./var/app

COPY ./instances /var/app/instances
COPY ./logs /var/app/logs
COPY ./src /var/app/src
COPY ./build.sh /var/app/build.sh
COPY ./instance-preprocessor.js /var/app/instance-preprocessor.js
COPY ./MCTOPP.csproj /var/app/MCTOPP.csproj
COPY ./omnisharp.json /var/app/omnisharp.json
COPY ./Program.cs /var/app/Program.cs
RUN bash /var/app/build.sh

# Inifinite loop to keep container alive
RUN echo "while(true);" > /app.js

CMD ["node", "/app.js"]