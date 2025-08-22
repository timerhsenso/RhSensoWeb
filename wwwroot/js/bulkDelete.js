
    (function () {
          if (!window.DT) window.DT = { };

          /**
           * Exclusão em massa (chame a partir do seu dropdown)
           * @@param {object} cfg
    *  - tableId: id da tabela (ex.: 'tblSistemas')
    *  - deleteBaseUrl: base do endpoint Delete (ex.: '/SEG/Tsistema/Delete')
    *  - method: 'POST' | 'DELETE' (default 'POST')
    *  - itemSelector: seletor dos checkboxes de linha (default '.row-check')
    *  - masterSelector: seletor do checkbox mestre (default '#checkAll')
    */
    window.DT.bulkDelete = async function (cfg) {
            const {
        tableId,
        deleteBaseUrl,
        method = 'POST',
        itemSelector = '.row-check',
        masterSelector = '#checkAll'
    } = cfg || { };

    const api = window.dataTable || (tableId ? $('#' + tableId).DataTable() : null);
    if (!api) {showToast({ title: 'Aviso', message: 'Tabela não inicializada.', variant: 'info' }); return; }

    // IDs marcados (página atual) + trim
    const nodes = api.rows({page: 'current' }).nodes();
    const ids = $(nodes).find(itemSelector + ':checked')
              .map((_, el) => String(el.value || '').trim())
    .get();

    if (!ids.length) {
        showToast({ title: 'Atenção', message: 'Selecione ao menos um registro.', variant: 'warning' }); return;

            }

    // Confirmação
    try {
              if (window.confirmModal) {
        await window.confirmModal({
            title: 'Excluir selecionados',
            message: `Você está prestes a excluir <b>${ids.length}</b> registro(s). Deseja continuar?`,
            okText: 'Excluir',
            cancelText: 'Cancelar',
            okClass: 'btn-danger'
        });
              } else if (!window.confirm(`Excluir ${ids.length} registro(s)?`)) {
                return;
              }
            } catch { return; } // cancelado

    // Cabeçalhos AJAX; AntiForgery vai no BODY
    const headers = {
        'X-Requested-With': 'XMLHttpRequest',
    'Content-Type': 'application/x-www-form-urlencoded; charset=UTF-8'
            };
    const afToken = (typeof anti === 'function'
    ? anti()
    : (document.querySelector('input[name="__RequestVerificationToken"]')?.value || '')
    );

    let ok = 0, fail = 0, lastError = '';

    // Exclui 1 a 1 (sequencial)
    for (const id of ids) {
              const url = `${deleteBaseUrl}/${encodeURIComponent(id)}`;
    try {
                // eslint-disable-next-line no-await-in-loop
                const res = await fetch(url, {
        method,
        headers,
        credentials: 'same-origin',
    body: new URLSearchParams({'__RequestVerificationToken': afToken })
                });

    if (!res.ok) {
        let body = '';
    try {body = await res.text(); } catch { }
    if (res.status === 429) body = body || 'Muitas operações em sequência. Aguarde alguns segundos e tente novamente.';
    fail++;
    lastError = `${res.status} ${res.statusText}${body ? ' — ' + body : ''}`;
    console.error('[BulkDelete] Falha', {id, url, status: res.status, statusText: res.statusText, body });
                } else {
        ok++;
    console.log('[BulkDelete] OK', {id, url});
                }
              } catch (err) {
        fail++;
    lastError = (err && err.message) || 'Erro desconhecido';
    console.error('[BulkDelete] Exceção', {id, url, err});
              }
            }

    // Limpa o "selecionar todos" e recarrega mantendo estado
    if (masterSelector) {
              const master = document.querySelector(masterSelector);
    if (master) {master.checked = false; master.indeterminate = false; }
            }
    api.ajax.reload(null, false);

    // Feedback
    if (fail === 0) {
        showToast({
            title: 'Sucesso',
            message: `${ok} registro(s) excluído(s) com sucesso.`,
            variant: 'success'
        });
            } else if (ok === 0) {
        showToast({
            title: 'Erro',
            message: `Nenhum registro excluído. ${lastError || ''}`.trim(),
            variant: 'danger'
        });
            } else {
        showToast({
            title: 'Parcial',
            message: `Excluídos: ${ok} • Falharam: ${fail}${lastError ? ` — ${lastError}` : ''}`,
            variant: 'warning'
        });
            }
          };
        })();
