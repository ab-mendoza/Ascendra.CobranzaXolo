import type {
  Heatmap,
  PaymentCatalog,
  PaymentPage,
  PromiseActivity,
  PromiseActivityDetailItem,
  PromiseCatalog,
  PromisePage,
  Recurrence,
  RecurrenceCatalog,
  RecurrenceDetailItem,
} from "./types";

const apiBase = (
  import.meta.env.VITE_API_BASE_URL ?? "http://localhost:5080/api/v1"
).replace(/\/$/, "");
const credentialKey = "ascendra.authentication";
let basicCredentials = window.sessionStorage.getItem(credentialKey) ?? "";

export interface QueryValues {
  [key: string]: string | number | string[] | number[] | undefined | null;
}

function queryString(values: QueryValues): string {
  const parameters = new URLSearchParams();
  Object.entries(values).forEach(([key, value]) => {
    if (value === undefined || value === null || value === "") return;
    if (Array.isArray(value)) {
      value.forEach((item) => parameters.append(key, String(item)));
      return;
    }
    parameters.set(key, String(value));
  });
  const serialized = parameters.toString();
  return serialized ? `?${serialized}` : "";
}

async function get<T>(path: string, values: QueryValues = {}): Promise<T> {
  const response = await fetch(`${apiBase}${path}${queryString(values)}`, {
    headers: {
      Accept: "application/json",
      ...(basicCredentials
        ? { Authorization: `Basic ${basicCredentials}` }
        : {}),
    },
  });
  if (!response.ok) {
    if (response.status === 401) {
      throw new Error("Usuario o contraseña incorrectos.");
    }

    if (response.status >= 500) {
      throw new Error(
        "No fue posible conectar con la base de datos. Verifica la VPN e inténtalo nuevamente.",
      );
    }

    let detail = "No fue posible cargar la información.";
    try {
      const problem = (await response.json()) as {
        detail?: string;
        title?: string;
      };
      detail = problem.detail ?? problem.title ?? detail;
    } catch {
      // Response did not include a problem-details body.
    }
    throw new Error(detail);
  }
  return response.json() as Promise<T>;
}

async function download(
  path: string,
  values: QueryValues,
  fallbackName: string,
): Promise<void> {
  const response = await fetch(`${apiBase}${path}${queryString(values)}`, {
    headers: {
      ...(basicCredentials
        ? { Authorization: `Basic ${basicCredentials}` }
        : {}),
    },
  });
  if (!response.ok)
    throw new Error("No fue posible preparar el archivo de Excel.");

  const disposition = response.headers.get("content-disposition");
  const name =
    disposition?.match(/filename="?([^";]+)"?/i)?.[1] ?? fallbackName;
  const blobUrl = URL.createObjectURL(await response.blob());
  const anchor = document.createElement("a");
  anchor.href = blobUrl;
  anchor.download = name;
  anchor.click();
  URL.revokeObjectURL(blobUrl);
}

export const api = {
  hasCredentials: () => Boolean(basicCredentials),
  setCredentials: (username: string, password: string) => {
    basicCredentials = window.btoa(`${username}:${password}`);
    window.sessionStorage.setItem(credentialKey, basicCredentials);
  },
  clearCredentials: () => {
    basicCredentials = "";
    window.sessionStorage.removeItem(credentialKey);
  },
  verifyCredentials: () => get<PaymentCatalog>("/catalogos/pagos"),
  paymentCatalog: () => get<PaymentCatalog>("/catalogos/pagos"),
  promiseCatalog: () => get<PromiseCatalog>("/catalogos/promesas"),
  promiseExport: (query: QueryValues) =>
    download("/promesas/exportar", query, "ascendra-promesas.xlsx"),
  recurrenceCatalog: () => get<RecurrenceCatalog>("/catalogos/recurrencia"),
  promises: (query: QueryValues) => get<PromisePage>("/promesas", query),
  payments: (query: QueryValues) => get<PaymentPage>("/pagos", query),
  heatmap: (view: string, week?: number) =>
    get<Heatmap>("/cobranza/heatmap", { view, week }),
  recurrence: (query: QueryValues) => get<Recurrence>("/recurrencia", query),
  recurrenceExport: (query: QueryValues) =>
    download("/recurrencia/exportar", query, "ascendra-recurrencia.xlsx"),
  recurrenceDetail: (clienteUnico: string, year: number, week: number) =>
    get<RecurrenceDetailItem[]>("/recurrencia/detalle", {
      clienteUnico,
      year,
      week,
    }),
  activity: (fecha?: string) =>
    get<PromiseActivity>("/actividad-promesas", { fecha }),
  activityDetail: (fecha: string, agente: string, hora: number) =>
    get<PromiseActivityDetailItem[]>("/actividad-promesas/detalle", {
      fecha,
      agente,
      hora,
    }),
};
