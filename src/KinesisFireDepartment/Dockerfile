#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app

ENV ACTORSYSTEM="FireAlert"
ENV coordinate="30.267222,-97.743056"
ENV city="Texas"
ENV name="Houston Fire"
ENV port=5000
EXPOSE ${port}

COPY bin/Release/net6.0/publish/ /app

FROM mcr.microsoft.com/dotnet/runtime:6.0 AS app
WORKDIR /app

COPY --from=base /app /app

ENTRYPOINT ["dotnet", "KinesisFireDepartment.dll"]
