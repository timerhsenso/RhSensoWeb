use bd_rhu_copenor


/* constantes */
select * from const1

/* tabela auxiliar */
select * from taux1
select * from taux2

/* tabela de sistemas */ (ativo BIT)
select * from tsistema

/* tabela de usuario */ (ativo s/n)
select * from tuse1

/* tabele do usuario com o grupo do usuario */
select * from usrh1 where cdusuario = 'verusa' order by dtfimval 

/**/
select * from btfuncao

/* funcoes */
select * from fucn1

/* grupos */
select * from gurh1 where cdsistema = 'RHU'

select * from hbrh1





