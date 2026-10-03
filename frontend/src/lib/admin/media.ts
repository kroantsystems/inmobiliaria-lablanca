/** URL autenticada do admin a partir da pública (/api/public/media/{id} → /api/admin/files/{id}/content). */
export const adminMediaUrl = (publicUrl: string) => publicUrl.replace(/^\/api\/public\/media\/([^/?#]+).*$/, "/api/admin/files/$1/content");
