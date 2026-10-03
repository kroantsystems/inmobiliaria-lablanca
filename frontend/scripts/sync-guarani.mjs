// Mantém messages/gn.json com as mesmas chaves de es.json.
// Chaves novas recebem o texto em espanhol até um falante nativo traduzir; gn-pending.md lista o que falta.
import fs from "node:fs";
import path from "node:path";

const root = path.resolve(import.meta.dirname, "..");
const read = (locale) => JSON.parse(fs.readFileSync(path.join(root, "messages", `${locale}.json`), "utf8"));
const es = read("es");
const gnFile = path.join(root, "messages", "gn.json");
const gn = fs.existsSync(gnFile) ? read("gn") : {};
const pending = [];

function sync(source, target, prefix) {
  const result = {};
  for (const [key, value] of Object.entries(source)) {
    const fullKey = prefix ? `${prefix}.${key}` : key;
    if (value && typeof value === "object") {
      result[key] = sync(value, target?.[key] ?? {}, fullKey);
    } else {
      const translated = typeof target?.[key] === "string" ? target[key] : value;
      result[key] = translated;
      if (translated === value) pending.push(fullKey);
    }
  }
  return result;
}

fs.writeFileSync(gnFile, JSON.stringify(sync(es, gn, ""), null, 2) + "\n", "utf8");
fs.writeFileSync(
  path.join(root, "gn-pending.md"),
  [
    "# Traduções pendentes para guarani (gn)",
    "",
    "Estas chaves de `messages/gn.json` ainda estão com o texto em espanhol e precisam de revisão por falante nativo.",
    "Depois de traduzir em `messages/gn.json`, rode `node scripts/sync-guarani.mjs` para atualizar esta lista.",
    "Mensagens da API ficam em `backend/src/LaBlanca.Shared/Resources/Messages.gn.resx` (também em espanhol por enquanto).",
    "",
    `Total pendente: ${pending.length}`,
    "",
    ...pending.map((k) => `- \`${k}\``),
    "",
  ].join("\n"),
  "utf8",
);
console.log(`gn.json sincronizado (${pending.length} chaves pendentes)`);
