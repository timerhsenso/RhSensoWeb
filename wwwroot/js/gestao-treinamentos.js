$(document).ready(function() {
    console.log('🚀 INICIANDO GESTÃO DE TREINAMENTOS...');
    
    // Variáveis globais
    let tabelaVisaoGeral, tabelaPorTipo, tabelaAVencer;
    let idParaExcluir = 0;

    // Configurar valores padrão
    configurarFiltrosPadrao();
    
    // Aguardar um pouco antes de inicializar
    setTimeout(function() {
        inicializarComponentes();
        carregarDadosIniciais();
        inicializarTabelas();
        configurarEventos();
    }, 500);

    // ========== CONFIGURAÇÕES INICIAIS ==========
    function configurarFiltrosPadrao() {
        console.log('📅 Configurando filtros padrão...');
        
        const agora = new Date();
        const mesAtual = agora.getMonth() + 1;
        const anoAtual = agora.getFullYear();
        
        // Filtros padrão para todas as abas
        $('#filtroTipo').val('todos');
        $('#mesFiltro').val(mesAtual);
        $('#anoFiltro').val(anoAtual);
        
        $('#filtroEmpregadoTipo').val('todos');
        $('#mesFiltroTipo').val(mesAtual);
        $('#anoFiltroTipo').val(anoAtual);
        
        $('#mesFiltroVencer').val(mesAtual);
        $('#anoFiltroVencer').val(anoAtual);
        $('#diasLimite').val(30);
        
        console.log(`✅ Filtros configurados: Mês ${mesAtual}, Ano ${anoAtual}`);
    }

    function inicializarComponentes() {
        console.log('🔧 Inicializando componentes...');
        
        // Data máxima como hoje
        const hoje = new Date().toISOString().split('T')[0];
        $('#dataRealizacao').attr('max', hoje);
        
        // Contador de caracteres
        $('#observacao').on('input', function() {
            const length = $(this).val().length;
            $('#contadorObservacao').text(length);
            
            if (length > 255) {
                $(this).addClass('is-invalid');
            } else {
                $(this).removeClass('is-invalid');
            }
        });

        // Validação de campo numérico (ano)
        $('#anoFiltro, #anoFiltroTipo, #anoFiltroVencer').on('input', function() {
            this.value = this.value.replace(/[^0-9]/g, '');
        });
    }

    function carregarDadosIniciais() {
        console.log('📊 Carregando dados iniciais...');
        carregarTiposTreinamento();
        carregarColaboradores();
    }

    function carregarTiposTreinamento() {
        console.log('📋 Carregando tipos de treinamento...');
        
        $.ajax({
            url: '/SGC_GestaoTreinamentos/GetTiposTreinamento',
            type: 'GET',
            timeout: 10000
        })
        .done(function(data) {
            console.log('✅ Tipos carregados:', data);
            
            if (data && Array.isArray(data)) {
                const selects = ['#tipoTreinamentoId', '#tipoTreinamentoSelect'];
                selects.forEach(function(selector) {
                    const $select = $(selector);
                    $select.empty().append('<option value="">Selecione...</option>');
                    
                    data.forEach(function(item) {
                        $select.append(`<option value="${item.value}">${item.text}</option>`);
                    });
                });
                console.log('✅ Dropdowns de tipos preenchidos');
            } else {
                console.error('❌ Dados de tipos inválidos:', data);
            }
        })
        .fail(function(xhr, status, error) {
            console.error('❌ Erro ao carregar tipos:', error);
            alert('Erro ao carregar tipos de treinamento: ' + error);
        });
    }

    function carregarColaboradores() {
        console.log('👥 Carregando colaboradores...');
        
        $.ajax({
            url: '/SGC_GestaoTreinamentos/GetColaboradores',
            type: 'GET',
            timeout: 10000
        })
        .done(function(data) {
            console.log('✅ Colaboradores carregados:', data);
            
            if (data && Array.isArray(data)) {
                const $select = $('#colaboradorId');
                $select.empty().append('<option value="">Selecione...</option>');
                
                data.forEach(function(item) {
                    $select.append(`<option value="${item.value}">${item.text}</option>`);
                });
                console.log('✅ Dropdown de colaboradores preenchido');
            } else {
                console.error('❌ Dados de colaboradores inválidos:', data);
            }
        })
        .fail(function(xhr, status, error) {
            console.error('❌ Erro ao carregar colaboradores:', error);
            alert('Erro ao carregar colaboradores: ' + error);
        });
    }

    // ========== DATATABLES ==========
    function inicializarTabelas() {
        console.log('📊 Inicializando tabelas...');
        
        // Configuração básica
        const configBasico = {
            language: {
                url: '//cdn.datatables.net/plug-ins/1.13.7/i18n/pt-BR.json'
            },
            responsive: true,
            pageLength: 25,
            processing: true,
            serverSide: false,
            order: [[0, 'asc']],
            dom: 'Bfrtip',
            buttons: [
                {
                    extend: 'excel',
                    text: '<i class="fas fa-file-excel"></i> Excel',
                    className: 'btn btn-outline-secondary btn-sm'
                },
                {
                    extend: 'pdf',
                    text: '<i class="fas fa-file-pdf"></i> PDF',
                    className: 'btn btn-outline-secondary btn-sm'
                },
                {
                    extend: 'print',
                    text: '<i class="fas fa-print"></i> Print',
                    className: 'btn btn-outline-secondary btn-sm'
                }
            ]
        };

        // TABELA 1: Visão Geral
        try {
            tabelaVisaoGeral = $('#tabelaVisaoGeral').DataTable({
                ...configBasico,
                ajax: {
                    url: '/SGC_GestaoTreinamentos/GetVisaoGeral',
                    type: 'GET',
                    data: function(d) {
                        const params = {
                            filtroTipo: $('#filtroTipo').val() || 'todos',
                            mes: parseInt($('#mesFiltro').val()) || new Date().getMonth() + 1,
                            ano: parseInt($('#anoFiltro').val()) || new Date().getFullYear()
                        };
                        console.log('📤 Parâmetros ABA 1:', params);
                        return params;
                    },
                    dataSrc: function(json) {
                        console.log('📥 Resposta ABA 1:', json);
                        if (json.error) {
                            console.error('❌ Erro ABA 1:', json.error);
                            alert('Erro na ABA 1: ' + json.error);
                            return [];
                        }
                        if (json.data && Array.isArray(json.data)) {
                            console.log(`✅ ABA 1: ${json.data.length} registros carregados`);
                            return json.data;
                        }
                        console.error('❌ Dados inválidos ABA 1:', json);
                        return [];
                    },
                    error: function(xhr, error, thrown) {
                        console.error('❌ Erro AJAX ABA 1:', error, thrown);
                        alert('Erro ao carregar dados da ABA 1: ' + error);
                    }
                },
                columns: [
                    { data: 'nome', title: 'Nome' },
                    { data: 'tipoPessoa', title: 'Tipo Pessoa' },
                    { data: 'tipoTreinamento', title: 'Tipo de Treinamento' },
                    { data: 'dataRealizacao', title: 'Data Realização', className: 'text-center' },
                    { data: 'dataVencimento', title: 'Data Vencimento', className: 'text-center' },
                    { 
                        data: 'diasRestantes', 
                        title: 'Dias Restantes',
                        className: 'text-center',
                        render: function(data) {
                            let classe = 'text-success';
                            if (data < 0) classe = 'text-danger font-weight-bold';
                            else if (data <= 30) classe = 'text-warning font-weight-bold';
                            return `<span class="${classe}">${data}</span>`;
                        }
                    },
                    { 
                        data: 'status',
                        title: 'Status',
                        className: 'text-center',
                        render: function(data, type, row) {
                            return `<span class="badge badge-${row.statusClass || 'secondary'}">${data}</span>`;
                        }
                    },
                    {
                        data: null,
                        title: 'Ações',
                        className: 'text-center',
                        orderable: false,
                        render: function(data, type, row) {
                            return `
                                <button class="btn btn-sm btn-info btn-detalhes" data-id="${row.colaboradorId}" title="Ver Detalhes">
                                    <i class="fas fa-eye"></i>
                                </button>
                                <button class="btn btn-sm btn-warning btn-editar ml-1" data-id="${row.colaboradorTreinamentoId}" title="Editar">
                                    <i class="fas fa-edit"></i>
                                </button>
                                <button class="btn btn-sm btn-danger btn-excluir ml-1" data-id="${row.colaboradorTreinamentoId}" title="Excluir">
                                    <i class="fas fa-trash"></i>
                                </button>
                            `;
                        }
                    }
                ]
            });
            console.log('✅ Tabela 1 inicializada');
        } catch (error) {
            console.error('❌ Erro ao inicializar tabela 1:', error);
        }

        // TABELA 2: Por Tipo
        try {
            tabelaPorTipo = $('#tabelaPorTipo').DataTable({
                ...configBasico,
                ajax: {
                    url: '/SGC_GestaoTreinamentos/GetPorTipo',
                    type: 'GET',
                    data: function(d) {
                        const params = {
                            tipoTreinamentoId: parseInt($('#tipoTreinamentoSelect').val()) || 0,
                            filtroEmpregado: $('#filtroEmpregadoTipo').val() || 'todos',
                            mes: parseInt($('#mesFiltroTipo').val()) || new Date().getMonth() + 1,
                            ano: parseInt($('#anoFiltroTipo').val()) || new Date().getFullYear()
                        };
                        console.log('📤 Parâmetros ABA 2:', params);
                        return params;
                    },
                    dataSrc: function(json) {
                        console.log('📥 Resposta ABA 2:', json);
                        if (json.error) {
                            console.error('❌ Erro ABA 2:', json.error);
                            return [];
                        }
                        if (json.data && Array.isArray(json.data)) {
                            console.log(`✅ ABA 2: ${json.data.length} registros carregados`);
                            return json.data;
                        }
                        return [];
                    },
                    error: function(xhr, error, thrown) {
                        console.error('❌ Erro AJAX ABA 2:', error, thrown);
                    }
                },
                columns: [
                    { data: 'nome', title: 'Nome' },
                    { data: 'tipoPessoa', title: 'Tipo Pessoa' },
                    { data: 'dataRealizacao', title: 'Data Realização', className: 'text-center' },
                    { data: 'dataVencimento', title: 'Data Vencimento', className: 'text-center' },
                    { 
                        data: 'diasRestantes', 
                        title: 'Dias Restantes',
                        className: 'text-center',
                        render: function(data) {
                            let classe = 'text-success';
                            if (data < 0) classe = 'text-danger font-weight-bold';
                            else if (data <= 30) classe = 'text-warning font-weight-bold';
                            return `<span class="${classe}">${data}</span>`;
                        }
                    },
                    { 
                        data: 'status',
                        title: 'Status',
                        className: 'text-center',
                        render: function(data, type, row) {
                            return `<span class="badge badge-${row.statusClass || 'secondary'}">${data}</span>`;
                        }
                    },
                    {
                        data: null,
                        title: 'Ações',
                        className: 'text-center',
                        orderable: false,
                        render: function(data, type, row) {
                            return `
                                <button class="btn btn-sm btn-info btn-detalhes" data-id="${row.colaboradorId}" title="Ver Detalhes">
                                    <i class="fas fa-eye"></i>
                                </button>
                                <button class="btn btn-sm btn-warning btn-editar ml-1" data-id="${row.colaboradorTreinamentoId}" title="Editar">
                                    <i class="fas fa-edit"></i>
                                </button>
                                <button class="btn btn-sm btn-danger btn-excluir ml-1" data-id="${row.colaboradorTreinamentoId}" title="Excluir">
                                    <i class="fas fa-trash"></i>
                                </button>
                            `;
                        }
                    }
                ]
            });
            console.log('✅ Tabela 2 inicializada');
        } catch (error) {
            console.error('❌ Erro ao inicializar tabela 2:', error);
        }

        // TABELA 3: A Vencer
        try {
            tabelaAVencer = $('#tabelaAVencer').DataTable({
                ...configBasico,
                ajax: {
                    url: '/SGC_GestaoTreinamentos/GetAVencer',
                    type: 'GET',
                    data: function(d) {
                        const params = {
                            diasLimite: parseInt($('#diasLimite').val()) || 30,
                            mes: parseInt($('#mesFiltroVencer').val()) || new Date().getMonth() + 1,
                            ano: parseInt($('#anoFiltroVencer').val()) || new Date().getFullYear()
                        };
                        console.log('📤 Parâmetros ABA 3:', params);
                        return params;
                    },
                    dataSrc: function(json) {
                        console.log('📥 Resposta ABA 3:', json);
                        if (json.error) {
                            console.error('❌ Erro ABA 3:', json.error);
                            return [];
                        }
                        if (json.data && Array.isArray(json.data)) {
                            console.log(`✅ ABA 3: ${json.data.length} registros carregados`);
                            return json.data;
                        }
                        return [];
                    },
                    error: function(xhr, error, thrown) {
                        console.error('❌ Erro AJAX ABA 3:', error, thrown);
                    }
                },
                columns: [
                    { data: 'nome', title: 'Nome' },
                    { data: 'tipoPessoa', title: 'Tipo Pessoa' },
                    { data: 'tipoTreinamento', title: 'Treinamento' },
                    { data: 'dataRealizacao', title: 'Data Realização', className: 'text-center' },
                    { data: 'dataVencimento', title: 'Data Vencimento', className: 'text-center' },
                    { 
                        data: 'diasRestantes', 
                        title: 'Dias Restantes',
                        className: 'text-center',
                        render: function(data) {
                            let classe = 'text-success';
                            if (data < 0) classe = 'text-danger font-weight-bold';
                            else if (data <= 30) classe = 'text-warning font-weight-bold';
                            return `<span class="${classe}">${data}</span>`;
                        }
                    },
                    { 
                        data: 'status',
                        title: 'Status',
                        className: 'text-center',
                        render: function(data, type, row) {
                            return `<span class="badge badge-${row.statusClass || 'secondary'}">${data}</span>`;
                        }
                    },
                    {
                        data: null,
                        title: 'Ações',
                        className: 'text-center',
                        orderable: false,
                        render: function(data, type, row) {
                            return `
                                <button class="btn btn-sm btn-info btn-detalhes" data-id="${row.colaboradorId}" title="Ver Detalhes">
                                    <i class="fas fa-eye"></i>
                                </button>
                                <button class="btn btn-sm btn-warning btn-editar ml-1" data-id="${row.colaboradorTreinamentoId}" title="Editar">
                                    <i class="fas fa-edit"></i>
                                </button>
                                <button class="btn btn-sm btn-danger btn-excluir ml-1" data-id="${row.colaboradorTreinamentoId}" title="Excluir">
                                    <i class="fas fa-trash"></i>
                                </button>
                            `;
                        }
                    }
                ]
            });
            console.log('✅ Tabela 3 inicializada');
        } catch (error) {
            console.error('❌ Erro ao inicializar tabela 3:', error);
        }

        // Carregar dados da primeira aba automaticamente
        setTimeout(function() {
            if (tabelaVisaoGeral) {
                console.log('🔄 Carregando dados iniciais da ABA 1...');
                tabelaVisaoGeral.ajax.reload();
            }
        }, 1000);
    }

    // ========== EVENTOS ==========
    function configurarEventos() {
        console.log('🎯 Configurando eventos...');
        
        // Filtros ABA 1
        $('#filtroTipo, #mesFiltro, #anoFiltro').on('change', function() {
            console.log('🔄 Recarregando ABA 1...');
            if (tabelaVisaoGeral) {
                tabelaVisaoGeral.ajax.reload();
            }
        });

        // Filtros ABA 2
        $('#tipoTreinamentoSelect').on('change', function() {
            const tipoSelecionado = $(this).val();
            console.log('🔄 Tipo selecionado:', tipoSelecionado);
            if (tipoSelecionado && tabelaPorTipo) {
                tabelaPorTipo.ajax.reload();
            } else if (tabelaPorTipo) {
                tabelaPorTipo.clear().draw();
            }
        });

        $('#filtroEmpregadoTipo, #mesFiltroTipo, #anoFiltroTipo').on('change', function() {
            if ($('#tipoTreinamentoSelect').val() && tabelaPorTipo) {
                console.log('🔄 Recarregando ABA 2...');
                tabelaPorTipo.ajax.reload();
            }
        });

        // Filtros ABA 3
        $('#diasLimite, #mesFiltroVencer, #anoFiltroVencer').on('change', function() {
            console.log('🔄 Recarregando ABA 3...');
            if (tabelaAVencer) {
                tabelaAVencer.ajax.reload();
            }
        });

        // Botão incluir
        $('#btnIncluirGeral').on('click', function() {
            console.log('➕ Abrindo modal de inclusão...');
            abrirModalTreinamento();
        });

        // Eventos de tabela
        $(document).on('click', '.btn-detalhes', function() {
            const colaboradorId = $(this).data('id');
            abrirModalDetalhes(colaboradorId);
        });

        $(document).on('click', '.btn-editar', function() {
            const id = $(this).data('id');
            editarTreinamento(id);
        });

        $(document).on('click', '.btn-excluir', function() {
            const id = $(this).data('id');
            idParaExcluir = id;
            $('#modalExcluir').modal('show');
        });

        // Modal de treinamento
        $('#formTreinamento').on('submit', function(e) {
            e.preventDefault();
            salvarTreinamento();
        });

        // Modal de exclusão
        $('#btnConfirmarExclusao').on('click', function() {
            excluirTreinamento();
        });

        // Abas
        $('a[data-toggle="tab"]').on('shown.bs.tab', function(e) {
            const target = $(e.target).attr('href');
            setTimeout(() => {
                if (target === '#visao-geral' && tabelaVisaoGeral) {
                    tabelaVisaoGeral.columns.adjust();
                } else if (target === '#por-tipo' && tabelaPorTipo) {
                    tabelaPorTipo.columns.adjust();
                } else if (target === '#a-vencer' && tabelaAVencer) {
                    tabelaAVencer.columns.adjust();
                }
            }, 100);
        });
    }

    // ========== FUNÇÕES DE MODAL ==========
    function abrirModalTreinamento(id = 0) {
        console.log(`📝 Abrindo modal (ID: ${id})`);
        
        $('#formTreinamento')[0].reset();
        $('#colaboradorTreinamentoId').val(id);
        $('#contadorObservacao').text('0');
        
        if (id === 0) {
            $('#modalTreinamentoTitle').html('<i class="fas fa-plus"></i> Adicionar Treinamento');
            $('#dataRealizacao').val(new Date().toISOString().split('T')[0]);
        } else {
            $('#modalTreinamentoTitle').html('<i class="fas fa-edit"></i> Editar Treinamento');
            carregarDadosTreinamento(id);
        }
        
        $('#modalTreinamento').modal('show');
    }

    function carregarDadosTreinamento(id) {
        console.log(`📋 Carregando dados do treinamento ${id}`);
        
        $.get('/SGC_GestaoTreinamentos/GetTreinamento', { id: id })
            .done(function(data) {
                if (data.error) {
                    alert('Erro: ' + data.error);
                    return;
                }
                
                $('#colaboradorId').val(data.colaboradorId);
                $('#tipoTreinamentoId').val(data.tipoTreinamentoId);
                $('#dataRealizacao').val(data.dataRealizacao);
                $('#observacao').val(data.observacao || '');
                $('#contadorObservacao').text((data.observacao || '').length);
            })
            .fail(function() {
                alert('Erro ao carregar dados do treinamento');
            });
    }

    function abrirModalDetalhes(colaboradorId) {
        console.log(`👤 Detalhes do colaborador ${colaboradorId}`);
        
        $.get('/SGC_GestaoTreinamentos/GetDetalhes', { colaboradorId: colaboradorId })
            .done(function(data) {
                if (data.error) {
                    alert('Erro: ' + data.error);
                    return;
                }
                
                $('#detalhesColaborador').html(`
                    <div class="card">
                        <div class="card-body">
                            <h5><i class="fas fa-user"></i> ${data.colaborador.nome}</h5>
                            <p class="mb-0"><strong>Tipo:</strong> ${data.colaborador.tipoPessoa}</p>
                        </div>
                    </div>
                `);
                
                const tbody = $('#tabelaDetalhes tbody');
                tbody.empty();
                
                if (data.treinamentos.length === 0) {
                    tbody.append('<tr><td colspan="6" class="text-center">Nenhum treinamento encontrado</td></tr>');
                } else {
                    data.treinamentos.forEach(function(item) {
                        const statusBadge = `<span class="badge badge-${item.statusClass || 'secondary'}">${item.status}</span>`;
                        const diasRestantes = item.diasRestantes < 0 ? 
                            `<span class="text-danger font-weight-bold">${item.diasRestantes}</span>` :
                            item.diasRestantes <= 30 ? 
                                `<span class="text-warning font-weight-bold">${item.diasRestantes}</span>` :
                                `<span class="text-success">${item.diasRestantes}</span>`;
                        
                        tbody.append(`
                            <tr>
                                <td>${item.tipoTreinamento}</td>
                                <td class="text-center">${item.dataRealizacao}</td>
                                <td class="text-center">${item.dataVencimento}</td>
                                <td class="text-center">${diasRestantes}</td>
                                <td class="text-center">${statusBadge}</td>
                                <td>${item.observacao || '-'}</td>
                            </tr>
                        `);
                    });
                }
                
                $('#modalDetalhes').modal('show');
            })
            .fail(function() {
                alert('Erro ao carregar detalhes');
            });
    }

    function editarTreinamento(id) {
        abrirModalTreinamento(id);
    }

    function salvarTreinamento() {
        console.log('💾 Salvando...');
        
        const form = $('#formTreinamento')[0];
        
        if (!form.checkValidity()) {
            form.classList.add('was-validated');
            return;
        }
        
        const formData = new FormData(form);
        formData.append('__RequestVerificationToken', $('input[name="__RequestVerificationToken"]').val());
        
        $.ajax({
            url: '/SGC_GestaoTreinamentos/Salvar',
            type: 'POST',
            data: formData,
            processData: false,
            contentType: false
        })
        .done(function(response) {
            if (response.success) {
                alert('Sucesso: ' + response.message);
                $('#modalTreinamento').modal('hide');
                
                // Recarregar todas as tabelas
                if (tabelaVisaoGeral) tabelaVisaoGeral.ajax.reload();
                if ($('#tipoTreinamentoSelect').val() && tabelaPorTipo) tabelaPorTipo.ajax.reload();
                if (tabelaAVencer) tabelaAVencer.ajax.reload();
            } else {
                alert('Erro: ' + response.message);
            }
        })
        .fail(function() {
            alert('Erro ao salvar');
        });
    }

    function excluirTreinamento() {
        console.log(`🗑️ Excluindo ${idParaExcluir}`);
        
        $.ajax({
            url: '/SGC_GestaoTreinamentos/Excluir',
            type: 'POST',
            data: { 
                id: idParaExcluir,
                __RequestVerificationToken: $('input[name="__RequestVerificationToken"]').val()
            }
        })
        .done(function(response) {
            $('#modalExcluir').modal('hide');
            
            if (response.success) {
                alert('Sucesso: ' + response.message);
                
                // Recarregar todas as tabelas
                if (tabelaVisaoGeral) tabelaVisaoGeral.ajax.reload();
                if ($('#tipoTreinamentoSelect').val() && tabelaPorTipo) tabelaPorTipo.ajax.reload();
                if (tabelaAVencer) tabelaAVencer.ajax.reload();
            } else {
                alert('Erro: ' + response.message);
            }
        })
        .fail(function() {
            alert('Erro ao excluir');
        });
    }

    console.log('✅ Sistema de Gestão de Treinamentos carregado!');
});

