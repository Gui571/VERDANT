# Cinco reinos — direção para R1/R2

Este documento define conteúdo planejado. R0 não adiciona encontros ou bosses de reino jogáveis; o código atual ainda oferece monstros genéricos da oficina.

| Reino | Bioma / elemento | Tecido base | Tropas | Boss |
|---|---|---|---|---|
| Bosque de Verdália | Floresta, planta/veneno | `849967` | Arqueiros e curandeiros | Ancião Raiz, planta |
| Cinzas de Emberhold | Vulcão, fogo | `c4575a` | Guerreiros e lanceiros | Dragão de Brasa, dragão |
| Gelo de Frostmar | Tundra, gelo | `79bed1` | Guardiões e magos | Golem Glacial, golem |
| Marés de Naufraga | Costa/pântano, água | `57afa4` | Duelistas e magos | Serpente-Maré, serpente |
| Coroa das Sombras | Ruínas, sombra | `9678cf` | Classes mistas, elites | Rei Sombrio, alado |

Ordem livre. “Quinto reino” significa o quinto conquistado, não obrigatoriamente Coroa das Sombras. Tier de dificuldade depende de conquistas e distância; identidade define composição e bioma, não um multiplicador oculto de dificuldade. Começar por qualquer reino deve ser viável, validado pelo bot antes do balanceamento final.

Todos os soldados recebem a rampa `Cloth` do reino; pele, metal e equipamento mantêm seus papéis. Um emblema procedural 16×16, três cores e forma distintiva acompanha a cor. Bosses usam `MonsterRenderer` em células 48×48, duas habilidades e fases, nunca heróis ampliados. O quinto confronto de boss poderá ter uma terceira fase descrita em dados.

Decisão sobre água: o enum atual só tem Fire/Ice/Plant/Shadow/Poison. R2 deve acrescentar Water no final, preservando valores existentes e hashes de receitas antigas. Água usa empurrão/jato; não será disfarçada de gelo. Fogo queima, gelo retarda, planta imobiliza, veneno aplica DOT e sombra drena.

Composições de tropas e bosses ficarão em tabelas, incluindo habilidades e limiares de fase. O seletor usará famílias/raridades adequadas ao bioma, 2–6 monstros ou 3–6 tropas, elites com modificador visível. Recompensa de conquista: escolha maior incluindo recrutamento do reino ou relíquia, com cura explícita. Números de vida/dano, duração e recompensas serão calibrados em R2/R4, não inventados como resultados nesta fundação.
