export const SIMULATOR_TERMS = [5, 10, 15, 20] as const;

/** Parcela fixa pela tabela Price (sistema francês). Devolve null para valores inválidos. */
export function monthlyPayment(principal: number, annualRatePercent: number, years: number): number | null {
  if (!Number.isFinite(principal) || principal <= 0 || years <= 0) return null;
  const months = years * 12;
  const rate = annualRatePercent / 100 / 12;
  if (rate === 0) return principal / months;
  const factor = Math.pow(1 + rate, months);
  return (principal * rate * factor) / (factor - 1);
}
