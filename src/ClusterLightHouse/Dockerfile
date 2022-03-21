#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS base
WORKDIR /app
ENV CLUSTER_PORT=4053
ENV CMD_PORT=9110
ENV ACTORSYSTEM="FireAlert"
ENV CLUSTER_SEEDS="[akka.tcp://FireAlert@127.0.0.1:4053,akka.tcp://FireAlert@light-house-2:4054]"

# 9110 - Petabridge.Cmd
# 4053 - Akka.Cluster
EXPOSE ${CMD_PORT} ${CLUSTER_PORT}

# Install Petabridge.Cmd client so it can be invoked remotely via
# Docker or K8s 'exec` commands
RUN dotnet tool install --global pbm 
# RUN pbm help

COPY bin/Release/net6.0/publish/ /app

FROM mcr.microsoft.com/dotnet/runtime:6.0 AS app
WORKDIR /app

COPY --from=base /app /app

# copy .NET Core global tool
COPY --from=base /root/.dotnet /root/.dotnet/

# Needed because https://stackoverflow.com/questions/51977474/install-dotnet-core-tool-dockerfile
ENV PATH="${PATH}:/root/.dotnet/tools"

CMD ["dotnet", "ClusterLightHouse.dll"]