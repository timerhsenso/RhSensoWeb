/* app-modal.js */
window.AppModal = (function () {
  const $formModal = $('#formModal');
  const $formTitle = $('#formModalLabel');
  const $formBody  = $('#formModalBody');
  const $saveBtn   = $('#saveFormBtn');
  const $confirmModal = $('#confirmModal');
  const $confirmMsg   = $('#confirmMessage');
  const $confirmBtn   = $('#confirmActionBtn');

  function openConfirm(message = 'Tem certeza?') {
    return new Promise((resolve) => {
      let decided = false;
      $confirmMsg.text(message);
      $confirmBtn.off('click').on('click', () => {
        decided = true;
        $confirmModal.modal('hide');
        resolve(true);
      });
      $confirmModal.off('hidden.bs.modal').on('hidden.bs.modal', () => {
        if (!decided) resolve(false);
      });
      $confirmModal.modal('show');
    });
  }

  function openForm({ title = 'Formulário', getUrl, postUrl, onSuccess, onError } = {}) {
    $formTitle.text(title);
    $formBody.html(`<div class="d-flex justify-content-center my-4">
        <div class="spinner-border" role="status"></div>
        <span class="visualmente-oculto">Carregando...</span>
      </div>`);
    $formModal.data('post-url', postUrl || null);
    $saveBtn.prop('disabled', true);
    $formModal.modal('show');

    $.get(getUrl).done(html => {
      $formBody.html(html);
      if ($.validator && $.validator.unobtrusive) $.validator.unobtrusive.parse($formBody.find('form'));
      $saveBtn.prop('disabled', false);
    }).fail((xhr) => {
      $formBody.html('<div class="alert alert-danger">Não foi possível carregar o formulário.</div>');
      window.AppAjax && AppAjax.handleError(xhr);
    });

    $saveBtn.off('click').on('click', () => {
      const $form = $formBody.find('form').first();
      if (!$form.length) return;
      if ($.fn.validate && $form.data('validator') && !$form.valid()) return;

      const action = ($form.attr('action') || $formModal.data('post-url') || '').trim();
      if (!action) { window.AppNotifier ? AppNotifier.warn('Ação do formulário não definida.') : alert('Ação do formulário não definida.'); return; }
      const method = ($form.attr('method') || 'POST').toUpperCase();
      const isMultipart = ($form.attr('enctype') || '').toLowerCase() === 'multipart/form-data';

      let data, contentType, processData;
      if (isMultipart) { data = new FormData($form[0]); contentType = false; processData = false; }
      else { data = $form.serialize(); contentType = 'application/x-www-form-urlencoded; charset=UTF-8'; processData = true; }

      $saveBtn.prop('disabled', true);
      $.ajax({ url: action, type: method, data, contentType, processData })
        .done((res) => {
          if (res && res.success === false) {
            (window.AppNotifier ? AppNotifier.warn(res.message || 'Falha ao salvar.') : alert(res.message || 'Falha ao salvar.'));
            $saveBtn.prop('disabled', false);
            return;
          }
          $formModal.modal('hide');
          if (typeof onSuccess === 'function') onSuccess(res);
          window.AppNotifier && AppNotifier.success('Salvo com sucesso.');
        })
        .fail((xhr) => {
          window.AppAjax ? AppAjax.handleError(xhr) : alert('Erro ao salvar.');
          if (typeof onError === 'function') onError(xhr);
        })
        .always(() => $saveBtn.prop('disabled', false));
    });
  }

  return { confirm: openConfirm, form: openForm };
})();
