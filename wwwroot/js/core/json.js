export function pick(obj, ...keys) {
  for (const k of keys) if (obj && obj[k] !== undefined) return obj[k];
  return undefined;
}
export function isJsonResponse(res) {
  const ct = res.headers.get('content-type') || '';
  return ct.includes('application/json');
}