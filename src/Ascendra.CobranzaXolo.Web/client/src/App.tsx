import { useEffect, useMemo, useState } from "react";
import {
  Bar,
  BarChart,
  CartesianGrid,
  Legend,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from "recharts";
import { api } from "./api";
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
  TabId,
} from "./types";
import "./App.css";

const money = new Intl.NumberFormat("es-MX", {
  style: "currency",
  currency: "MXN",
  minimumFractionDigits: 0,
  maximumFractionDigits: 0,
});
const moneyExact = new Intl.NumberFormat("es-MX", {
  style: "currency",
  currency: "MXN",
  minimumFractionDigits: 2,
  maximumFractionDigits: 2,
});
const tabs: Array<{ id: TabId; label: string }> = [
  { id: "cobranza", label: "Cobranza" },
  { id: "promesas", label: "Promesas" },
  { id: "pagos", label: "Pagos" },
  { id: "actividad", label: "Actividad de promesas" },
  { id: "recurrencia", label: "Recurrencia" },
];
const chartColors = [
  "#4f83ec",
  "#19a974",
  "#f49b42",
  "#9a6bea",
  "#28a7d9",
  "#df6b8c",
];
const formatMoney = (value: number | null | undefined, exact = false) =>
  (exact ? moneyExact : money).format(value ?? 0);
const formatDate = (value: string | null | undefined, time = false) =>
  value
    ? new Intl.DateTimeFormat("es-MX", {
        day: "2-digit",
        month: "2-digit",
        year: "numeric",
        ...(time ? { hour: "2-digit", minute: "2-digit", hour12: false } : {}),
      }).format(new Date(value.includes("T") ? value : `${value}T12:00:00`))
    : "—";
const getError = (error: unknown) =>
  error instanceof Error
    ? error.message
    : "No fue posible cargar la información.";

function LoginScreen({ onSuccess }: { onSuccess: () => void }) {
  const [username, setUsername] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState("");
  const [loading, setLoading] = useState(false);

  const submit = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setLoading(true);
    setError("");
    api.setCredentials(username.trim(), password);
    try {
      await api.verifyCredentials();
      onSuccess();
    } catch (reason: unknown) {
      api.clearCredentials();
      setError(getError(reason));
    } finally {
      setLoading(false);
    }
  };

  return (
    <main className="login-shell">
      <section className="login-card">
        <div className="brand-logo-frame login-logo">
          <img className="brand-logo" src="/assets/xolo-wolf.png" alt="Xolo" />
        </div>
        <p className="brand-eyebrow">GESTIÓN DE COBRANZA</p>
        <h1>Ascendra</h1>
        <p className="login-subtitle">Cobranza Xolo</p>
        <form onSubmit={submit}>
          <label>
            Usuario
            <input
              autoComplete="username"
              value={username}
              onChange={(event) => setUsername(event.target.value)}
              required
            />
          </label>
          <label>
            Contraseña
            <input
              type="password"
              autoComplete="current-password"
              value={password}
              onChange={(event) => setPassword(event.target.value)}
              required
            />
          </label>
          {error && (
            <p className="login-error" role="alert">
              {error}
            </p>
          )}
          <button type="submit" disabled={loading}>
            {loading ? "Verificando…" : "Entrar a Ascendra"}
          </button>
        </form>
        <p className="login-note">
          Acceso seguro a la información de cobranza.
        </p>
      </section>
    </main>
  );
}

function FilterDropdown({
  label,
  options,
  selected,
  onChange,
  allLabel = "Todos",
}: {
  label: string;
  options: Array<string | number>;
  selected: string[];
  onChange: (values: string[]) => void;
  allLabel?: string;
}) {
  const [search, setSearch] = useState("");
  const filtered = useMemo(() => {
    const keyword = search.trim().toLocaleLowerCase("es-MX");
    return options
      .filter((option) =>
        String(option).toLocaleLowerCase("es-MX").includes(keyword),
      )
      .slice(0, 100);
  }, [options, search]);
  const selectionLabel =
    selected.length === 0
      ? allLabel
      : selected.length === 1
        ? selected[0]
        : `${selected.length} seleccionados`;
  const toggle = (value: string) =>
    onChange(
      selected.includes(value)
        ? selected.filter((item) => item !== value)
        : [...selected, value],
    );
  return (
    <div className="filter-field">
      <span className="filter-label">{label}</span>
      <details className="filter-dropdown">
        <summary>
          <span className={selected.length ? "" : "placeholder"}>
            {selectionLabel}
          </span>
          <span className="caret">⌄</span>
        </summary>
        <div className="dropdown-panel">
          <input
            className="dropdown-search"
            type="search"
            value={search}
            onChange={(event) => setSearch(event.target.value)}
            placeholder={`Buscar ${label.toLocaleLowerCase("es-MX")}`}
          />
          <div className="dropdown-actions">
            <button type="button" onClick={() => onChange([])}>
              Limpiar
            </button>
            <span>{options.length} opciones</span>
          </div>
          <div className="dropdown-options">
            {filtered.map((option) => {
              const value = String(option);
              return (
                <label key={value} className="dropdown-option">
                  <input
                    type="checkbox"
                    checked={selected.includes(value)}
                    onChange={() => toggle(value)}
                  />
                  <span>{value}</span>
                </label>
              );
            })}
            {filtered.length === 0 && (
              <p className="empty-options">Sin coincidencias.</p>
            )}
          </div>
          {options.length > filtered.length && !search && (
            <p className="dropdown-hint">Escribe para acotar la lista.</p>
          )}
        </div>
      </details>
    </div>
  );
}

