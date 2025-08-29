/* crud-toggle.js — handler global, seguro p/ páginas sem toggle */
(() => {
    if (window.__crudToggleBound) return;        // evita bind duplicado
    window.__crudToggleBound = true;

    const token = () =>
        document.querySelector('meta[name="request-verification-token"]')?.content || '';

    const inflight = new Set();                  // 1 req por ID
    const uiCooldown = new Map();                // cooldown visual por ID
    const TIMEOUT = 6000;

    const now = () => Date.now();
    const isCooling = id => (uiCooldown.get(id) ?? 0) > now();
    const startCooldown = (id, ms) => uiCooldown.set(id, now() + ms);

    document.addEventListener('change', async (ev) => {
        const el = ev.target;
        if (!el.matches('.crud-toggle')) return;   // páginas sem toggle: NO-OP

        const id = el.dataset.id;
        const url = el.dataset.url;
        if (!id || !url) return;                   // só atua quando há dados

        if (isCooling(id) || inflight.has(id)) {   // bloqueia spam local
            el.checked = !el.checked;                // reverte visualmente
            return;
        }

        const desired = el.checked;
        inflight.add(id);
        el.disabled = true;

        const fd = new FormData();
        fd.append('id', id);
        fd.append('ativo', String(desired));

        const ac = new AbortController();
        const tId = setTimeout(() => ac.abort('timeout'), TIMEOUT);

        try {
            const r = await fetch(url, {
                method: 'POST',
                headers: { 'RequestVerificationToken': token() },
                body: fd,
                signal: ac.signal
            });
            const j = await r.json().catch(() => ({}));

            if (r.status === 429) {                  // rate limit
                const ms = ((j?.retryAfter ?? 2) * 1000);
                startCooldown(id, ms);
                el.checked = !desi
                red;
                // opcional: notify?.error?.(j?.message || 'Muitas tentativas. Aguarde.');
                return;
            }

            if (!r.ok || j?.success === false) {     // erro normal
                el.checked = !desired;
                // opcional: notify?.error?.(j?.message || `Erro ${r.status}`);
                return;
            }

            // sucesso: se quiser, mostre toast quando não for NO-OP
            // if (!j?.noop) notify?.success?.(j?.message || 'Alterado com sucesso');

        } catch {
            el.checked = !desired;                   // timeout/rede => reverte
        } finally {
            clearTimeout(tId);
            el.disabled = false;
            inflight.delete(id);
        }
    }, { passive: true });
})();
