FROM mcr.microsoft.com/dotnet/core/aspnet:3.1-buster-slim AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/core/sdk:3.1-buster AS build
WORKDIR /src
COPY ["src/Avalon.Admin.Api/Avalon.Admin.Api.csproj", "src/Avalon.Admin.Api/"]
COPY ["src/Avalon.Admin.EntityFramework.Shared/Avalon.Admin.EntityFramework.Shared.csproj", "src/Avalon.Admin.EntityFramework.Shared/"]
COPY ["src/Avalon.Admin.EntityFramework.SqlServer/Avalon.Admin.EntityFramework.SqlServer.csproj", "src/Avalon.Admin.EntityFramework.SqlServer/"]
COPY ["src/Avalon.Admin.EntityFramework.PostgreSQL/Avalon.Admin.EntityFramework.PostgreSQL.csproj", "src/Avalon.Admin.EntityFramework.PostgreSQL/"]
COPY ["src/Avalon.Shared/Avalon.Shared.csproj", "src/Avalon.Shared/"]
COPY ["src/Avalon.Admin.EntityFramework.MySql/Avalon.Admin.EntityFramework.MySql.csproj", "src/Avalon.Admin.EntityFramework.MySql/"]
RUN dotnet restore "src/Avalon.Admin.Api/Avalon.Admin.Api.csproj"
COPY . .
WORKDIR "/src/src/Avalon.Admin.Api"
RUN dotnet build "Avalon.Admin.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Avalon.Admin.Api.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENV ASPNETCORE_FORWARDEDHEADERS_ENABLED=true
ENTRYPOINT ["dotnet", "Avalon.Admin.Api.dll"]