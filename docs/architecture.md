# Arquitectura

## Propósito

Ascendra · Cobranza Xolo centraliza la consulta operativa de recuperación, promesas, pagos y recurrencia. La aplicación lee información de SQL Server; no inserta, modifica ni elimina registros de las tablas operativas.

## Componentes

```text
React
  │ HTTPS / JSON
ASP.NET Core Web API
  │
Application ── reglas, filtros, DTOs y exportaciones
  │
Domain ── entidades y contratos
  │
Infrastructure ── SQL Server y autenticación
  │
SQL Server ── clientes.promesas · dbo.pagos · dbo.cartera
```

La separación permite cambiar la interfaz o el mecanismo de autenticación sin duplicar la lógica de negocio ni las consultas.

## Fuentes de datos

| Tabla | Uso principal |
| --- | --- |
| `clientes.promesas` | Promesas, agentes, fechas, montos, tipos y estatus. |
| `dbo.pagos` | Recuperación, producto, fecha de recepción, semana y día. |
| `dbo.cartera` | Cartera vigente y estatus actual del plan. |

`Cliente_Unico` es la llave lógica para relacionar las tres fuentes. `IdPromesa` identifica una promesa y `dbo.pagos.ID` identifica técnicamente cada pago.

## Reglas de negocio

- Los pagos con `Recuperación_por_Gestión` igual a cero se excluyen de la cobranza atribuible.
- Un pago se atribuye primero a una promesa vigente del mismo cliente. Si no existe, puede atribuirse a una promesa vencida creada durante la misma semana ISO. Los pagos restantes se clasifican como **Orgánico**.
- Cuando hay más de una promesa candidata, se utiliza la más reciente de manera determinista.
- Recurrencia toma `ESTATUS_PLAN` del corte más reciente de `dbo.cartera`. Un cliente presente en ese corte está **Activo**; si no aparece, está **Inactivo**.
- El valor `N/A` de estatus de plan se presenta en la interfaz como **Sin plan**.
- Las exportaciones de Excel reutilizan los mismos filtros y reglas que la vista de origen.

## API e interfaz

La API expone recursos bajo `/api/v1` y devuelve datos paginados. Los catálogos de filtros se consultan de manera independiente para no depender de los registros visibles en una página.

La interfaz React contiene las cinco áreas operativas: Cobranza, Promesas, Pagos, Actividad de promesas y Recurrencia. El frontend no contiene credenciales ni cadenas de conexión.

## Configuración y seguridad

En desarrollo, la API obtiene la cadena de conexión y las credenciales de User Secrets. En IIS, esos valores se suministran por variables de entorno, configuración protegida u otro mecanismo aprobado por infraestructura.

La autenticación actual es usuario y contraseña mediante Basic Authentication sobre HTTPS. El diseño mantiene la autenticación aislada para permitir una futura integración con Active Directory o SSO.

## Rendimiento

- Los filtros y la paginación se ejecutan en SQL Server.
- Las consultas son de solo lectura y se parametrizan.
- Los catálogos usan valores distintos y se cargan por separado de las tablas paginadas.
- Las exportaciones se generan bajo demanda a partir de los filtros solicitados.
