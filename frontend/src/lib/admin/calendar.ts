export type MonthRef = { year: number; month: number };
export type GridDay = { key: string; day: number; inMonth: boolean };

const pad = (value: number) => String(value).padStart(2, "0");
const isoDay = (date: Date) => `${date.getUTCFullYear()}-${pad(date.getUTCMonth() + 1)}-${pad(date.getUTCDate())}`;

/** Diferença (ms) entre o horário de parede no fuso e UTC naquele instante. */
function zoneOffset(instant: number, timeZone: string): number {
  const parts = Object.fromEntries(
    new Intl.DateTimeFormat("en-US", {
      timeZone,
      hourCycle: "h23",
      year: "numeric",
      month: "2-digit",
      day: "2-digit",
      hour: "2-digit",
      minute: "2-digit",
      second: "2-digit",
    })
      .formatToParts(new Date(instant))
      .map((part) => [part.type, part.value]),
  );
  const wall = Date.UTC(+parts.year, +parts.month - 1, +parts.day, +parts.hour, +parts.minute, +parts.second);
  return wall - Math.floor(instant / 1000) * 1000;
}

/** "2026-10-20" + "10:30" no fuso informado → instante UTC em ISO. */
export function zonedToUtcIso(date: string, time: string, timeZone: string): string {
  const [year, month, day] = date.split("-").map(Number);
  const [hour, minute] = time.split(":").map(Number);
  const wall = Date.UTC(year, month - 1, day, hour, minute);
  let instant = wall - zoneOffset(wall, timeZone);
  instant = wall - zoneOffset(instant, timeZone); // segunda passada acerta mudanças de horário
  return new Date(instant).toISOString();
}

/** Dia do calendário (aaaa-mm-dd) de um instante no fuso informado. */
export function dayKey(iso: string, timeZone: string): string {
  return new Intl.DateTimeFormat("en-CA", { timeZone, year: "numeric", month: "2-digit", day: "2-digit" }).format(new Date(iso));
}

/** Semanas completas (domingo a sábado) que cobrem o mês. */
export function monthGrid(year: number, month: number): GridDay[] {
  const first = new Date(Date.UTC(year, month - 1, 1));
  const last = new Date(Date.UTC(year, month, 0));
  const start = new Date(first);
  start.setUTCDate(1 - first.getUTCDay());
  const end = new Date(last);
  end.setUTCDate(last.getUTCDate() + (6 - last.getUTCDay()));

  const days: GridDay[] = [];
  for (const cursor = new Date(start); cursor <= end; cursor.setUTCDate(cursor.getUTCDate() + 1)) {
    days.push({ key: isoDay(cursor), day: cursor.getUTCDate(), inMonth: cursor.getUTCMonth() === month - 1 });
  }
  return days;
}

/** Intervalo [from, to) em UTC para buscar as visitas exibidas na grade. */
export function gridRange(days: GridDay[], timeZone: string): { from: string; to: string } {
  const lastDay = new Date(`${days[days.length - 1].key}T00:00:00Z`);
  lastDay.setUTCDate(lastDay.getUTCDate() + 1);
  return { from: zonedToUtcIso(days[0].key, "00:00", timeZone), to: zonedToUtcIso(isoDay(lastDay), "00:00", timeZone) };
}

export function shiftMonth({ year, month }: MonthRef, delta: number): MonthRef {
  const index = year * 12 + (month - 1) + delta;
  return { year: Math.floor(index / 12), month: (index % 12) + 1 };
}

/** Mês atual no fuso informado. */
export function currentMonth(timeZone: string, now = new Date()): MonthRef {
  const [year, month] = dayKey(now.toISOString(), timeZone).split("-").map(Number);
  return { year, month };
}

/** Hora de parede (HH:mm) de um instante no fuso informado. */
export function timeKey(iso: string, timeZone: string): string {
  return new Intl.DateTimeFormat("en-GB", { timeZone, hour: "2-digit", minute: "2-digit", hourCycle: "h23" }).format(new Date(iso));
}
