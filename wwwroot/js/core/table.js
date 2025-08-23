export function createCrudTable(selector, { ajaxUrl, columns, options, onReady } = {}) {
  const dt = $(selector).DataTable(Object.assign({
    ajax: { url: ajaxUrl, dataSrc: 'data' },
    processing: true, responsive: true, order: [], searching: true,
    columns: columns || []
  }, options || {}));
  if (typeof onReady === 'function') onReady(dt);
  return dt;
}