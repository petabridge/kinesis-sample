#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app

ENV streamName="fire-alert"
ENV FIRE_DEPARTMENT="Texas"
ENV accessKey=""
ENV accessSecret=""
ENV threshold="120"

COPY bin/Release/net6.0/publish/ /app

FROM mcr.microsoft.com/dotnet/runtime:6.0 AS app
WORKDIR /app

COPY --from=base /app /app

ENTRYPOINT ["dotnet", "KinesisSample.dll"]