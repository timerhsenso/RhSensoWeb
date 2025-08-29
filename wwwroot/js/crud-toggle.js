/* ========================================================================
   crud-toggle.js
   Handler global para toggles de "Ativo/Inativo" (checkbox .crud-toggle)
   ------------------------------------------------------------------------
   • Seguro para páginas sem toggle (só reage a elementos .crud-toggle)
   • Trava o switch durante a requisição (disabled + aria-busy + .is-loading)
   • Reverte visualmente em erro/timeout/429 (Rate Limit)
   • Suporta PK simples (data-id) e PK composta (data-k1/data-k2)
   • Envia Anti-CSRF pelo header RequestVerificationToken (meta no <head>)
   • Usa JSON do servidor: { success, message, noop } e retryAfter no 429
   • Opcional: integra com window.notify?.success/error (se existir)
   ======================================================================== */
(() => {
    // Evita registrar o listener mais de uma vez (layout global)
    if (window.__crudToggleBound) return;
    window.__crudToggleBound = true;

    // ==========================
    // Configuração central
    // ==========================
    const CFG = {
        TIMEOUT_MS: 6000,          // aborta requisição lenta
        TOAST_SUCCESS: (msg) => window.notify?.success?.(msg) ?? void 0,
        TOAST_ERROR: (msg) => window.notify?.error?.(msg) ?? void 0,
        // Quando true, mostra toast só quando houve alteração real (não NO-OP)
        TOAST_ONLY_ON_CHANGE: true,
        // Classe visual aplicada enquanto processa (estilize via CSS se quiser)
        LOADING_CLASS: "is-loading"
    };

    // ==========================
    // Estado do cliente
    // ==========================
    const inflight = new Set();        // chaves em voo (1 req por registro)
    const uiCooldown = new Map();      // cooldown visual por registro (ms epoch)

    // ==========================
    // Utilidades
    // ==========================
    const now = () => Date.now();

    function token() {
        // Meta gerado no layout: <meta name="request-verification-token" ... />
        return document.querySelector('meta[name="request-verification-token"]')?.content || "";
    }

    function isCooling(key) {
        return (uiCooldown.get(key) ?? 0) > now();
    }

    function startCooldown(key, ms) {
        uiCooldown.set(key, now() + ms);
    }

    function getKeyFromEl(el) {
        // Suporta PK simples (data-id) e composta (data-k1/data-k2)
        const id = el.dataset.id;
        if (id) return id;

        const k1 = el.dataset.k1, k2 = el.dataset.k2;
        if (k1 && k2) return `${k1}|${k2}`;

        return null;
    }

    function buildFormData(el, desired) {
        const fd = new FormData();

        // Mapeia automaticamente os campos de acordo com os data-*
        if (el.dataset.id) {
            fd.append("id", el.dataset.id);
        } else if (el.dataset.k1 && el.dataset.k2) {
            // Padrão Taux2 (PK composta)
            fd.append("cdtptabela", el.dataset.k1);
            fd.append("cdsituacao", el.dataset.k2);
        } else {
            // Sem identificadores => nada a fazer
            return null;
        }

        fd.append("ativo", String(desired));
        return fd;
    }

    function setLoading(el, on) {
        el.disabled = on;
        el.setAttribute("aria-busy", on ? "true" : "false");
        if (CFG.LOADING_CLASS) el.classList.toggle(CFG.LOADING_CLASS, on);
    }

    function parseRetryAfter(r, json) {
        // Prioriza JSON.retryAfter; fallback para header Retry-After
        if (json && Number.isFinite(json.retryAfter)) return Math.max(1, json.retryAfter);

        const header = r.headers?.get("Retry-After");
        const n = header ? Number(header) : NaN;
        if (Number.isFinite(n) && n > 0) return Math.max(1, n);

        return 2; // default
    }

    async function postWithTimeout(url, options, timeoutMs) {
        const ac = new AbortController();
        const tId = setTimeout(() => ac.abort("timeout"), timeoutMs);
        try {
            const resp = await fetch(url, { ...options, signal: ac.signal });
            return resp;
        } finally {
            clearTimeout(tId);
        }
    }

    // ==========================
    // Listener único (delegado)
    // ==========================
    document.addEventListener(
        "change",
        async (ev) => {
            const el = ev.target;
            if (!el || !el.matches(".crud-toggle")) return;

            const url = el.dataset.url;
            if (!url) return; // sem URL, não aciona

            const key = getKeyFromEl(el);
            if (!key) return; // sem PK, ignora

            // Se há cooldown visual, reverte e sai
            if (isCooling(key)) {
                el.checked = !el.checked;
                CFG.TOAST_ERROR?.("Aguarde o intervalo mínimo.");
                return;
            }

            // Evita duas requisições simultâneas para o MESMO registro
            if (inflight.has(key)) {
                el.checked = !el.checked; // reverte ação duplicada
                return;
            }

            const desired = el.checked;
            const fd = buildFormData(el, desired);
            if (!fd) {
                // Sem dados suficientes para enviar; reverte
                el.checked = !desired;
                return;
            }

            inflight.add(key);
            setLoading(el, true);

            try {
                const resp = await postWithTimeout(
                    url,
                    {
                        method: "POST",
                        headers: { "RequestVerificationToken": token() },
                        body: fd
                    },
                    CFG.TIMEOUT_MS
                );

                // Tenta ler JSON; pode falhar se backend não retornar JSON
                let data = {};
                try { data = await resp.json(); } catch { /* noop */ }

                // Rate limit (429): reverte, inicia cooldown visual e informa
                if (resp.status === 429) {
                    const retryAfter = parseRetryAfter(resp, data);
                    startCooldown(key, retryAfter * 1000);
                    el.checked = !desired;

                    // Mensagem amigável (se houve)
                    const msg = data?.message || `Muitas tentativas. Tente novamente em ${retryAfter}s.`;
                    CFG.TOAST_ERROR?.(msg);
                    return;
                }

                // Demais erros HTTP ou sucesso=false do backend
                if (!resp.ok || data?.success === false) {
                    el.checked = !desired; // reverte
                    CFG.TOAST_ERROR?.(data?.message || `Erro ${resp.status}`);
                    return;
                }

                // Sucesso: só notifica se realmente mudou (não NO-OP)
                if (!data?.noop && CFG.TOAST_ONLY_ON_CHANGE) {
                    if (data?.message) CFG.TOAST_SUCCESS?.(data.message);
                }

            } catch (err) {
                // Timeout/erro de rede => reverte
                el.checked = !desired;
                CFG.TOAST_ERROR?.(err?.message || "Falha na conexão. Tente novamente.");
            } finally {
                setLoading(el, false);
                inflight.delete(key);
            }
        },
        { passive: true }
    );
})();
