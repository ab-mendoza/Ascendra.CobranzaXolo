export type TabId =
  | "cobranza"
  | "promesas"
  | "pagos"
  | "actividad"
  | "recurrencia";

export interface PromiseItem {
  idPromesa: string;
  clienteUnico: string | null;
  agente: string;
  fechaPromesa: string | null;
  montoInicial: number | null;
  montoSemanal: number | null;
  tipoPromesa: string | null;
  numeroSemanas: number | null;
  fechaCreacion: string | null;
  estatus: string;
}

export interface PromisePage {
  items: PromiseItem[];
  page: number;
  pageSize: number;
  totalRecords: number;
  totalMontoInicial: number;
  totalMontoSemanal: number;
}

export interface PaymentItem {
  id: number;
  agente: string;
  fechaRecepcion: string | null;
  clienteUnico: string | null;
  producto: string | null;
  recuperacionPorGestion: number;
  semana: number | null;
  diaSemana: string | null;
  idPromesaAsignada: string | null;
  tipoAsignacion: string | null;
}

export interface PaymentPage {
  items: PaymentItem[];
  page: number;
  pageSize: number;
  totalRecords: number;
  totalAmount: number;
}

export interface PromiseCatalog {
  idPromesa: string[];
  clienteUnico: string[];
  agente: string[];
  tipoPromesa: string[];
  estatus: string[];
}

export interface PaymentCatalog {
  clienteUnico: string[];
  agente: string[];
  producto: string[];
  diaSemana: string[];
  semana: number[];
}

export interface RecurrenceCatalog {
  clienteUnico: string[];
  estadoPlan: string[];
  agente: string[];
  estatusDespacho: string[];
}

export interface HeatmapCell {
  row: string;
  column: string;
  amount: number;
}

export interface Heatmap {
  view: string;
  week: number | null;
  rows: string[];
  columns: string[];
  cells: HeatmapCell[];
  totalRecovery: number;
}

export interface RecurrencePeriod {
  id: string;
  label: string;
  year: number;
  week: number;
}

export interface RecurrenceRow {
  clienteUnico: string;
  estatusDespacho: string;
  agente: string;
  estadoPlan: string;
  payments: Record<string, number | null>;
}

export interface Recurrence {
  periods: RecurrencePeriod[];
  rows: RecurrenceRow[];
  totalRecovery: number;
}

export interface RecurrenceDetailItem {
  paymentId: number;
  clienteUnico: string;
  fechaRecepcion: string;
  producto: string | null;
  recuperacionPorGestion: number;
  estatusDespacho: string;
  estadoPlan: string;
  agente: string;
  idPromesaAsignada: string | null;
  fechaPromesa: string | null;
  fechaCreacion: string | null;
  tipoPromesa: string | null;
  estatusPromesa: string | null;
  montoInicial: number | null;
  montoSemanal: number | null;
  numeroSemanas: number | null;
}

export interface PromiseActivityCell {
  agente: string;
  hora: number;
  promesas: number;
  montoInicial: number;
  montoSemanal: number;
}

export interface PromiseActivity {
  fecha: string;
  agentes: string[];
  horas: number[];
  celdas: PromiseActivityCell[];
  totalMontoInicial: number;
  totalMontoSemanal: number;
}

export interface PromiseActivityDetailItem {
  idPromesa: string;
  clienteUnico: string | null;
  agente: string;
  fechaCreacion: string;
  fechaPromesa: string | null;
  tipoPromesa: string | null;
  estatus: string;
  montoInicial: number | null;
  montoSemanal: number | null;
  numeroSemanas: number | null;
}
