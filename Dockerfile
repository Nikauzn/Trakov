#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/core/aspnet:3.1-buster-slim AS base
WORKDIR /app
CMD ASPNETCORE_URLS=http://*:$PORT dotnet Trakov.Backend.dll
EXPOSE 80
EXPOSE 443
EXPOSE 5001
EXPOSE 5000

FROM mcr.microsoft.com/dotnet/core/sdk:3.1-buster AS build
WORKDIR /src
COPY ["Trakov.Backend.csproj", ""]
RUN dotnet restore "./Trakov.Backend.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "Trakov.Backend.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Trakov.Backend.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Trakov.Backend.dll"]