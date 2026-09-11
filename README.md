# Ascendra · Cobranza Xolo

Dashboard de cobranza para seguimiento de pagos, promesas y recuperación.

## Tecnologías

- ASP.NET Core 10
- React
- SQL Server

## Carpetas principales

- `src/Ascendra.CobranzaXolo.Api`: API y configuración.
- `src/Ascendra.CobranzaXolo.Web/client`: interfaz web.
- `tests`: pruebas automatizadas.
- `docs`: notas de arquitectura y despliegue.

## Desarrollo local

La conexión a SQL Server y las credenciales se configuran localmente. No se deben agregar secretos al repositorio.

```bash
dotnet test

cd src/Ascendra.CobranzaXolo.Web/client
npm ci
npm run dev
```
