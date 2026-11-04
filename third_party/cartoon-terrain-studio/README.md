# Cartoon Terrain Studio — subconjunto MIT

Origem: https://github.com/idlerunner00/cartoon-terrain-studio
Commit fixado: `7aa76ad58d4a31c4a9441f5718e8c3a31d6349a1`.

`Elevation.cs` e `Scalar.cs` são cópias sem modificações de `src/Domain/Dungeon/Elevation.cs` e `src/Domain/MathKit/Scalar.cs`. O gerador VERDANT usa diretamente `Elevation.fractalValueNoise`, `Elevation.valueNoise` e `Elevation.latticeHash` para relevo, rio e distribuição de vegetação. Adota a construção em patamares 2×2 explicada em `TerrainTerrace.cs`, sem copiar seu editor.

`NumericCompatibility.cs` implementa apenas floor/imul/ToInt32/ToUint32 requeridos por Elevation. Evita importar a camada JS e funções transcendentais não utilizadas. Não é uma implementação geral de JavaScript. Testes incluem resultados de referência calculados no código original.

A composição de mapas de arena, malhas, paletas, controles e integração são novas adaptações para o VERDANT. O editor completo, streaming infinito, compositor de mapas e shaders Forward+ do Studio não foram importados. O upstream usa Godot 4.7.2; esta integração mantém 4.5.2 Compatibility.

Licença MIT de idlerunner00 preservada em LICENSE. THIRD_PARTY_NOTICES.md é mantido como referência da origem; os componentes three.js/V8/shaders listados ali não fazem parte deste subconjunto.
