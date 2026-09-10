# Interfaz web

Este directorio contiene la interfaz React de Ascendra · Cobranza Xolo. El código de la aplicación está en `client` y se compila como un sitio estático.

## Desarrollo

```bash
cd client
cp .env.example .env.local
npm ci
npm run dev
```

`VITE_API_BASE_URL` identifica la API. Si no se especifica, el cliente usa `http://localhost:5080/api/v1`.

## Comandos disponibles

```bash
npm run dev
npm run lint
npm run build
npm run format:check
```

## Alcance funcional

- Inicio de sesión con la autenticación Basic de la API.
- Cobranza con heatmaps y comparativo semanal.
- Promesas y pagos con filtros, catálogos, rangos de fecha, paginación y totales.
- Actividad diaria de promesas con detalle por gestor y hora.
- Recurrencia por cliente, semana, estatus de plan y pertenencia al despacho.
- Exportación de Excel en Promesas y Recurrencia, respetando los filtros aplicados.
