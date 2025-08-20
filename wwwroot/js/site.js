/**
 * SGCWeb - JavaScript Customizado
 * Funcionalidades interativas e utilitários para o sistema
 */

// ===== CONFIGURAÇÕES GLOBAIS =====
const SGCWeb = {
    config: {
        toastDuration: 5000,
        animationDuration: 300,
        ajaxTimeout: 30000,
        debounceDelay: 300
    },
    
    // Cache para elementos DOM frequentemente utilizados
    cache: {
        $window: $(window),
        $document: $(document),
        $body: $('body'),
        $sidebar: $('.main-sidebar'),
        $content: $('.content-wrapper')
    }
};

// ===== INICIALIZAÇÃO =====
$(document).ready(function() {
    SGCWeb.init();
});

SGCWeb.init = function() {
    console.log('SGCWeb: Inicializando sistema...');
    
    // Inicializa componentes
    this.initSidebar();
    this.initToasts();
    this.initForms();
    this.initTables();
    this.initModals();
    this.initTooltips();
    this.initAjaxSetup();
    this.initLoadingOverlay();
    this.initPermissionButtons();
    
    console.log('SGCWeb: Sistema inicializado com sucesso!');
};

// ===== GERENCIAMENTO DO SIDEBAR =====
SGCWeb.initSidebar = function() {
    const $sidebar = this.cache.$sidebar;
    const $body = this.cache.$body;
    
    // Toggle do sidebar
    $(document).on('click', '[data-widget="pushmenu"]', function(e) {
        e.preventDefault();
        $body.toggleClass('sidebar-collapse');
        
        // Salva estado no localStorage
        const isCollapsed = $body.hasClass('sidebar-collapse');
        localStorage.setItem('sidebar-collapsed', isCollapsed);
    });
    
    // Restaura estado do sidebar
    const sidebarCollapsed = localStorage.getItem('sidebar-collapsed');
    if (sidebarCollapsed === 'true') {
        $body.addClass('sidebar-collapse');
    }
    
    // Submenu toggle
    $(document).on('click', '.nav-link[data-toggle="treeview"]', function(e) {
        e.preventDefault();
        const $this = $(this);
        const $parent = $this.parent();
        const $treeview = $parent.find('.nav-treeview').first();
        
        if ($parent.hasClass('menu-open')) {
            $treeview.slideUp(SGCWeb.config.animationDuration, function() {
                $parent.removeClass('menu-open');
            });
        } else {
            $treeview.slideDown(SGCWeb.config.animationDuration, function() {
                $parent.addClass('menu-open');
            });
        }
    });
    
    // Marca item ativo no menu
    this.setActiveMenuItem();
};

SGCWeb.setActiveMenuItem = function() {
    const currentPath = window.location.pathname;
    $('.nav-sidebar .nav-link').each(function() {
        const $this = $(this);
        const href = $this.attr('href');
        
        if (href && currentPath.startsWith(href) && href !== '/') {
            $this.addClass('active');
            
            // Abre submenu se necessário
            const $parent = $this.closest('.has-treeview');
            if ($parent.length) {
                $parent.addClass('menu-open');
                $parent.find('.nav-treeview').show();
            }
        }
    });
};

// ===== SISTEMA DE TOASTS =====
SGCWeb.initToasts = function() {
    // Container para toasts
    if (!$('#toast-container').length) {
        $('body').append('<div id="toast-container" class="position-fixed" style="top: 20px; right: 20px; z-index: 1050;"></div>');
    }
};

