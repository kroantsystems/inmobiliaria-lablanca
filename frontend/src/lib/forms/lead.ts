// zod/mini: mesma validação com uma fração do tamanho; o formulário aparece em todas as páginas (newsletter).
import * as z from "zod/mini";
import type { LeadSource } from "@/lib/api/types";

export type LeadMessages = { required: string; invalidEmail: string; invalidPhone: string; consentRequired: string };

const EMAIL = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

export function leadSchema(source: LeadSource, messages: LeadMessages) {
  return z.object({
    name: z.string().check(z.trim(), z.minLength(1, messages.required), z.maxLength(120)),
    phone: z.string().check(z.refine((value) => (value.match(/\d/g)?.length ?? 0) >= 8, messages.invalidPhone)),
    // Obrigatório só na newsletter; nos outros formulários pode ficar vazio, mas se preenchido precisa ser válido.
    email: z
      .string()
      .check(
        z.trim(),
        z.refine((value) => source !== "Newsletter" || value.length > 0, messages.required),
        z.refine((value) => value === "" || EMAIL.test(value), messages.invalidEmail),
      ),
    message: z.optional(z.string().check(z.maxLength(2000))),
    consent: z.boolean().check(z.refine((value) => value, messages.consentRequired)),
    website: z.optional(z.string()),
  });
}

export type LeadValues = z.infer<ReturnType<typeof leadSchema>>;
