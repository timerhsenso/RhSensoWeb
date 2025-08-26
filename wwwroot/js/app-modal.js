/* app-modal.js
 * Revisões:
 *  - Compatível com Bootstrap 5 (usa API `bootstrap.Modal`, com fallback para jQuery `.modal()` se existir)
 *  - Modal de formulário inicia com skeleton e é centralizado/scrollável sempre
 *  - Corrigido spinner: classe certa é `visually-hidden`
 *  - Armazena e lê a rota de POST com chave consistente: `postUrl` (evita hífens no .data)
 *  - Aceita `getUrl` como string OU função
 *  - Re-parse de validação unobtrusive após carregar a partial
 *  - Normaliza JSON PascalCase/camelCase (`Success/success`, `Message/message`) para não depender de política de casing
 *  - Trata 400 com HTML (re-renderiza form) e erros de ModelState (`errors`)
 *  - Evita handlers duplicados no botão salvar e no confirm
 */

(function (w) {
    "use strict";

    // ---------- Helpers Bootstrap/jQuery ----------
    function bsInstance($el) {
        try {
            if (w.bootstrap && w.bootstrap.Modal && $el && $el[0]) {
                return w.bootstrap.Modal.getOrCreateInstance($el[0]);
            }
        } catch (_) { /* ignore */ }
        return null;
    }
    function showModal($el) {
        const bs = bsInstance($el);
        if (bs) bs.show(); else if ($el && $el.modal) $el.modal('show');
    }
    function hideModal($el) {
        const bs = bsInstance($el);
        if (bs) bs.hide(); else if ($el && $el.modal) $el.modal('hide');
    }
    function onHiddenOnce($el, cb) {
        // funciona tanto com BS5 quanto com jQuery plugin
        const handler = () => {
            $el.off('hidden.bs.modal', handler);
            if (typeof cb === 'function') cb();
        };
        $el.off('hidden.bs.modal', handler).on('hidden.bs.modal', handler);
    }

    // ---------- Notifier / Ajax handlers opcionais ----------
    const Notifier = {
        ok: (msg) => w.AppNotifier ? w.AppNotifier.success(msg) : alert(msg),
        warn: (msg) => w.AppNotifier ? w.AppNotifier.warn(msg) : alert(msg),
        err: (msg) => w.AppNotifier ? w.AppNotifier.error(msg) : alert(msg),
    };
    const Ajax = {
        handle: (xhr) => w.AppAjax ? w.AppAjax.handleError(xhr) : Notifier.err('Erro inesperado.')
    };

    // ---------- Normalização de resposta (PascalCase/camelCase) ----------
    function getBool(obj, k) {
        if (!obj) return false;
        if (k in obj) return !!obj[k];
        const alt = k[0].toUpperCase() + k.slice(1);
        if (alt in obj) return !!obj[alt];
        return false;
    }
    function getStr(obj, k, def = '') {
        if (!obj) return def;
        if (k in obj && obj[k] != null) return String(obj[k]);
        const alt = k[0].toUpperCase() + k.slice(1);
        if (alt in obj && obj[alt] != null) return String(obj[alt]);
        return def;
    }
    function getObj(obj, k) {
        if (!obj) return null;
        if (k in obj) return obj[k];
        const alt = k[0].toUpperCase() + k.slice(1);
        return (alt in obj) ? obj[alt] : null;
    }

    // ---------- Elementos ----------
    const $formModal = $('#formModal');
    const $formTitle = $('#formModalLabel');
    const $formBody = $('#formModalBody');
    const $saveBtn = $('#saveFormBtn');

    const $confirmModal = $('#confirmModal');
    const $confirmMsg = $('#confirmMessage');
    const $confirmBtn = $('#confirmActionBtn');

    // Garante classes de diálogo (centralizado/scroll/size) em toda abertura
    function ensureDialogClasses() {
        $formModal.find('.modal-dialog')
            .addClass('modal-lg modal-dialog-scrollable modal-dialog-centered');
    }

    // ---------- Confirm ----------
    function openConfirm(message = 'Tem certeza?') {
        return new Promise((resolve) => {
            let decided = false;

            $confirmMsg.text(message);

            // click confirma
            $confirmBtn.off('click').on('click', () => {
                decided = true;
                hideModal($confirmModal);
                resolve(true);
            });

            // fechar por X / ESC / backdrop => false
            onHiddenOnce($confirmModal, () => {
                if (!decided) resolve(false);
            });

            showModal($confirmModal);
        });
    }

    // ---------- Renderiza erros de ModelState no form ----------
    function renderModelState($form, errors) {
        try {
            $form.find('.is-invalid').removeClass('is-invalid');
            $form.find('.invalid-feedback').remove();

            if (!errors) return;

            for (const [key, msgs] of Object.entries(errors)) {
                const $input = $form.find(`[name="${key}"]`);
                if ($input.length) {
                    $input.addClass('is-invalid');
                    if ($input.next('.invalid-feedback').length === 0) {
                        $input.after(`<div class="invalid-feedback">${(msgs || []).join('<br>')}</div>`);
                    }
                }
            }
        } catch (_) { /* noop */ }
    }

    // ---------- Form ----------
    function openForm({ title = 'Formulário', getUrl, postUrl, onSuccess, onError } = {}) {
        // título + skeleton inicial
        $formTitle.text(title);
        $formBody.html(`
      <div class="d-flex justify-content-center align-items-center gap-2 my-4">
        <div class="spinner-border" role="status" aria-hidden="true"></div>
        <span class="visually-hidden">Carregando...</span>
      </div>
    `);

        // centraliza e habilita scroll sempre
        ensureDialogClasses();

        // usa uma única chave consistente no .data()
        $formModal.data('postUrl', postUrl || null);

        // evita submit antes de carregar
        $saveBtn.prop('disabled', true);

        // abre o modal
        showModal($formModal);

        // aceita getUrl string OU função
        const url = (typeof getUrl === 'function') ? getUrl() : getUrl;

        $.get(url)
            .done(html => {
                $formBody.html(html);

                // re-parse unobtrusive validation, se presente
                if ($.validator && $.validator.unobtrusive) {
                    const $form = $formBody.find('form');
                    $.validator.unobtrusive.parse($form);
                }

                $saveBtn.prop('disabled', false);

                // foco primeiro campo
                setTimeout(() => {
                    $formBody.find('input,select,textarea,button')
                        .filter(':visible:enabled:not([type=hidden])')
                        .first().trigger('focus');
                }, 50);
            })
            .fail(xhr => {
                $formBody.html('<div class="alert alert-danger mb-0">Não foi possível carregar o formulário.</div>');
                Ajax.handle(xhr);
            });

        // handler do botão salvar (idempotente)
        $saveBtn.off('click').on('click', () => {
            const $form = $formBody.find('form').first();
            if (!$form.length) return;

            // validação client-side se disponível
            if ($.fn.validate && $form.data('validator') && !$form.valid()) return;

            // rota de POST: action do form > postUrl passado por parâmetro
            const action = ($form.attr('action') || $formModal.data('postUrl') || '').trim();
            if (!action) {
                Notifier.warn('Ação do formulário não definida.');
                return;
            }

            const method = ($form.attr('method') || 'POST').toUpperCase();
            const isMultipart = ($form.attr('enctype') || '').toLowerCase() === 'multipart/form-data';

            // Anti-forgery: tanto faz header ou form-field; se existir input hidden, já vai no serialize
            const token = $form.find('input[name="__RequestVerificationToken"]').val();

            let data, contentType, processData;
            if (isMultipart) {
                data = new FormData($form[0]);
                contentType = false;
                processData = false;
            } else {
                data = $form.serialize();
                contentType = 'application/x-www-form-urlencoded; charset=UTF-8';
                processData = true;
            }

            // loading no botão
            $saveBtn.prop('disabled', true);
            const removeSpinner = (() => {
                $saveBtn.append('<span class="spinner-border spinner-border-sm ms-2"></span>');
                return () => $saveBtn.find('.spinner-border').remove();
            })();

            $.ajax({
                url: action,
                type: method,
                data,
                contentType,
                processData,
                headers: token ? { 'RequestVerificationToken': token } : {}
            })
                .done(res => {
                    // normaliza `success/message/errors`
                    const ok = getBool(res, 'success');
                    const msg = getStr(res, 'message', ok ? 'Salvo com sucesso.' : 'Falha ao salvar.');
                    const errors = getObj(res, 'errors');

                    // se o servidor devolveu falha sem HTML, mostra e mantém modal aberto
                    if (ok === false) {
                        if (errors) renderModelState($form, errors);
                        Notifier.warn(msg);
                        return;
                    }

                    // sucesso => fecha modal e chama callback
                    hideModal($formModal);
                    if (typeof onSuccess === 'function') onSuccess(res);
                    Notifier.ok(msg);
                })
                .fail(xhr => {
                    // se veio 400 com HTML (partial de validação), re-renderiza o corpo
                    const ctype = (xhr.getResponseHeader('content-type') || '').toLowerCase();
                    if (xhr.status === 400 && ctype.includes('text/html') && xhr.responseText) {
                        $formBody.html(xhr.responseText);
                        // re-parse validação
                        if ($.validator && $.validator.unobtrusive) {
                            const $form2 = $formBody.find('form');
                            $.validator.unobtrusive.parse($form2);
                        }
                        Notifier.warn('Corrija os campos destacados.');
                    } else {
                        Ajax.handle(xhr);
                        if (typeof onError === 'function') onError(xhr);
                    }
                })
                .always(() => {
                    removeSpinner();
                    $saveBtn.prop('disabled', false);
                });
        });
    }

    // ---------- Exports ----------
    w.AppModal = {
        confirm: openConfirm,
        form: openForm
    };

})(window);
