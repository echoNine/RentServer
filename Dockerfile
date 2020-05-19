FROM mcr.microsoft.com/dotnet/core/sdk:2.1.803 AS build

WORKDIR /RentServer

COPY RentServer.sln .
COPY RentServer.csproj .

RUN dotnet restore

COPY . .
RUN dotnet publish -c release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/core/aspnet:2.1.15

WORKDIR /app

COPY --from=build /app ./

RUN mkdir -p /app/wwwroot/static/imgs
RUN mkdir -p /app/wwwroot/static/videos

CMD ["dotnet", "RentServer.dll"]