SGCWeb.showToast = function(message, type = 'info', title = null) {
    const types = {
        success: { icon: 'fas fa-check-circle', class: 'bg-success' },
        error: { icon: 'fas fa-exclamation-circle', class: 'bg-danger' },
        warning: { icon: 'fas fa-exclamation-triangle', class: 'bg-warning' },
        info: { icon: 'fas fa-info-circle', class: 'bg-info' }
    };
    
    const config = types[type] || types.info;
    const toastId = 'toast-' + Date.now();
    
    const toastHtml = `
        <div id="${toastId}" class="toast ${config.class} text-white" role="alert" data-autohide="true" data-delay="${this.config.toastDuration}">
            <div class="toast-header ${config.class} text-white border-0">
                <i class="${config.icon} me-2"></i>
                <strong class="me-auto">${title || this.getToastTitle(type)}</strong>
                <button type="button" class="btn-close btn-close-white" data-bs-dismiss="toast"></button>
            </div>
            <div class="toast-body">
                ${message}
            </div>
        </div>
    `;
    
    const $toast = $(toastHtml);
    $('#toast-container').append($toast);
    
    // Inicializa e mostra o toast
    const toast = new bootstrap.Toast($toast[0]);
    toast.show();
    
    // Remove do DOM após esconder
    $toast.on('hidden.bs.toast', function() {
        $(this).remove();
    });
    
    return toastId;
};

SGCWeb.getToastTitle = function(type) {
    const titles = {
        success: 'Sucesso',
        error: 'Erro',
        warning: 'Atenção',
        info: 'Informação'
    };
    return titles[type] || 'Notificação';
};

// ===== FORMULÁRIOS =====
SGCWeb.initForms = function() {
    // Validação em tempo real
    $(document).on('blur', '.form-control[required]', function() {
        const $this = $(this);
        const value = $this.val().trim();
        
        if (value === '') {
            $this.addClass('is-invalid');
        } else {
            $this.removeClass('is-invalid').addClass('is-valid');
        }
    });
    
    // Máscara para campos
    this.initInputMasks();
    
    // Confirmação antes de submeter formulários críticos
    $(document).on('submit', 'form[data-confirm]', function(e) {
        const message = $(this).data('confirm');
        if (!confirm(message)) {
            e.preventDefault();
            return false;
        }
    });
    
    // Auto-save para formulários longos
    this.initAutoSave();
};

SGCWeb.initInputMasks = function() {
    // Implementar máscaras conforme necessário
    // Exemplo: CPF, CNPJ, telefone, etc.
    
    // CPF
    $('.mask-cpf').on('input', function() {
        let value = this.value.replace(/\D/g, '');
        value = value.replace(/(\d{3})(\d)/, '$1.$2');
        value = value.replace(/(\d{3})(\d)/, '$1.$2');
        value = value.replace(/(\d{3})(\d{1,2})$/, '$1-$2');
        this.value = value;
    });
    
    // Telefone
    $('.mask-phone').on('input', function() {
        let value = this.value.replace(/\D/g, '');
        if (value.length <= 10) {
            value = value.replace(/(\d{2})(\d)/, '($1) $2');
            value = value.replace(/(\d{4})(\d)/, '$1-$2');
        } else {
            value = value.replace(/(\d{2})(\d)/, '($1) $2');
            value = value.replace(/(\d{5})(\d)/, '$1-$2');
        }
        this.value = value;
    });
};

SGCWeb.initAutoSave = function() {
    let autoSaveTimeout;
    
    $(document).on('input', 'form[data-autosave] input, form[data-autosave] textarea, form[data-autosave] select', function() {
        const $form = $(this).closest('form');
        
        clearTimeout(autoSaveTimeout);
        autoSaveTimeout = setTimeout(function() {
            SGCWeb.autoSaveForm($form);
        }, 5000); // Auto-save após 5 segundos de inatividade
    });
};

SGCWeb.autoSaveForm = function($form) {
    const formData = $form.serialize();
    const formId = $form.attr('id') || 'form-' + Date.now();
    
    localStorage.setItem('autosave-' + formId, formData);
    this.showToast('Rascunho salvo automaticamente', 'info');
};

