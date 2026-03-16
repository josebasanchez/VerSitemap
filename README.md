# VerSitemap

Front en ASP.NET Core (Razor Pages + SignalR) para cargar enlaces de una pagina y procesarlos con POST a traves de la API de scraping.

## Requisitos

- .NET SDK 10
- API Python `scraper-api` en ejecucion
- MySQL configurado como en la API

## Configuracion

Edita `appsettings.json` o `appsettings.Development.json`:

```
ScraperApi:
  BaseUrl: "http://localhost:5000"
  Username: "admin"
  Password: "admin123"
```

## Ejecutar

```
dotnet run --project VerSitemap/VerSitemap.csproj
```

## Flujo

1. Cargar un dominio/pagina.
2. Seleccionar URLs.
3. Generar para procesarlas y ver el progreso en tiempo real.

