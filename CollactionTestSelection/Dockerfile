FROM mcr.microsoft.com/dotnet/core/sdk:3.1-buster AS build
ARG BUILD_CONFIG=Release
WORKDIR /src
COPY CollactionTestSelection/CollactionTestSelection.csproj CollactionTestSelection/
RUN dotnet restore CollactionTestSelection/CollactionTestSelection.csproj
COPY . .
WORKDIR /src/CollactionTestSelection
RUN dotnet build CollactionTestSelection.csproj -c Release -o /app
RUN dotnet publish CollactionTestSelection.csproj -c Release -o /app
RUN apt-get update && apt-get install dos2unix && dos2unix /app/ecs-deploy

FROM mcr.microsoft.com/dotnet/core/aspnet:3.1-buster-slim
WORKDIR /app
COPY --from=build /app .
RUN chmod a+x /app/ecs-deploy && apt-get update && apt-get install -y python3 python3-pip jq && pip3 install awscli
ENTRYPOINT ["dotnet", "CollactionTestSelection.dll"]