# Ascendra · Cobranza Xolo

Aplicación interna para consultar recuperación, promesas, pagos, actividad del equipo y recurrencia. Reemplaza el dashboard operativo basado en archivos por una interfaz React respaldada por una API ASP.NET Core 10 de solo lectura sobre SQL Server.

## Estructura del repositorio

| Ruta | Responsabilidad |
| --- | --- |
| `src/Ascendra.CobranzaXolo.Domain` | Entidades y contratos del dominio. |
| `src/Ascendra.CobranzaXolo.Application` | Casos de uso, DTOs y reglas de negocio. |
| `src/Ascendra.CobranzaXolo.Infrastructure` | Consultas de solo lectura a SQL Server y adaptadores de infraestructura. |
| `src/Ascendra.CobranzaXolo.Api` | API HTTP, autenticación y configuración. |
| `src/Ascendra.CobranzaXolo.Web/client` | Interfaz React y estilos de la aplicación. |
| `tests` | Pruebas de reglas de negocio y exportaciones. |
| `docs` | Arquitectura y guía de despliegue. |

## Requisitos de desarrollo

- .NET SDK 10
- Node.js LTS y npm
- Acceso de red a SQL Server

## Ejecutar localmente

Configura la API con secretos locales. No agregues credenciales al repositorio.

```bash
cd src/Ascendra.CobranzaXolo.Api
dotnet user-secrets set "ConnectionStrings:SqlServer" "<cadena de conexión SQL Server>"
dotnet user-secrets set "Authentication:Simple:Username" "<usuario>"
dotnet user-secrets set "Authentication:Simple:Password" "<contraseña>"
dotnet run --urls http://localhost:5080
```

En una segunda terminal, inicia la interfaz:

```bash
cd src/Ascendra.CobranzaXolo.Web/client
cp .env.example .env.local
npm ci
npm run dev
```

`VITE_API_BASE_URL` define la URL de la API. Por defecto, el cliente usa `http://localhost:5080/api/v1`.

## Verificación

```bash
dotnet test

cd src/Ascendra.CobranzaXolo.Web/client
npm run lint
npm run build
```

La API publica Swagger únicamente en el ambiente de desarrollo. Las rutas de datos requieren autenticación Basic y las operaciones sobre SQL Server son de consulta.

## Documentación

- [Arquitectura](docs/architecture.md)
- [Despliegue en IIS](docs/deployment.md)
