ARG CI_REGISTRY_IMAGE

FROM $CI_REGISTRY_IMAGE:builder as deps

RUN mkdir -p /src/packages

FROM hub.lumigame.com/base/dotnet/core/sdk:6.0 AS builder
ENV NUGET_PACKAGES=/src/packages

COPY --from=deps $NUGET_PACKAGES/. $NUGET_PACKAGES/

WORKDIR /src
COPY ["MemberSite/MemberSite/MemberSite.csproj", "MemberSite/"]
RUN dotnet restore -v n "./MemberSite/MemberSite.csproj"

COPY . .
WORKDIR "/src/MemberSite"
RUN dotnet publish -c Release -o /app/publish

FROM hub.lumigame.com/base/dotnet/core/aspnet:6.0
WORKDIR /app
COPY --from=builder /app/publish .
EXPOSE 5000

ENTRYPOINT ["dotnet", "MemberSite.dll"]