function DateRange({
  label,
  from,
  to,
  onChange,
}: {
  label: string;
  from: string;
  to: string;
  onChange: (value: { from: string; to: string }) => void;
}) {
  return (
    <div className="filter-field">
      <span className="filter-label">{label}</span>
      <div className="date-range">
        <input
          aria-label={`${label} desde`}
          type="date"
          value={from}
          onChange={(event) => onChange({ from: event.target.value, to })}
        />
        <span>→</span>
        <input
          aria-label={`${label} hasta`}
          type="date"
          value={to}
          onChange={(event) => onChange({ from, to: event.target.value })}
        />
      </div>
    </div>
  );
}
function Loading({ label = "Cargando información…" }: { label?: string }) {
  return (
    <div className="loading-state">
      <span className="loading-dot" />
      {label}
    </div>
  );
}
function ErrorMessage({ message }: { message: string }) {
  return <div className="error-state">{message}</div>;
}
function Pagination({
  page,
  pageSize,
  total,
  onChange,
}: {
  page: number;
  pageSize: number;
  total: number;
  onChange: (page: number) => void;
}) {
  const pages = Math.max(1, Math.ceil(total / pageSize));
  return (
    <div className="pagination">
      <button
        type="button"
        disabled={page <= 1}
        onClick={() => onChange(page - 1)}
      >
        Anterior
      </button>
      <span>
        Página {page} de {pages}
      </span>
      <button
        type="button"
        disabled={page >= pages}
        onClick={() => onChange(page + 1)}
      >
        Siguiente
      </button>
    </div>
  );
}

function CobranzaView({ catalog }: { catalog: PaymentCatalog | null }) {
  const [view, setView] = useState("agent-day");
  const [week, setWeek] = useState<number | undefined>();
  const [data, setData] = useState<Heatmap | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  useEffect(() => {
    if (!week && catalog?.semana[0]) setWeek(catalog.semana[0]);
  }, [catalog, week]);
  useEffect(() => {
    let current = true;
    setLoading(true);
    setError("");
    api
      .heatmap(view, view === "weekly-comparison" ? undefined : week)
      .then((result) => current && setData(result))
      .catch((reason: unknown) => current && setError(getError(reason)))
      .finally(() => current && setLoading(false));
    return () => {
      current = false;
    };
  }, [view, week]);
  return (
    <>
      <section className="card filters-card collection-filters">
        <div className="filter-field">
          <label className="filter-label" htmlFor="collection-view">
            Vista
          </label>
          <select
            id="collection-view"
            value={view}
            onChange={(event) => setView(event.target.value)}
          >
            <option value="agent-day">Agente × día</option>
            <option value="agent-product">Agente × producto</option>
            <option value="weekly-comparison">Comparativo semanal</option>
          </select>
        </div>
        <div className="filter-field">
          <label className="filter-label" htmlFor="collection-week">
            Semana
          </label>
          <select
            id="collection-week"
            disabled={view === "weekly-comparison"}
            value={week ?? ""}
            onChange={(event) => setWeek(Number(event.target.value))}
          >
            {(catalog?.semana ?? []).map((item) => (
              <option key={item} value={item}>
                Semana {item}
              </option>
            ))}
          </select>
        </div>
      </section>
      <section className="card visualization-card">
        <div className="section-title-row">
          <div>
            <p className="section-kicker">Cobranza aplicada</p>
            <h2>
              {view === "agent-day"
                ? "Recuperación diaria por agente"
                : view === "agent-product"
                  ? "Recuperación por agente y producto"
                  : "Recuperación por agente y semana"}
            </h2>
          </div>
          {data && (
            <p className="metric-chip">
              Total: {formatMoney(data.totalRecovery)}
            </p>
          )}
        </div>
        {loading && <Loading />}
        {error && <ErrorMessage message={error} />}
        {!loading &&
          !error &&
          data &&
          (view === "weekly-comparison" ? (
            <WeeklyChart data={data} />
          ) : (
            <HeatmapGrid data={data} />
          ))}
      </section>
    </>
  );
}

