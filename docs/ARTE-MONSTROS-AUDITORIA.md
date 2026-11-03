# Auditoria visual dos monstros

Base: roguelike-reinos. Pranchas anteriores preservadas em `arte/antes`, oito movimentos por família. A leitura ampliada revela repetição de poses, além de problemas que aparecem em 1×/2×.

| Família | Problema observado | Prioridade |
|---|---|---|
| Slime | Cabeça quase separada da base; apêndices parecem chifres. Walk/run só sobem o corpo. Brilho pequeno não sugere gel. | Massa contínua, compressão e poça |
| Besta | Corpo horizontal legível, mas ombros baixos e pernas sem passagem. Boca de humano no golpe. | Ombros, focinho, apoio alternado e tombamento |
| Golem | Cabeça redonda domina; punhos são dois retângulos sem articulação. Morte derrete como slime. | Ombros blocados, punhos com planos, desmoronamento |
| Dragão | Perfil muito próximo da besta; cauda triangular imóvel, pescoço sem curva. | Pescoço elevado, placas, cauda atrasada |
| Artrópode | Pernas parecem uma escada; pinças não antecipam golpe. Abdômen pouco distinto. | Segmentos, pernas angulares e recolhimento |
| Alado | Asas curtas com duas posições; parece levantar os braços. | Membrana e articulação, queda antes do encolhimento |
| Serpente | Base achatada sem espiral; deslocamento copia quadrúpedes. | Espiral com espaço negativo e desenrolar |
| Aquático | Contorno de peixe já legível; nadadeira pequena e cauda rígida. | Nervuras e atraso da cauda; morte murcha |
| Planta | Cabeça redonda e braços finos, sem copa suficiente. | Folhas agrupadas, raízes e secagem |

Em todas: contorno e contraste facial funcionam em 2×, mas adornos genéricos confundem a família em 1×. Idle contém longos trechos repetidos; corrida só acelera caminhada. O flash é legível. Special/Cast cobrem o rosto, porém seus efeitos estão fora do escopo e serão preservados. Não foram vistos pixels isolados no repouso das nove receitas de referência; a validação deve ampliar essa cobertura. Não basta aumentar detalhes: espaços negativos e poses extremas são a primeira correção.
