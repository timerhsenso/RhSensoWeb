// wwwroot/js/confirm-modal.js
(function () {
    // evita redefinir se já existir
    if (window.confirmModal) return;

    function ensureModalEl() {
        const el = document.getElementById('appConfirmModal');
        if (!el) throw new Error('Modal de confirmação (#appConfirmModal) não encontrado no DOM.');
        return el;
    }

    window.confirmModal = function (options = {}) {
        // busca o elemento apenas quando a função é chamada
        let el;
        try { el = ensureModalEl(); } catch (e) {
            // fallback: se o modal ainda não está no DOM, usa window.confirm
            const ok = window.confirm(options.message || 'Você tem certeza?');
            return ok ? Promise.resolve(true) : Promise.reject(new Error('cancelled'));
        }

        // garante Bootstrap (quando faltar, também cai no confirm nativo)
        const Bs = window.bootstrap && window.bootstrap.Modal;
        if (!Bs) {
            const ok = window.confirm(options.message || 'Você tem certeza?');
            return ok ? Promise.resolve(true) : Promise.reject(new Error('cancelled'));
        }

        const bsModal = window.bootstrap.Modal.getOrCreateInstance(el);

        const o = {
            title: 'Confirmação',
            message: 'Você tem certeza?',
            okText: 'OK',
            cancelText: 'Cancelar',
            okClass: 'btn-primary',
            ...options
        };

        const title = el.querySelector('.modal-title');
        const body = el.querySelector('.modal-body');
        const okBtn = el.querySelector('[data-role="ok"]') || el.querySelector('.modal-footer .btn:not(.btn-secondary):not(.btn-link)');
        const cancelBtn = el.querySelector('[data-role="cancel"]') || el.querySelector('.modal-footer .btn.btn-secondary');
        const closeBtn = el.querySelector('.btn-close');

        if (!okBtn || !cancelBtn) {
            return Promise.reject(new Error('Modal de confirmação não está configurado corretamente.'));
        }

        if (title) title.textContent = o.title;
        if (body) body.innerHTML = o.message;

        // normaliza classe do botão OK
        const variants = [
            'btn-primary', 'btn-secondary', 'btn-success', 'btn-danger', 'btn-warning',
            'btn-info', 'btn-dark', 'btn-light',
            'btn-outline-primary', 'btn-outline-secondary', 'btn-outline-success', 'btn-outline-danger',
            'btn-outline-warning', 'btn-outline-info', 'btn-outline-dark', 'btn-outline-light'
        ];
        okBtn.classList.add('btn');
        variants.forEach(v => okBtn.classList.remove(v));
        o.okClass.split(/\s+/).forEach(c => c && okBtn.classList.add(c));
        okBtn.textContent = o.okText;
        cancelBtn.textContent = o.cancelText;

        return new Promise((resolve, reject) => {
            let decided = false;

            const cleanup = () => {
                okBtn.removeEventListener('click', onOk);
                cancelBtn.removeEventListener('click', onCancel);
                closeBtn && closeBtn.removeEventListener('click', onCancel);
                el.removeEventListener('hidden.bs.modal', onHide);
            };

            const onOk = () => { if (decided) return; decided = true; cleanup(); bsModal.hide(); resolve(true); };
            const onCancel = () => { if (decided) return; decided = true; cleanup(); bsModal.hide(); reject(new Error('cancelled')); };
            const onHide = () => { if (decided) return; decided = true; cleanup(); reject(new Error('cancelled')); };

            okBtn.addEventListener('click', onOk);
            cancelBtn.addEventListener('click', onCancel);
            closeBtn && closeBtn.addEventListener('click', onCancel);
            el.addEventListener('hidden.bs.modal', onHide);

            bsModal.show();
        });
    };
})();