function HeatmapGrid({ data }: { data: Heatmap }) {
  const max = Math.max(...data.cells.map((cell) => cell.amount), 1);
  const values = new Map(
    data.cells.map((cell) => [`${cell.row}::${cell.column}`, cell.amount]),
  );
  const gridStyle = {
    gridTemplateColumns: `minmax(210px, 1.2fr) repeat(${data.columns.length}, minmax(112px, 1fr)) minmax(104px, .6fr)`,
  };
  return (
    <div className="heatmap-scroll">
      <div className="heatmap-grid" style={gridStyle}>
        <div className="heatmap-corner">Agente</div>
        {data.columns.map((column) => (
          <div className="heatmap-header" key={column}>
            {column}
          </div>
        ))}
        <div className="heatmap-header total-header">Total</div>
        {data.rows.map((row) => {
          const total = data.columns.reduce(
            (sum, column) => sum + (values.get(`${row}::${column}`) ?? 0),
            0,
          );
          return [
            <div className="heatmap-row-label" key={`${row}-label`}>
              {row}
            </div>,
            ...data.columns.map((column) => {
              const amount = values.get(`${row}::${column}`) ?? 0;
              return (
                <button
                  type="button"
                  className="heatmap-cell"
                  key={`${row}-${column}`}
                  title={`${row} · ${column}: ${formatMoney(amount, true)}`}
                  style={
                    {
                      "--heat": Math.max(0.06, amount / max),
                    } as React.CSSProperties
                  }
                >
                  {amount ? formatMoney(amount) : ""}
                </button>
              );
            }),
            <div className="heatmap-total" key={`${row}-total`}>
              {formatMoney(total)}
            </div>,
          ];
        })}
      </div>
    </div>
  );
}
function WeeklyChart({ data }: { data: Heatmap }) {
  const values = new Map(
    data.cells.map((cell) => [`${cell.row}::${cell.column}`, cell.amount]),
  );
  const chartData = data.rows.map((row) =>
    Object.fromEntries([
      ["agente", row],
      ...data.columns.map((column) => [
        column,
        values.get(`${row}::${column}`) ?? 0,
      ]),
    ]),
  );
  return (
    <div
      className="weekly-chart"
      style={{ height: Math.max(360, data.rows.length * 42) }}
    >
      <ResponsiveContainer width="100%" height="100%">
        <BarChart
          data={chartData}
          layout="vertical"
          margin={{ top: 8, right: 28, left: 8, bottom: 8 }}
          barGap={3}
        >
          <CartesianGrid
            horizontal={false}
            stroke="var(--chart-line)"
            strokeDasharray="3 5"
          />
          <XAxis
            type="number"
            tickFormatter={(value) => formatMoney(Number(value))}
            stroke="var(--chart-muted)"
            fontSize={11}
          />
          <YAxis
            dataKey="agente"
            type="category"
            width={190}
            stroke="var(--chart-muted)"
            fontSize={11}
          />
          <Tooltip
            formatter={(value) => formatMoney(Number(value), true)}
            contentStyle={{
              borderRadius: 12,
              border: "1px solid var(--line)",
              background: "var(--popup)",
              color: "var(--ink)",
            }}
          />
          <Legend wrapperStyle={{ fontSize: 12, paddingBottom: 8 }} />
          {data.columns.map((column, index) => (
            <Bar
              key={column}
              dataKey={column}
              fill={chartColors[index % chartColors.length]}
              radius={[3, 3, 3, 3]}
            />
          ))}
        </BarChart>
      </ResponsiveContainer>
    </div>
  );
}

