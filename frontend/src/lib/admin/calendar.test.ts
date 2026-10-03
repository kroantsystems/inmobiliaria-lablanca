import { describe, expect, it } from "vitest";
import { dayKey, gridRange, monthGrid, shiftMonth, timeKey, zonedToUtcIso } from "./calendar";

describe("zonedToUtcIso", () => {
  it("converts Asunción wall time (UTC-3) to UTC", () => {
    expect(zonedToUtcIso("2026-10-20", "10:30", "America/Asuncion")).toBe("2026-10-20T13:30:00.000Z");
  });

  it("handles times that cross midnight in UTC", () => {
    expect(zonedToUtcIso("2026-12-31", "22:00", "America/Asuncion")).toBe("2027-01-01T01:00:00.000Z");
  });

  it("works for other zones", () => {
    expect(zonedToUtcIso("2026-07-01", "09:00", "America/Sao_Paulo")).toBe("2026-07-01T12:00:00.000Z");
  });
});

describe("dayKey", () => {
  it("returns the calendar day in the business time zone", () => {
    expect(dayKey("2026-10-21T01:30:00Z", "America/Asuncion")).toBe("2026-10-20");
    expect(dayKey("2026-10-21T04:00:00Z", "America/Asuncion")).toBe("2026-10-21");
  });
});

describe("monthGrid", () => {
  it("starts on Sunday and fills whole weeks", () => {
    const days = monthGrid(2026, 10);
    expect(days[0]).toEqual({ key: "2026-09-27", day: 27, inMonth: false });
    expect(days.find((d) => d.key === "2026-10-01")).toEqual({ key: "2026-10-01", day: 1, inMonth: true });
    expect(days.length % 7).toBe(0);
    expect(days.at(-1)).toEqual({ key: "2026-10-31", day: 31, inMonth: true });
    expect(days.filter((d) => d.inMonth)).toHaveLength(31);
  });

  it("adds the next month's days to complete the last week", () => {
    const days = monthGrid(2026, 2);
    expect(days[0].key).toBe("2026-02-01");
    expect(days.at(-1)).toEqual({ key: "2026-02-28", day: 28, inMonth: true });
    const march = monthGrid(2026, 3);
    expect(march.at(-1)).toEqual({ key: "2026-04-04", day: 4, inMonth: false });
  });
});

describe("gridRange", () => {
  it("covers the first to the day after the last grid day in Asunción time", () => {
    expect(gridRange(monthGrid(2026, 10), "America/Asuncion")).toEqual({ from: "2026-09-27T03:00:00.000Z", to: "2026-11-01T03:00:00.000Z" });
  });
});

describe("shiftMonth", () => {
  it("moves across years", () => {
    expect(shiftMonth({ year: 2026, month: 12 }, 1)).toEqual({ year: 2027, month: 1 });
    expect(shiftMonth({ year: 2026, month: 1 }, -1)).toEqual({ year: 2025, month: 12 });
  });
});

describe("timeKey", () => {
  it("returns the wall-clock time in the business time zone", () => {
    expect(timeKey("2026-10-20T13:30:00Z", "America/Asuncion")).toBe("10:30");
    expect(timeKey("2026-10-21T02:05:00Z", "America/Asuncion")).toBe("23:05");
  });
});
