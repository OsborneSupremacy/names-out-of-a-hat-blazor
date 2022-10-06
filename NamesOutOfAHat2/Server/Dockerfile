#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.
ARG NUGET_API_KEY

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["NamesOutOfAHat2/Server/NamesOutOfAHat2.Server.csproj", "NamesOutOfAHat2/Server/"]
COPY ["NamesOutOfAHat2/Client/NamesOutOfAHat2.Client.csproj", "NamesOutOfAHat2/Client/"]
COPY ["NamesOutOfAHat2.Model/NamesOutOfAHat2.Model.csproj", "NamesOutOfAHat2.Model/"]
COPY ["NamesOutOfAHat2.Service/NamesOutOfAHat2.Service.csproj", "NamesOutOfAHat2.Service/"]
COPY ["NamesOutOfAHat2.Interface/NamesOutOfAHat2.Interface.csproj", "NamesOutOfAHat2.Interface/"]
COPY ["NamesOutOfAHat2.Utility/NamesOutOfAHat2.Utility.csproj", "NamesOutOfAHat2.Utility/"]

RUN dotnet new nugetconfig
RUN dotnet nuget add source https://nuget.pkg.github.com/osbornesupremacy/index.json -n PrivateFeed -u docker -p %NUGET_API_KEY% --store-password-in-clear-text --configfile nuget.config

RUN dotnet restore "NamesOutOfAHat2/Server/NamesOutOfAHat2.Server.csproj"
WORKDIR "/src/NamesOutOfAHat2/Server"
RUN dotnet build "NamesOutOfAHat2.Server.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "NamesOutOfAHat2.Server.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "NamesOutOfAHat2.Server.dll"]