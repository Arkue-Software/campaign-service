# campaign-service

Servicio de Campañas de RedVital (.NET 10 / ASP.NET Core). El esquema
`db_campana` y sus migraciones Flyway viven en `Arkue-Software/databases`;
este servicio solo se conecta mediante el rol de ejecución `campana_servicio`.

## Ejecución local

Desde `Arkue-Software/databases`, levanta `db-campana` y `migracion-campana`
siguiendo su README. La base se mantiene en la red interna
`redvital_campana_data` y no publica un puerto al host.

La API ejecutada con `dotnet run` en el host no puede conectarse a la base.
Para ejecutar la API contra PostgreSQL se necesita un contenedor conectado a
`redvital_campana_data`, usando `Host=db-campana;Port=5432`.

## Docker Compose

`docker-compose.yml` construye la API desde el Dockerfile y publica el puerto
`8080`. Compose crea automáticamente una red predeterminada para la API, por
lo que se puede arrancar el contenedor sin crear redes manualmente:

PowerShell:

```powershell
docker compose up --build
```

En macOS/Linux:

```sh
docker compose up --build
```

Por defecto, la API usa `Host=db-campana;Port=5432;Database=db_campana;Username=campana_servicio`
y descubre JWKS en `http://identity-service:8080/.well-known/openid-configuration`.
Puedes sobrescribir estos valores con `CAMPANAS_CONNECTION_STRING` y
`JWT_DISCOVERY_URL`; `CAMPAIGN_PORT` cambia el puerto publicado y
`ASPNETCORE_ENVIRONMENT` el entorno de ASP.NET Core.

Sin PostgreSQL e Identidad accesibles, el contenedor puede arrancar, pero los
endpoints que consultan la base de datos o validan tokens no funcionarán. Para
una ejecución funcional, conecta la API a las redes de esos servicios o ajusta
las variables de conexión y descubrimiento para que sean alcanzables desde el
contenedor. Esta composición no crea PostgreSQL o Identidad, ni crea o migra el
esquema de la base de datos. No copies llaves privadas a este repositorio.

La aplicación no crea ni migra el esquema al iniciar. La URI de descubrimiento
local está en `appsettings.Development.json`; producción usa la configuración
interna de Identidad desde `appsettings.json`.

## Pruebas

Ejecuta `dotnet test RedVital.Campanas.sln` desde la raíz del repositorio.
