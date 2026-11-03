# Prova web R0

Demonstra `Verdant.Core` rodando no navegador: `BattleSimulation` real, 30 Hz, seis unidades e quadros Forge pré-calculados. Pausar, repetir e especial de Bram funcionam por toque ou clique. Sem servidor de lógica, contas ou acesso remoto durante o combate. Não é a interface final da run e só apresenta caminhada e barras; estados/especiais são simulados, sem a apresentação completa da arena.

.NET 8 SDK basta para esta prova interpretada; `wasm-tools` é opcional para otimizar publicação/AOT. A primeira restauração usa NuGet.org para obter o runtime do navegador e Blazor. A publicação desativa trimming para tornar esta investigação reproduzível; seu tamanho não é o orçamento final.

```sh
dotnet restore tools/WebProof --source https://api.nuget.org/v3/index.json
dotnet publish tools/WebProof -c Release --no-restore -o /tmp/verdant-web
python3 -m http.server 8765 --bind 127.0.0.1 --directory /tmp/verdant-web/wwwroot
```

Abra http://127.0.0.1:8765. O servidor apenas entrega arquivos estáticos; pode ser encerrado após o carregamento. Não é necessário SharedArrayBuffer, COOP/COEP ou o fork do Godot nesta prova.

Teste real de navegador (Python Playwright e Chromium instalados):

```sh
dotnet run --project tools/PixelPreview -- web-fixture /tmp/verdant-web-trace.json
python3 tools/WebProof/test_browser.py --fixture /tmp/verdant-web-trace.json --capture /tmp/web-proof.png
```

O teste compara os 222 eventos de uma batalha completa no navegador com o CLI nativo e aciona controles por eventos de toque em 16:9, 18:9 e 20:9. Verifica os três botões com pelo menos 48 pixels CSS. Isso não valida DPI Android, notch real, persistência web ou a futura UI Godot. API de teste exposta em `window.proof` somente nesta prova.
