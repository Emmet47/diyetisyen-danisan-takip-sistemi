# syntax=docker/dockerfile:1
# API imajı. Derleme bağlamı (context) repo köküdür:
#   docker build -f src/Ddts.Api/Dockerfile -t ddts-api .
# (docker compose bunu kendisi yapar.)

# ---------- 1. aşama: derleme (SDK imajı, büyük) ----------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Önce yalnızca proje dosyalarını kopyalayıp paketleri indiriyoruz (restore).
# Kod değişse bile paketler değişmediyse bu adım Docker önbelleğinden gelir, derleme hızlanır.
COPY global.json Directory.Build.props Directory.Packages.props ./
COPY src/Ddts.Domain/Ddts.Domain.csproj                 src/Ddts.Domain/
COPY src/Ddts.Application/Ddts.Application.csproj       src/Ddts.Application/
COPY src/Ddts.Infrastructure/Ddts.Infrastructure.csproj src/Ddts.Infrastructure/
COPY src/Ddts.Api/Ddts.Api.csproj                       src/Ddts.Api/
RUN dotnet restore src/Ddts.Api/Ddts.Api.csproj

COPY src/ src/
RUN dotnet publish src/Ddts.Api/Ddts.Api.csproj -c Release -o /app/publish --no-restore

# ---------- 2. aşama: çalıştırma (yalnızca runtime, küçük) ----------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Resmi .NET imajlarındaki yetkisiz kullanıcı (root olarak çalışmıyoruz)
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "Ddts.Api.dll"]
