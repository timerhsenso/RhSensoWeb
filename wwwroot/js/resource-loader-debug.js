/**
 * Resource Loader Debug Tool v1.0
 * Monitora o carregamento de recursos CSS e JavaScript
 * Compatível com ASP.NET Core e outras aplicações web
 * 
 * @author Desenvolvedor
 * @license MIT
 */

(function (window, document) {
    'use strict';

    // Configuração global
    const ResourceDebugger = {
        config: {
            showConsoleOutput: true,
            showVisualIndicator: true,
            timeout: 30000,
            excludePatterns: [
                /chrome-extension:/,
                /moz-extension:/,
                /webpack-dev-server/,
                /hot-update/
            ]
        },

        resources: new Map(),
        stats: {
            total: 0,
            loaded: 0,
            errors: 0,
            timeouts: 0,
            pending: 0
        },

        initialized: false,
        indicator: null,
        startTime: Date.now()
    };

    // Utilitários
    const Utils = {
        log: function (message, type = 'info') {
            if (!ResourceDebugger.config.showConsoleOutput) return;

            const timestamp = new Date().toLocaleTimeString();
            const prefix = {
                info: '📋',
                success: '✅',
                warning: '⚠️',
                error: '❌',
                loading: '⏳'
            }[type] || '📋';

            console.log(`[${timestamp}] ${prefix} ResourceDebug: ${message}`);
        },

        shouldExclude: function (url) {
            return ResourceDebugger.config.excludePatterns.some(pattern =>
                pattern instanceof RegExp ? pattern.test(url) : url.includes(pattern)
            );
        },

        createIndicator: function () {
            if (!ResourceDebugger.config.showVisualIndicator || ResourceDebugger.indicator) return;

            const indicator = document.createElement('div');
            indicator.id = 'resource-debug-indicator';
            indicator.innerHTML = `
                <div style="
                    position: fixed;
                    top: 20px;
                    right: 20px;
                    z-index: 10000;
                    background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
                    color: white;
                    padding: 12px 18px;
                    border-radius: 25px;
                    font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
                    font-size: 12px;
                    font-weight: 500;
                    box-shadow: 0 8px 25px rgba(102, 126, 234, 0.3);
                    cursor: pointer;
                    transition: all 0.3s cubic-bezier(0.4, 0, 0.2, 1);
                    min-width: 140px;
                    text-align: center;
                    backdrop-filter: blur(10px);
                    border: 1px solid rgba(255,255,255,0.1);
                " onmouseover="this.style.transform='translateY(-2px) scale(1.05)'; this.style.boxShadow='0 12px 35px rgba(102, 126, 234, 0.4)'" 
                   onmouseout="this.style.transform='translateY(0) scale(1)'; this.style.boxShadow='0 8px 25px rgba(102, 126, 234, 0.3)'">
                    <div id="debug-status" style="font-weight: 600;">🔄 Carregando...</div>
                    <div style="font-size: 10px; opacity: 0.9; margin-top: 4px; font-weight: 400;">
                        <span id="debug-progress">0/0</span> recursos
                    </div>
                </div>
            `;

            indicator.addEventListener('click', ResourceDebugger.showReport);
            document.body.appendChild(indicator);
            ResourceDebugger.indicator = indicator;
        },

        updateIndicator: function () {
            if (!ResourceDebugger.indicator) return;

            const stats = ResourceDebugger.getStats();
            const statusEl = document.getElementById('debug-status');
            const progressEl = document.getElementById('debug-progress');
            const containerEl = ResourceDebugger.indicator.firstElementChild;

            if (statusEl && progressEl && containerEl) {
                if (stats.pending > 0) {
                    statusEl.innerHTML = '🔄 Carregando...';
                    containerEl.style.background = 'linear-gradient(135deg, #3498db 0%, #2980b9 100%)';
                } else if (stats.errors > 0 || stats.timeouts > 0) {
                    statusEl.innerHTML = '❌ Erros encontrados';
                    containerEl.style.background = 'linear-gradient(135deg, #e74c3c 0%, #c0392b 100%)';
                } else {
                    statusEl.innerHTML = '✅ Concluído';
                    containerEl.style.background = 'linear-gradient(135deg, #27ae60 0%, #229954 100%)';
                }

                progressEl.textContent = `${stats.loaded + stats.errors + stats.timeouts}/${stats.total}`;
            }
        },

        formatTime: function (ms) {
            if (ms < 1000) return `${ms}ms`;
            return `${(ms / 1000).toFixed(1)}s`;
        },

        getFileNameFromUrl: function (url) {
            return url.split('/').pop().split('?')[0] || 'arquivo-sem-nome';
        }
    };

    // Monitor de recursos
    const ResourceMonitor = {
        trackResource: function (element, type) {
            const url = type === 'css' ? element.href : element.src;

            if (!url || Utils.shouldExclude(url)) return;

            const resourceId = `${type}_${url}_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`;
            const resource = {
                id: resourceId,
                url: url,
                type: type,
                element: element,
                startTime: Date.now(),
                status: 'loading',
                loadTime: null,
                error: null,
                fileName: Utils.getFileNameFromUrl(url)
            };

            ResourceDebugger.resources.set(resourceId, resource);
            ResourceDebugger.stats.total++;
            ResourceDebugger.stats.pending++;

            Utils.log(`Iniciando carregamento: ${resource.fileName}`, 'loading');
            Utils.updateIndicator();

            // Event listeners
            const onLoad = () => {
                resource.status = 'loaded';
                resource.loadTime = Date.now() - resource.startTime;
                ResourceDebugger.stats.loaded++;
                ResourceDebugger.stats.pending--;

                Utils.log(`✅ Carregado (${Utils.formatTime(resource.loadTime)}): ${resource.fileName}`, 'success');
                Utils.updateIndicator();

                element.removeEventListener('load', onLoad);
                element.removeEventListener('error', onError);
            };

            const onError = (event) => {
                resource.status = 'error';
                resource.error = event.message || 'Falha no carregamento do arquivo';
                resource.loadTime = Date.now() - resource.startTime;
                ResourceDebugger.stats.errors++;
                ResourceDebugger.stats.pending--;

                Utils.log(`❌ Erro (${Utils.formatTime(resource.loadTime)}): ${resource.fileName} - ${resource.error}`, 'error');
                Utils.updateIndicator();

                element.removeEventListener('load', onLoad);
                element.removeEventListener('error', onError);
            };

            element.addEventListener('load', onLoad);
            element.addEventListener('error', onError);

            // Timeout handler
            const timeoutId = setTimeout(() => {
                if (resource.status === 'loading') {
                    resource.status = 'timeout';
                    resource.loadTime = Date.now() - resource.startTime;
                    resource.error = `Timeout após ${Utils.formatTime(ResourceDebugger.config.timeout)}`;
                    ResourceDebugger.stats.timeouts++;
                    ResourceDebugger.stats.pending--;

                    Utils.log(`⏰ Timeout (${Utils.formatTime(resource.loadTime)}): ${resource.fileName}`, 'warning');
                    Utils.updateIndicator();

                    element.removeEventListener('load', onLoad);
                    element.removeEventListener('error', onError);
                }
            }, ResourceDebugger.config.timeout);

            // Limpar timeout se carregar com sucesso
            const originalOnLoad = onLoad;
            const originalOnError = onError;

            const newOnLoad = () => {
                clearTimeout(timeoutId);
                originalOnLoad();
            };

            const newOnError = (event) => {
                clearTimeout(timeoutId);
                originalOnError(event);
            };

            element.removeEventListener('load', onLoad);
            element.removeEventListener('error', onError);
            element.addEventListener('load', newOnLoad);
            element.addEventListener('error', newOnError);

            return resource;
        },

        scanExistingResources: function () {
            Utils.log('🔍 Escaneando recursos existentes...');

            // CSS existente
            const cssLinks = document.querySelectorAll('link[rel="stylesheet"]');
            cssLinks.forEach(link => {
                this.trackResource(link, 'css');
            });

            // JS existente
            const scripts = document.querySelectorAll('script[src]');
            scripts.forEach(script => {
                this.trackResource(script, 'js');
            });

            Utils.log(`📊 Encontrados ${cssLinks.length} arquivos CSS e ${scripts.length} arquivos JS`);
        },

        observeNewResources: function () {
            const observer = new MutationObserver(mutations => {
                mutations.forEach(mutation => {
                    mutation.addedNodes.forEach(node => {
                        if (node.nodeType === Node.ELEMENT_NODE) {
                            if (node.tagName === 'LINK' && node.rel === 'stylesheet') {
                                this.trackResource(node, 'css');
                            } else if (node.tagName === 'SCRIPT' && node.src) {
                                this.trackResource(node, 'js');
                            }

                            // Verificar filhos também
                            if (node.querySelectorAll) {
                                node.querySelectorAll('link[rel="stylesheet"]').forEach(child => {
                                    this.trackResource(child, 'css');
                                });
                                node.querySelectorAll('script[src]').forEach(child => {
                                    this.trackResource(child, 'js');
                                });
                            }
                        }
                    });
                });
            });

            observer.observe(document.head, {
                childList: true,
                subtree: true
            });
            observer.observe(document.body, {
                childList: true,
                subtree: true
            });

            Utils.log('👀 Monitoramento de novos recursos ativado');
        }
    };

    // API pública do ResourceDebugger
    ResourceDebugger.init = function (options = {}) {
        if (this.initialized) {
            Utils.log('ResourceDebugger já foi inicializado', 'warning');
            return;
        }

        // Aplicar configurações
        Object.assign(this.config, options);

        Utils.log('🚀 Inicializando Resource Debugger...');
        Utils.createIndicator();

        // Escanear recursos existentes
        ResourceMonitor.scanExistingResources();

        // Observar novos recursos
        ResourceMonitor.observeNewResources();

        this.initialized = true;
        Utils.log(`✅ Resource Debugger inicializado! Monitorando ${this.stats.total} recursos.`);

        // Verificação periódica de conclusão
        const checkInterval = 1000;
        const checkComplete = setInterval(() => {
            if (this.stats.pending === 0 && this.stats.total > 0) {
                clearInterval(checkComplete);
                const totalTime = Date.now() - this.startTime;
                Utils.log(`🎯 Carregamento concluído em ${Utils.formatTime(totalTime)}`);

                if (this.stats.errors > 0 || this.stats.timeouts > 0) {
                    Utils.log(`⚠️ ${this.stats.errors + this.stats.timeouts} recursos falharam - clique no indicador para detalhes`, 'warning');
                }
            }
        }, checkInterval);

        // Limpar interval após 5 minutos para evitar vazamentos
        setTimeout(() => clearInterval(checkComplete), 300000);
    };

    ResourceDebugger.getStats = function () {
        return { ...this.stats };
    };

    ResourceDebugger.getResources = function () {
        return Array.from(this.resources.values());
    };

    ResourceDebugger.showReport = function () {
        const resources = this.getResources();
        const stats = this.getStats();
        const totalTime = Date.now() - this.startTime;

        // Separar recursos por status
        const loaded = resources.filter(r => r.status === 'loaded');
        const errors = resources.filter(r => r.status === 'error');
        const timeouts = resources.filter(r => r.status === 'timeout');
        const pending = resources.filter(r => r.status === 'loading');

        let report = `
🔍 RELATÓRIO DETALHADO DE RECURSOS
===================================

📊 RESUMO ESTATÍSTICO:
• Total de recursos: ${stats.total}
• Carregados com sucesso: ${stats.loaded} ✅
• Erros de carregamento: ${stats.errors} ❌
• Timeouts: ${stats.timeouts} ⏰
• Ainda pendentes: ${stats.pending} 🔄
• Tempo total de análise: ${Utils.formatTime(totalTime)}

`;

        if (loaded.length > 0) {
            report += `\n✅ RECURSOS CARREGADOS COM SUCESSO (${loaded.length}):\n`;
            loaded.forEach(r => {
                report += `  • ${r.fileName} (${r.type.toUpperCase()}) - ${Utils.formatTime(r.loadTime)}\n`;
            });
        }

        if (errors.length > 0) {
            report += `\n❌ RECURSOS COM ERRO (${errors.length}):\n`;
            errors.forEach(r => {
                report += `  • ${r.fileName} (${r.type.toUpperCase()}) - ${r.error}\n    URL: ${r.url}\n`;
            });
        }

        if (timeouts.length > 0) {
            report += `\n⏰ RECURSOS COM TIMEOUT (${timeouts.length}):\n`;
            timeouts.forEach(r => {
                report += `  • ${r.fileName} (${r.type.toUpperCase()}) - ${r.error}\n    URL: ${r.url}\n`;
            });
        }

        if (pending.length > 0) {
            report += `\n🔄 RECURSOS AINDA CARREGANDO (${pending.length}):\n`;
            pending.forEach(r => {
                const elapsedTime = Date.now() - r.startTime;
                report += `  • ${r.fileName} (${r.type.toUpperCase()}) - ${Utils.formatTime(elapsedTime)} decorridos\n`;
            });
        }

        report += `\n💡 DICAS:
• Use F12 → Network para ver detalhes de rede
• Recursos com erro podem afetar a funcionalidade da página
• Timeouts podem indicar problemas de conectividade
• Para mais informações, verifique o console do navegador
        `;

        console.log(report);
        alert(report);
    };

    ResourceDebugger.exportReport = function () {
        const resources = this.getResources();
        const stats = this.getStats();
        const totalTime = Date.now() - this.startTime;

        const data = {
            timestamp: new Date().toISOString(),
            userAgent: navigator.userAgent,
            url: window.location.href,
            totalAnalysisTime: totalTime,
            stats: stats,
            resources: resources.map(r => ({
                fileName: r.fileName,
                url: r.url,
                type: r.type,
                status: r.status,
                loadTime: r.loadTime,
                error: r.error,
                startTime: new Date(r.startTime).toISOString()
            })),
            config: this.config,
            performance: {
                navigationStart: performance.timing.navigationStart,
                domContentLoadedEventEnd: performance.timing.domContentLoadedEventEnd,
                loadEventEnd: performance.timing.loadEventEnd
            }
        };

        console.log('JSON Report:', JSON.stringify(data, null, 2));
        return data;
    };

    // Expor globalmente
    window.ResourceDebugger = ResourceDebugger;

})(window, document);
