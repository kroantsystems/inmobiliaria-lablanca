// Gera favicon.ico, apple-icon.png e os ícones do manifest a partir dos SVGs da marca.
// Uso: npm run icons (usa o sharp que já vem com o Next).
import fs from "node:fs";
import path from "node:path";
import sharp from "sharp";

const root = path.resolve(import.meta.dirname, "..");
const favicon = fs.readFileSync(path.join(root, "src/app/icon.svg"));
const mark = fs.readFileSync(path.join(root, "public/brand/brand-mark.svg"));
const BLUE = "#005DAA";

const png = (svg, size) => sharp(svg, { density: 1200 }).resize(size, size, { fit: "contain", background: { r: 0, g: 0, b: 0, alpha: 0 } }).png().toBuffer();

/** Marca centralizada sobre fundo azul (ícones opacos exigidos por iOS/Android). */
async function onBlue(size, markRatio, radius = 0) {
  const inner = Math.round(size * markRatio);
  const background = Buffer.from(
    `<svg xmlns="http://www.w3.org/2000/svg" width="${size}" height="${size}"><rect width="${size}" height="${size}" rx="${radius}" fill="${BLUE}"/></svg>`,
  );
  return sharp(background)
    .composite([{ input: await png(mark, inner), gravity: "center" }])
    .png()
    .toBuffer();
}

/** ICO com PNGs embutidos (16, 32, 48). */
function ico(images) {
  const header = Buffer.alloc(6);
  header.writeUInt16LE(0, 0);
  header.writeUInt16LE(1, 2);
  header.writeUInt16LE(images.length, 4);
  let offset = 6 + images.length * 16;
  const entries = images.map(({ size, data }) => {
    const entry = Buffer.alloc(16);
    entry.writeUInt8(size >= 256 ? 0 : size, 0);
    entry.writeUInt8(size >= 256 ? 0 : size, 1);
    entry.writeUInt8(0, 2);
    entry.writeUInt8(0, 3);
    entry.writeUInt16LE(1, 4);
    entry.writeUInt16LE(32, 6);
    entry.writeUInt32LE(data.length, 8);
    entry.writeUInt32LE(offset, 12);
    offset += data.length;
    return entry;
  });
  return Buffer.concat([header, ...entries, ...images.map((i) => i.data)]);
}

const icoImages = await Promise.all([16, 32, 48].map(async (size) => ({ size, data: await png(favicon, size) })));
fs.writeFileSync(path.join(root, "src/app/favicon.ico"), ico(icoImages));
fs.writeFileSync(path.join(root, "src/app/apple-icon.png"), await onBlue(180, 0.72));
fs.writeFileSync(path.join(root, "public/icons/icon-192.png"), await onBlue(192, 0.72, 36));
fs.writeFileSync(path.join(root, "public/icons/icon-512.png"), await onBlue(512, 0.72, 96));
// Maskable: a área segura é o círculo de 80%; a marca ocupa ~56% do lado.
fs.writeFileSync(path.join(root, "public/icons/icon-maskable-512.png"), await onBlue(512, 0.56));
console.log("Ícones gerados.");
