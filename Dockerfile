FROM mcr.microsoft.com/dotnet/core/sdk:2.2.301 as build-env

ENV APP_NAME="Liaro"

WORKDIR /app

EXPOSE 5000

COPY ${APP_NAME}.sln .
COPY src/${APP_NAME}/${APP_NAME}.csproj src/${APP_NAME}/${APP_NAME}.csproj
COPY src/${APP_NAME}.DataLayer/${APP_NAME}.DataLayer.csproj src/${APP_NAME}.DataLayer/${APP_NAME}.DataLayer.csproj
COPY src/${APP_NAME}.ModelLayer/${APP_NAME}.ModelLayer.csproj src/${APP_NAME}.ModelLayer/${APP_NAME}.ModelLayer.csproj
COPY src/${APP_NAME}.ServiceLayer/${APP_NAME}.ServiceLayer.csproj src/${APP_NAME}.ServiceLayer/${APP_NAME}.ServiceLayer.csproj
COPY src/${APP_NAME}.Entities/${APP_NAME}.Entities.csproj src/${APP_NAME}.Entities/${APP_NAME}.Entities.csproj

RUN dotnet restore -s https://api.nuget.org/v3/index.json


COPY . /tmp/code
RUN chgrp -R 0 /tmp/code && \
    chmod -R g=u /tmp/code && \
    cp -a /tmp/code/. . && \
    rm -rf /tmp/code && \
    # Output the binary and assets to /out
    mkdir -p /out && \
    # 1) Build the application.
    dotnet publish src/${APP_NAME}/${APP_NAME}.csproj -c Release -o /out


FROM mcr.microsoft.com/dotnet/core/aspnet:2.2.6

COPY --from=build-env /out ./app


WORKDIR /app

ENTRYPOINT ["dotnet", "Liaro.dll"]

RUN chmod -R ug+rwx /app
