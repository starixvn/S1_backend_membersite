FROM hub.lumigame.com/base/dotnet/core/sdk:6.0 AS builder

COPY . .
WORKDIR MemberSite/MemberSite
RUN dotnet publish -c Release -o /app

FROM hub.lumigame.com/base/dotnet/core/sdk:6.0
WORKDIR /app
COPY --from=builder /app .
RUN apt-get update -y && apt-get install -y libc6-dev libgdiplus

ENTRYPOINT ["dotnet", "MemberSite.dll"]
