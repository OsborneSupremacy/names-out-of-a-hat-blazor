#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["NamesOutOfAHat2/Server/NamesOutOfAHat2.Server.csproj", "NamesOutOfAHat2/Server/"]
COPY ["NamesOutOfAHat2/Client/NamesOutOfAHat2.Client.csproj", "NamesOutOfAHat2/Client/"]
COPY ["NamesOutOfAHat2.Model/NamesOutOfAHat2.Model.csproj", "NamesOutOfAHat2.Model/"]
COPY ["NamesOutOfAHat2.Service/NamesOutOfAHat2.Service.csproj", "NamesOutOfAHat2.Service/"]
COPY ["NamesOutOfAHat2.Interface/NamesOutOfAHat2.Interface.csproj", "NamesOutOfAHat2.Interface/"]
COPY ["NamesOutOfAHat2.Utility/NamesOutOfAHat2.Utility.csproj", "NamesOutOfAHat2.Utility/"]
RUN dotnet restore "NamesOutOfAHat2/Server/NamesOutOfAHat2.Server.csproj"
COPY . .
WORKDIR "/src/NamesOutOfAHat2/Server"
RUN dotnet build "NamesOutOfAHat2.Server.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "NamesOutOfAHat2.Server.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "NamesOutOfAHat2.Server.dll"]