// ===== TABELAS =====
SGCWeb.initTables = function() {
    // Ordenação de tabelas
    $(document).on('click', 'th[data-sort]', function() {
        const $this = $(this);
        const $table = $this.closest('table');
        const column = $this.data('sort');
        const currentOrder = $this.data('order') || 'asc';
        const newOrder = currentOrder === 'asc' ? 'desc' : 'asc';
        
        // Remove ordenação de outras colunas
        $table.find('th[data-sort]').removeClass('sorted-asc sorted-desc').removeData('order');
        
        // Aplica nova ordenação
        $this.addClass('sorted-' + newOrder).data('order', newOrder);
        
        // Implementar lógica de ordenação aqui
        this.sortTable($table, column, newOrder);
    });
    
    // Filtro de tabelas
    $(document).on('input', '[data-table-filter]', this.debounce(function() {
        const $input = $(this);
        const targetTable = $input.data('table-filter');
        const filterValue = $input.val().toLowerCase();
        
        $(targetTable + ' tbody tr').each(function() {
            const $row = $(this);
            const text = $row.text().toLowerCase();
            
            if (text.includes(filterValue)) {
                $row.show();
            } else {
                $row.hide();
            }
        });
    }, this.config.debounceDelay));
};

SGCWeb.sortTable = function($table, column, order) {
    // Implementação básica de ordenação
    const $tbody = $table.find('tbody');
    const $rows = $tbody.find('tr').toArray();
    
    $rows.sort(function(a, b) {
        const aVal = $(a).find(`td[data-sort="${column}"]`).text().trim();
        const bVal = $(b).find(`td[data-sort="${column}"]`).text().trim();
        
        if (order === 'asc') {
            return aVal.localeCompare(bVal);
        } else {
            return bVal.localeCompare(aVal);
        }
    });
    
    $tbody.empty().append($rows);
};

// ===== MODAIS =====
SGCWeb.initModals = function() {
    // Carregamento dinâmico de conteúdo
    $(document).on('show.bs.modal', '[data-remote]', function() {
        const $modal = $(this);
        const url = $modal.data('remote');
        
        if (url) {
            $modal.find('.modal-body').html('<div class="text-center"><div class="spinner"></div></div>');
            
            $.get(url)
                .done(function(data) {
                    $modal.find('.modal-body').html(data);
                })
                .fail(function() {
                    $modal.find('.modal-body').html('<div class="alert alert-danger">Erro ao carregar conteúdo.</div>');
                });
        }
    });
    
    // Limpeza ao fechar modal
    $(document).on('hidden.bs.modal', '.modal', function() {
        const $modal = $(this);
        if ($modal.data('remote')) {
            $modal.find('.modal-body').empty();
        }
    });
};

// ===== TOOLTIPS E POPOVERS =====
SGCWeb.initTooltips = function() {
    // Inicializa tooltips
    const tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
    tooltipTriggerList.map(function(tooltipTriggerEl) {
        return new bootstrap.Tooltip(tooltipTriggerEl);
    });
    
    // Inicializa popovers
    const popoverTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="popover"]'));
    popoverTriggerList.map(function(popoverTriggerEl) {
        return new bootstrap.Popover(popoverTriggerEl);
    });
};

// ===== CONFIGURAÇÃO AJAX =====
SGCWeb.initAjaxSetup = function() {
    $.ajaxSetup({
        timeout: this.config.ajaxTimeout,
        beforeSend: function(xhr, settings) {
            // Adiciona token CSRF se disponível
            const token = $('input[name="__RequestVerificationToken"]').val();
            if (token) {
                xhr.setRequestHeader('RequestVerificationToken', token);
            }
            
            // Mostra loading se não for uma requisição silenciosa
            if (!settings.silent) {
                SGCWeb.showLoading();
            }
        },
        complete: function(xhr, status) {
            SGCWeb.hideLoading();
        },
        error: function(xhr, status, error) {
            if (status !== 'abort') {
                let message = 'Erro na comunicação com o servidor.';
                
                if (xhr.responseJSON && xhr.responseJSON.message) {
                    message = xhr.responseJSON.message;
                } else if (xhr.status === 403) {
                    message = 'Acesso negado.';
                } else if (xhr.status === 404) {
                    message = 'Recurso não encontrado.';
                } else if (xhr.status === 500) {
                    message = 'Erro interno do servidor.';
                }
                
                SGCWeb.showToast(message, 'error');
            }
        }
    });
};

