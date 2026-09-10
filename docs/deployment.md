# Despliegue en IIS

## Requisitos del servidor

- IIS y .NET Hosting Bundle para .NET 10.
- Certificado TLS válido para el sitio.
- Acceso de red a SQL Server.
- Cuenta SQL con permisos de lectura sobre `clientes.promesas`, `dbo.pagos` y `dbo.cartera`.

Node.js solo es necesario en el equipo que genera el build de React; no se requiere en el servidor IIS si se entrega el directorio `dist`.

## Configuración de producción

Las credenciales no se incluyen en `appsettings.json`, el repositorio ni los archivos de frontend. Configura estos valores como variables de entorno de la aplicación IIS, configuración protegida o el mecanismo definido por infraestructura:

```text
ConnectionStrings__SqlServer=<cadena de conexión SQL Server>
Authentication__Simple__Username=<usuario de Ascendra>
Authentication__Simple__Password=<contraseña de Ascendra>
Cors__AllowedOrigins__0=https://<dominio-del-frontend>
```

Si API e interfaz se sirven desde el mismo origen, la configuración de CORS puede restringirse a ese dominio o eliminarse cuando no sea necesaria.

## Generar los entregables

### API

```bash
dotnet publish src/Ascendra.CobranzaXolo.Api \
  --configuration Release \
  --output ./publish/api
```

El resultado `publish/api` es el directorio que debe configurarse como aplicación ASP.NET Core en IIS.

### Interfaz React

Define la URL pública de la API antes de compilar. Debe terminar en `/api/v1`.

```bash
cd src/Ascendra.CobranzaXolo.Web/client
VITE_API_BASE_URL=https://<dominio-api>/api/v1 npm ci
VITE_API_BASE_URL=https://<dominio-api>/api/v1 npm run build
```

Publica el contenido de `src/Ascendra.CobranzaXolo.Web/client/dist` como sitio estático IIS. Si la interfaz y la API comparten dominio, usa la ruta pública correspondiente de la API.

## Configuración de IIS

1. Crea una aplicación para el contenido publicado de la API y usa un application pool **No Managed Code**.
2. Crea un sitio o aplicación estática para los archivos compilados de React.
3. Configura HTTPS y redirige HTTP a HTTPS.
4. Otorga a la identidad del application pool acceso de lectura al directorio publicado.
5. Registra las variables de entorno de producción fuera del repositorio y reinicia la aplicación.

## Validación posterior al despliegue

1. Comprueba `GET /health`.
2. Inicia sesión y confirma acceso a las cinco áreas operativas.
3. Verifica filtros, paginación y exportaciones de Promesas y Recurrencia.
4. Confirma que la aplicación refleja cambios recientes de SQL Server.
5. Revisa los registros de IIS y de la aplicación antes de liberar el acceso a usuarios finales.

## Operación

Los datos se consultan en tiempo real desde SQL Server. No hay procesos de importación de Excel ni tareas programadas que deban ejecutarse junto con la aplicación.