type PromiseFilters = {
  idPromesa: string[];
  clienteUnico: string[];
  agente: string[];
  tipoPromesa: string[];
  estatus: string[];
  fechaPromesaDesde: string;
  fechaPromesaHasta: string;
  fechaCreacionDesde: string;
  fechaCreacionHasta: string;
};
const emptyPromiseFilters: PromiseFilters = {
  idPromesa: [],
  clienteUnico: [],
  agente: [],
  tipoPromesa: [],
  estatus: [],
  fechaPromesaDesde: "",
  fechaPromesaHasta: "",
  fechaCreacionDesde: "",
  fechaCreacionHasta: "",
};
function PromesasView() {
  const [catalog, setCatalog] = useState<PromiseCatalog | null>(null);
  const [filters, setFilters] = useState<PromiseFilters>(emptyPromiseFilters);
  const [page, setPage] = useState(1);
  const [data, setData] = useState<PromisePage | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  useEffect(() => {
    api
      .promiseCatalog()
      .then(setCatalog)
      .catch((reason: unknown) => setError(getError(reason)));
  }, []);
  useEffect(() => {
    let current = true;
    setLoading(true);
    setError("");
    api
      .promises({ ...filters, page, pageSize: 25 })
      .then((result) => current && setData(result))
      .catch((reason: unknown) => current && setError(getError(reason)))
      .finally(() => current && setLoading(false));
    return () => {
      current = false;
    };
  }, [filters, page]);
  const update = <K extends keyof PromiseFilters>(
    key: K,
    value: PromiseFilters[K],
  ) => {
    setFilters((current) => ({ ...current, [key]: value }));
    setPage(1);
  };
  return (
    <>
      <section className="card filters-card promise-filters">
        <FilterDropdown
          label="Id de promesa"
          options={catalog?.idPromesa ?? []}
          selected={filters.idPromesa}
          onChange={(value) => update("idPromesa", value)}
          allLabel="Todos los IDs"
        />
        <FilterDropdown
          label="Cliente único"
          options={catalog?.clienteUnico ?? []}
          selected={filters.clienteUnico}
          onChange={(value) => update("clienteUnico", value)}
          allLabel="Todos los clientes"
        />
        <FilterDropdown
          label="Agente"
          options={catalog?.agente ?? []}
          selected={filters.agente}
          onChange={(value) => update("agente", value)}
          allLabel="Todos los agentes"
        />
        <FilterDropdown
          label="Tipo de promesa"
          options={catalog?.tipoPromesa ?? []}
          selected={filters.tipoPromesa}
          onChange={(value) => update("tipoPromesa", value)}
          allLabel="Todos los tipos"
        />
        <FilterDropdown
          label="Estatus"
          options={catalog?.estatus ?? []}
          selected={filters.estatus}
          onChange={(value) => update("estatus", value)}
          allLabel="Todos los estatus"
        />
        <DateRange
          label="Fecha de promesa"
          from={filters.fechaPromesaDesde}
          to={filters.fechaPromesaHasta}
          onChange={({ from, to }) => {
            setFilters((current) => ({
              ...current,
              fechaPromesaDesde: from,
              fechaPromesaHasta: to,
            }));
            setPage(1);
          }}
        />
        <DateRange
          label="Fecha de creación"
          from={filters.fechaCreacionDesde}
          to={filters.fechaCreacionHasta}
          onChange={({ from, to }) => {
            setFilters((current) => ({
              ...current,
              fechaCreacionDesde: from,
              fechaCreacionHasta: to,
            }));
            setPage(1);
          }}
        />
      </section>
      <section className="card table-card">
        <div className="section-title-row table-heading">
          <h2>Promesas</h2>
          <div className="table-actions">
            {data && (
              <div className="table-metrics">
                <span>
                  {data.totalRecords.toLocaleString("es-MX")} promesas
                </span>
                <span>
                  Inicial: {formatMoney(data.totalMontoInicial, true)}
                </span>
                <span>
                  Semanal: {formatMoney(data.totalMontoSemanal, true)}
                </span>
              </div>
            )}
            <button
              type="button"
              className="export-button"
              onClick={() =>
                api
                  .promiseExport(filters)
                  .catch((reason: unknown) => setError(getError(reason)))
              }
            >
              Descargar Excel
            </button>
          </div>
        </div>
        {loading && <Loading />}
        {error && <ErrorMessage message={error} />}
        {!loading && !error && data && (
          <>
            <div className="table-scroll">
              <table>
                <thead>
                  <tr>
                    <th>Id de promesa</th>
                    <th>Cliente único</th>
                    <th>Agente</th>
                    <th>Fecha de promesa</th>
                    <th>Monto inicial</th>
                    <th>Monto semanal</th>
                    <th>Tipo</th>
                    <th>Semanas</th>
                    <th>Fecha de creación</th>
                    <th>Estatus</th>
                  </tr>
                </thead>
                <tbody>
                  {data.items.map((item) => (
                    <tr key={item.idPromesa}>
                      <td>{item.idPromesa}</td>
                      <td>{item.clienteUnico ?? "—"}</td>
                      <td>{item.agente}</td>
                      <td>{formatDate(item.fechaPromesa)}</td>
                      <td>{formatMoney(item.montoInicial, true)}</td>
                      <td>{formatMoney(item.montoSemanal, true)}</td>
                      <td>{item.tipoPromesa ?? "—"}</td>
                      <td>{item.numeroSemanas ?? "—"}</td>
                      <td>{formatDate(item.fechaCreacion, true)}</td>
                      <td>
                        <span
                          className={`status-pill status-${item.estatus.toLowerCase().replaceAll(" ", "-")}`}
                        >
                          {item.estatus}
                        </span>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
            <Pagination
              page={data.page}
              pageSize={data.pageSize}
              total={data.totalRecords}
              onChange={setPage}
            />
          </>
        )}
      </section>
    </>
  );
}

type PaymentFilters = {
  clienteUnico: string[];
  agente: string[];
  producto: string[];
  diaSemana: string[];
  semana: string[];
  fechaDesde: string;
  fechaHasta: string;
};
const emptyPaymentFilters: PaymentFilters = {
  clienteUnico: [],
  agente: [],
  producto: [],
  diaSemana: [],
  semana: [],
  fechaDesde: "",
  fechaHasta: "",
};
function PagosView() {
  const [catalog, setCatalog] = useState<PaymentCatalog | null>(null);
  const [filters, setFilters] = useState<PaymentFilters>(emptyPaymentFilters);
  const [page, setPage] = useState(1);
  const [data, setData] = useState<PaymentPage | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  useEffect(() => {
    api
      .paymentCatalog()
      .then(setCatalog)
      .catch((reason: unknown) => setError(getError(reason)));
  }, []);
  useEffect(() => {
    let current = true;
    setLoading(true);
    setError("");
    api
      .payments({
        ...filters,
        semana: filters.semana.map(Number),
        page,
        pageSize: 25,
      })
      .then((result) => current && setData(result))
      .catch((reason: unknown) => current && setError(getError(reason)))
      .finally(() => current && setLoading(false));
    return () => {
      current = false;
    };
  }, [filters, page]);
  const update = <K extends keyof PaymentFilters>(
    key: K,
    value: PaymentFilters[K],
  ) => {
    setFilters((current) => ({ ...current, [key]: value }));
    setPage(1);
  };
  return (
    <>
      <section className="card filters-card payment-filters">
        <FilterDropdown
          label="Cliente único"
          options={catalog?.clienteUnico ?? []}
          selected={filters.clienteUnico}
          onChange={(value) => update("clienteUnico", value)}
          allLabel="Todos los clientes"
        />
        <FilterDropdown
          label="Agente"
          options={catalog?.agente ?? []}
          selected={filters.agente}
          onChange={(value) => update("agente", value)}
          allLabel="Todos los agentes"
        />
        <FilterDropdown
          label="Producto"
          options={catalog?.producto ?? []}
          selected={filters.producto}
          onChange={(value) => update("producto", value)}
          allLabel="Todos los productos"
        />
        <FilterDropdown
          label="Día de la semana"
          options={catalog?.diaSemana ?? []}
          selected={filters.diaSemana}
          onChange={(value) => update("diaSemana", value)}
          allLabel="Todos los días"
        />
        <FilterDropdown
          label="Semana"
          options={catalog?.semana ?? []}
          selected={filters.semana}
          onChange={(value) => update("semana", value)}
          allLabel="Todas las semanas"
        />
        <DateRange
          label="Fecha de recepción"
          from={filters.fechaDesde}
          to={filters.fechaHasta}
          onChange={({ from, to }) => {
            setFilters((current) => ({
              ...current,
              fechaDesde: from,
              fechaHasta: to,
            }));
            setPage(1);
          }}
        />
      </section>
      <section className="card table-card">
        <div className="section-title-row table-heading">
          <h2>Todos los pagos</h2>
          {data && (
            <div className="table-metrics">
              <span>{data.totalRecords.toLocaleString("es-MX")} pagos</span>
              <span>Recuperación: {formatMoney(data.totalAmount, true)}</span>
            </div>
          )}
        </div>
        {loading && <Loading />}
        {error && <ErrorMessage message={error} />}
        {!loading && !error && data && (
          <>
            <div className="table-scroll">
              <table>
                <thead>
                  <tr>
                    <th>Agente</th>
                    <th>Fecha de recepción</th>
                    <th>Cliente único</th>
                    <th>Producto</th>
                    <th>Recuperación por gestión</th>
                    <th>Semana</th>
                    <th>Día</th>
                    <th>Promesa asignada</th>
                    <th>Asignación</th>
                  </tr>
                </thead>
                <tbody>
                  {data.items.map((item) => (
                    <tr key={item.id}>
                      <td>{item.agente}</td>
                      <td>{formatDate(item.fechaRecepcion, true)}</td>
                      <td>{item.clienteUnico ?? "—"}</td>
                      <td>{item.producto ?? "—"}</td>
                      <td className="numeric-cell">
                        {formatMoney(item.recuperacionPorGestion, true)}
                      </td>
                      <td>{item.semana ?? "—"}</td>
                      <td>{item.diaSemana ?? "—"}</td>
                      <td>{item.idPromesaAsignada ?? "—"}</td>
                      <td>{item.tipoAsignacion ?? "—"}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
            <Pagination
              page={data.page}
              pageSize={data.pageSize}
              total={data.totalRecords}
              onChange={setPage}
            />
          </>
        )}
      </section>
    </>
  );
}

function ActividadView() {
  const [date, setDate] = useState("");
  const [data, setData] = useState<PromiseActivity | null>(null);
  const [detail, setDetail] = useState<PromiseActivityDetailItem[] | null>(
    null,
  );
  const [selected, setSelected] = useState<{
    agent: string;
    hour: number;
  } | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  useEffect(() => {
    let current = true;
    setLoading(true);
    setError("");
    setDetail(null);
    setSelected(null);
    api
      .activity(date || undefined)
      .then((result) => {
        if (current) {
          setData(result);
          if (!date) setDate(result.fecha);
        }
      })
      .catch((reason: unknown) => current && setError(getError(reason)))
      .finally(() => current && setLoading(false));
    return () => {
      current = false;
    };
  }, [date]);
  const openDetail = (agent: string, hour: number) => {
    if (!data) return;
    setSelected({ agent, hour });
    setDetail(null);
    api
      .activityDetail(data.fecha, agent, hour)
      .then(setDetail)
      .catch((reason: unknown) => setError(getError(reason)));
  };
  const cells = new Map(
    (data?.celdas ?? []).map((cell) => [`${cell.agente}:${cell.hora}`, cell]),
  );
  const maxPromisesByHour = Math.max(
    ...(data?.celdas ?? []).map((cell) => cell.promesas),
    1,
  );
  return (
    <>
      <section className="card filters-card activity-filters">
        <div className="filter-field">
          <label className="filter-label" htmlFor="activity-date">
            Día de creación
          </label>
          <input
            id="activity-date"
            type="date"
            value={date}
            onChange={(event) => setDate(event.target.value)}
          />
        </div>
      </section>
      <section className="card visualization-card">
        <div className="section-title-row">
          <div>
            <p className="section-kicker">Ritmo operativo</p>
            <h2>Actividad de promesas</h2>
          </div>
          {data && (
            <div className="table-metrics">
              <span>{formatDate(data.fecha)}</span>
              <span>Inicial: {formatMoney(data.totalMontoInicial, true)}</span>
              <span>Semanal: {formatMoney(data.totalMontoSemanal, true)}</span>
            </div>
          )}
        </div>
        {loading && <Loading />}
        {error && <ErrorMessage message={error} />}
        {!loading && !error && data && (
          <div className="activity-scroll">
            <table className="activity-table">
              <thead>
                <tr>
                  <th>Gestor</th>
                  {data.horas.map((hour) => (
                    <th key={hour}>{String(hour).padStart(2, "0")}:00</th>
                  ))}
                </tr>
              </thead>
              <tbody>
                {data.agentes.map((agent) => (
                  <tr key={agent}>
                    <th>{agent}</th>
                    {data.horas.map((hour) => {
                      const cell = cells.get(`${agent}:${hour}`);
                      return (
                        <td key={hour}>
                          <button
                            type="button"
                            disabled={!cell}
                            className={
                              cell ? "activity-cell" : "activity-cell is-empty"
                            }
                            style={
                              cell
                                ? ({
                                    "--activity-intensity":
                                      cell.promesas / maxPromisesByHour,
                                  } as React.CSSProperties)
                                : undefined
                            }
                            onClick={() => openDetail(agent, hour)}
                          >
                            {cell ? (
                              <>
                                <strong>{cell.promesas}</strong>
                                <span>{formatMoney(cell.montoInicial)}</span>
                              </>
                            ) : (
                              "—"
                            )}
                          </button>
                        </td>
                      );
                    })}
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </section>
      {selected && (
        <section className="card detail-card">
          <div className="section-title-row">
            <h2>
              {selected.agent} · {String(selected.hour).padStart(2, "0")}:00
            </h2>
            <span className="metric-chip">Detalle de promesas</span>
          </div>
          {!detail && <Loading label="Cargando detalle…" />}
          {detail && (
            <div className="table-scroll">
              <table>
                <thead>
                  <tr>
                    <th>Id</th>
                    <th>Cliente único</th>
                    <th>Tipo</th>
                    <th>Estatus</th>
                    <th>Inicial</th>
                    <th>Semanal</th>
                    <th>Promesa</th>
                  </tr>
                </thead>
                <tbody>
                  {detail.map((item) => (
                    <tr key={item.idPromesa}>
                      <td>{item.idPromesa}</td>
                      <td>{item.clienteUnico ?? "—"}</td>
                      <td>{item.tipoPromesa ?? "—"}</td>
                      <td>{item.estatus}</td>
                      <td>{formatMoney(item.montoInicial, true)}</td>
                      <td>{formatMoney(item.montoSemanal, true)}</td>
                      <td>{formatDate(item.fechaPromesa)}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </section>
      )}
    </>
  );
}

type RecurrenceFilters = {
  clienteUnico: string[];
  estadoPlan: string[];
  agente: string[];
  estatusDespacho: string[];
};
const emptyRecurrenceFilters: RecurrenceFilters = {
  clienteUnico: [],
  estadoPlan: [],
  agente: [],
  estatusDespacho: [],
};
function RecurrenciaView() {
  const [catalog, setCatalog] = useState<RecurrenceCatalog | null>(null);
  const [filters, setFilters] = useState<RecurrenceFilters>(
    emptyRecurrenceFilters,
  );
  const [data, setData] = useState<Recurrence | null>(null);
  const [detail, setDetail] = useState<RecurrenceDetailItem[] | null>(null);
  const [selected, setSelected] = useState<{
    client: string;
    label: string;
  } | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  useEffect(() => {
    api
      .recurrenceCatalog()
      .then(setCatalog)
      .catch((reason: unknown) => setError(getError(reason)));
  }, []);
  useEffect(() => {
    let current = true;
    setLoading(true);
    setError("");
    setDetail(null);
    setSelected(null);
    api
      .recurrence(filters)
      .then((result) => current && setData(result))
      .catch((reason: unknown) => current && setError(getError(reason)))
      .finally(() => current && setLoading(false));
    return () => {
      current = false;
    };
  }, [filters]);
  const update = <K extends keyof RecurrenceFilters>(
    key: K,
    value: RecurrenceFilters[K],
  ) => setFilters((current) => ({ ...current, [key]: value }));
  const openDetail = (
    client: string,
    year: number,
    week: number,
    label: string,
  ) => {
    setSelected({ client, label });
    setDetail(null);
    api
      .recurrenceDetail(client, year, week)
      .then(setDetail)
      .catch((reason: unknown) => setError(getError(reason)));
  };
  return (
    <>
      <section className="card filters-card recurrence-filters">
        <FilterDropdown
          label="Cliente único"
          options={catalog?.clienteUnico ?? []}
          selected={filters.clienteUnico}
          onChange={(value) => update("clienteUnico", value)}
          allLabel="Todos los clientes"
        />
        <FilterDropdown
          label="Estado del plan"
          options={catalog?.estadoPlan ?? []}
          selected={filters.estadoPlan}
          onChange={(value) => update("estadoPlan", value)}
          allLabel="Todos los estados"
        />
        <FilterDropdown
          label="Agente"
          options={catalog?.agente ?? []}
          selected={filters.agente}
          onChange={(value) => update("agente", value)}
          allLabel="Todos los agentes"
        />
        <FilterDropdown
          label="Estatus despacho"
          options={catalog?.estatusDespacho ?? []}
          selected={filters.estatusDespacho}
          onChange={(value) => update("estatusDespacho", value)}
          allLabel="Activo e inactivo"
        />
      </section>
      <section className="card table-card recurrence-card">
        <div className="section-title-row table-heading">
          <div>
            <p className="section-kicker">Pagos consecutivos</p>
            <h2>Recurrencia</h2>
          </div>
          <div className="table-actions">
            {data && (
              <div className="table-metrics">
                <span>{data.rows.length.toLocaleString("es-MX")} clientes</span>
                <span>
                  Recuperación: {formatMoney(data.totalRecovery, true)}
                </span>
              </div>
            )}
            <button
              type="button"
              className="export-button"
              onClick={() =>
                api
                  .recurrenceExport(filters)
                  .catch((reason: unknown) => setError(getError(reason)))
              }
            >
              Descargar Excel
            </button>
          </div>
        </div>
        {loading && <Loading />}
        {error && <ErrorMessage message={error} />}
        {!loading && !error && data && (
          <div className="table-scroll recurrence-scroll">
            <table>
              <thead>
                <tr>
                  <th>Cliente único</th>
                  <th>Estado del plan</th>
                  <th>Agente</th>
                  <th>Despacho</th>
                  {data.periods.map((period) => (
                    <th key={period.id}>{period.label}</th>
                  ))}
                </tr>
              </thead>
              <tbody>
                {data.rows.map((row) => (
                  <tr key={row.clienteUnico}>
                    <td>{row.clienteUnico}</td>
                    <td>{row.estadoPlan}</td>
                    <td>{row.agente}</td>
                    <td>
                      <span
                        className={`status-pill ${row.estatusDespacho === "Activo" ? "status-activa" : "status-incumplida"}`}
                      >
                        {row.estatusDespacho}
                      </span>
                    </td>
                    {data.periods.map((period) => {
                      const amount = row.payments[period.id];
                      return (
                        <td key={period.id}>
                          {amount === null || amount === undefined ? (
                            <span className="no-payment">—</span>
                          ) : (
                            <button
                              type="button"
                              className="recurrence-payment"
                              onClick={() =>
                                openDetail(
                                  row.clienteUnico,
                                  period.year,
                                  period.week,
                                  period.label,
                                )
                              }
                            >
                              {formatMoney(amount, true)}
                            </button>
                          )}
                        </td>
                      );
                    })}
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </section>
      {selected && (
        <section className="card detail-card">
          <div className="section-title-row">
            <h2>
              {selected.client} · {selected.label}
            </h2>
            <span className="metric-chip">Detalle de promesa</span>
          </div>
          {!detail && <Loading label="Cargando detalle…" />}
          {detail && (
            <div className="table-scroll">
              <table>
                <thead>
                  <tr>
                    <th>Pago</th>
                    <th>Fecha</th>
                    <th>Producto</th>
                    <th>Recuperación</th>
                    <th>Agente</th>
                    <th>Promesa</th>
                    <th>Tipo</th>
                    <th>Estatus</th>
                    <th>Inicial</th>
                    <th>Semanal</th>
                  </tr>
                </thead>
                <tbody>
                  {detail.map((item) => (
                    <tr key={item.paymentId}>
                      <td>{item.paymentId}</td>
                      <td>{formatDate(item.fechaRecepcion, true)}</td>
                      <td>{item.producto ?? "—"}</td>
                      <td>{formatMoney(item.recuperacionPorGestion, true)}</td>
                      <td>{item.agente}</td>
                      <td>{item.idPromesaAsignada ?? "Orgánico"}</td>
                      <td>{item.tipoPromesa ?? "—"}</td>
                      <td>{item.estatusPromesa ?? "—"}</td>
                      <td>{formatMoney(item.montoInicial, true)}</td>
                      <td>{formatMoney(item.montoSemanal, true)}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </section>
      )}
    </>
  );
}

function App() {
  const [authenticated, setAuthenticated] = useState(() =>
    api.hasCredentials(),
  );
  const [tab, setTab] = useState<TabId>("cobranza");
  const [dark, setDark] = useState(false);
  const [paymentCatalog, setPaymentCatalog] = useState<PaymentCatalog | null>(
    null,
  );
  useEffect(() => {
    document.body.classList.toggle("ascendra-dark", dark);
  }, [dark]);
  useEffect(() => {
    if (authenticated)
      api
        .paymentCatalog()
        .then(setPaymentCatalog)
        .catch(() => undefined);
  }, [authenticated]);
  if (!authenticated)
    return <LoginScreen onSuccess={() => setAuthenticated(true)} />;
  return (
    <main className="app-shell">
      <header className="card dashboard-header">
        <div className="brand-block">
          <div className="brand-logo-frame">
            <img
              className="brand-logo"
              src="/assets/xolo-wolf.png"
              alt="Xolo"
            />
          </div>
          <div>
            <p className="brand-eyebrow">GESTIÓN DE COBRANZA</p>
            <h1>Ascendra</h1>
            <p className="dashboard-subtitle">
              Recuperación, promesas y actividad de tu equipo
            </p>
          </div>
        </div>
        <div className="header-actions">
          <span className="workspace-badge">
            <span className="workspace-mark" />
            Cobranza Xolo
          </span>
          <button
            className="theme-toggle"
            type="button"
            onClick={() => setDark((value) => !value)}
          >
            {dark ? "Modo claro" : "Modo oscuro"}
          </button>
          <button
            className="sign-out"
            type="button"
            onClick={() => {
              api.clearCredentials();
              setAuthenticated(false);
            }}
          >
            Salir
          </button>
        </div>
      </header>
      <nav className="main-tabs card" aria-label="Secciones de Ascendra">
        {tabs.map((item) => (
          <button
            type="button"
            key={item.id}
            onClick={() => setTab(item.id)}
            className={tab === item.id ? "nav-tab nav-tab-selected" : "nav-tab"}
          >
            {item.label}
          </button>
        ))}
      </nav>
      {tab === "cobranza" && <CobranzaView catalog={paymentCatalog} />}
      {tab === "promesas" && <PromesasView />}
      {tab === "pagos" && <PagosView />}
      {tab === "actividad" && <ActividadView />}
      {tab === "recurrencia" && <RecurrenciaView />}
      <footer>
        Ascendra · Cobranza Xolo{" "}
        <span>Datos en tiempo real desde SQL Server</span>
      </footer>
    </main>
  );
}
export default App;