// ===== LOADING OVERLAY =====
SGCWeb.initLoadingOverlay = function() {
    if (!$('#loading-overlay').length) {
        $('body').append(`
            <div id="loading-overlay" class="loading-overlay" style="display: none;">
                <div class="spinner"></div>
            </div>
        `);
    }
};

SGCWeb.showLoading = function() {
    $('#loading-overlay').fadeIn(200);
};

SGCWeb.hideLoading = function() {
    $('#loading-overlay').fadeOut(200);
};

// ===== BOTÕES COM PERMISSÃO =====
SGCWeb.initPermissionButtons = function() {
    // Confirma ações destrutivas
    $(document).on('click', '[data-action="delete"]', function(e) {
        e.preventDefault();
        const $this = $(this);
        const message = $this.data('confirm') || 'Tem certeza que deseja excluir este item?';
        
        if (confirm(message)) {
            const url = $this.attr('href') || $this.data('url');
            if (url) {
                window.location.href = url;
            }
        }
    });
    
    // Botões de ação com AJAX
    $(document).on('click', '[data-ajax-action]', function(e) {
        e.preventDefault();
        const $this = $(this);
        const url = $this.data('url') || $this.attr('href');
        const method = $this.data('method') || 'POST';
        const confirm = $this.data('confirm');
        
        if (confirm && !window.confirm(confirm)) {
            return;
        }
        
        $.ajax({
            url: url,
            method: method,
            success: function(response) {
                if (response.success) {
                    SGCWeb.showToast(response.message || 'Operação realizada com sucesso!', 'success');
                    
                    // Recarrega a página se solicitado
                    if (response.reload) {
                        setTimeout(function() {
                            window.location.reload();
                        }, 1000);
                    }
                } else {
                    SGCWeb.showToast(response.message || 'Erro ao executar operação.', 'error');
                }
            }
        });
    });
};

// ===== UTILITÁRIOS =====
SGCWeb.debounce = function(func, wait, immediate) {
    let timeout;
    return function() {
        const context = this;
        const args = arguments;
        const later = function() {
            timeout = null;
            if (!immediate) func.apply(context, args);
        };
        const callNow = immediate && !timeout;
        clearTimeout(timeout);
        timeout = setTimeout(later, wait);
        if (callNow) func.apply(context, args);
    };
};

SGCWeb.formatCurrency = function(value) {
    return new Intl.NumberFormat('pt-BR', {
        style: 'currency',
        currency: 'BRL'
    }).format(value);
};

SGCWeb.formatDate = function(date, format = 'dd/MM/yyyy') {
    if (!(date instanceof Date)) {
        date = new Date(date);
    }
    
    const day = String(date.getDate()).padStart(2, '0');
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const year = date.getFullYear();
    
    return format
        .replace('dd', day)
        .replace('MM', month)
        .replace('yyyy', year);
};

SGCWeb.copyToClipboard = function(text) {
    navigator.clipboard.writeText(text).then(function() {
        SGCWeb.showToast('Texto copiado para a área de transferência!', 'success');
    }).catch(function() {
        SGCWeb.showToast('Erro ao copiar texto.', 'error');
    });
};

// ===== EVENTOS GLOBAIS =====
$(window).on('resize', SGCWeb.debounce(function() {
    // Ajustes responsivos se necessário
}, 250));

// Previne duplo clique em botões de submit
$(document).on('submit', 'form', function() {
    const $form = $(this);
    const $submitBtn = $form.find('button[type="submit"], input[type="submit"]');
    
    $submitBtn.prop('disabled', true);
    
    setTimeout(function() {
        $submitBtn.prop('disabled', false);
    }, 3000);
});

// Confirma saída da página se houver alterações não salvas
let hasUnsavedChanges = false;

$(document).on('input', 'form input, form textarea, form select', function() {
    hasUnsavedChanges = true;
});

$(document).on('submit', 'form', function() {
    hasUnsavedChanges = false;
});

$(window).on('beforeunload', function() {
    if (hasUnsavedChanges) {
        return 'Você tem alterações não salvas. Tem certeza que deseja sair?';
    }
});

// Expõe SGCWeb globalmente
window.SGCWeb = SGCWeb;

