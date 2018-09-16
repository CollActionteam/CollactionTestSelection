FROM microsoft/dotnet:2.1-sdk AS build
WORKDIR /src
COPY CollactionTestSelection/CollactionTestSelection.csproj CollactionTestSelection/
RUN dotnet restore CollactionTestSelection/CollactionTestSelection.csproj
COPY . .
WORKDIR /src/CollactionTestSelection
RUN dotnet build CollactionTestSelection.csproj -c Release -o /app
RUN dotnet publish CollactionTestSelection.csproj -c Release -o /app
RUN apt update && apt install dos2unix && dos2unix /app/ecs-deploy

FROM microsoft/dotnet:2.1.1-aspnetcore-runtime AS final
WORKDIR /app
COPY --from=build /app .
RUN chmod a+x /app/ecs-deploy && apt-get update && apt-get install -y python3 python3-pip jq && pip3 install awscli
ENTRYPOINT ["dotnet", "CollactionTestSelection.dll"]