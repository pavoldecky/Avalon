FROM mcr.microsoft.com/dotnet/core/aspnet:3.1-buster-slim AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/core/sdk:3.1-buster AS build
WORKDIR /src
COPY ["Avalon.STS.Identity.csproj", "./"]
COPY ["./src/Avalon.Admin.EntityFramework.Shared/Avalon.Admin.EntityFramework.Shared.csproj", "./"]
COPY ["Avalon.Admin.EntityFramework.SqlServer.csproj", "./"]
COPY ["Avalon.Admin.EntityFramework.PostgreSQL.csproj", "./"]
COPY ["Avalon.Shared.csproj", "./"]
COPY ["Avalon.Admin.EntityFramework.MySql.csproj", "./"]
RUN dotnet restore "Avalon.STS.Identity.csproj"
COPY . .
WORKDIR "/src/src/Avalon.STS.Identity"
RUN dotnet build "Avalon.STS.Identity.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Avalon.STS.Identity.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENV ASPNETCORE_FORWARDEDHEADERS_ENABLED=true
ENTRYPOINT ["dotnet", "Avalon.STS.Identity.dll"]