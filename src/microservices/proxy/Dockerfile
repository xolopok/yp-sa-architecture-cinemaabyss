FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG TARGETARCH
WORKDIR /source

COPY --link Proxy.csproj .
RUN dotnet restore -a $TARGETARCH

COPY --link . .
RUN dotnet publish -a $TARGETARCH -c release --no-restore -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0
EXPOSE 8000
WORKDIR /app
COPY --link --from=build /app .
USER $APP_UID
ENTRYPOINT ["./Proxy"]
