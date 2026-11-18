# Fase 1 - 2.0 — Estado base ampliado de CAFY

Punto de restauración acordado. Incluye todo lo de **Fase 1** y **Fase 2**, más el nuevo flujo de recetas informativas y selección de ingredientes. Si un cambio futuro no gusta o rompe algo, se vuelve aquí.

## Commit / etiqueta

- Commit: el que tiene por mensaje `Fase 1 - 2.0: ...`
- Etiqueta: `Fase-1-2.0`
- Rama en que se creó: `KM`
- Etiquetas anteriores: `Fase-1`, `Fase-2`

## Cómo volver a Fase 1 - 2.0

```bash
# Cierra Unity antes de restaurar para que no reescriba escenas encima.
git reset --hard Fase-1-2.0

# Solo si además hay archivos nuevos que quitar (¡CUIDADO! borra lo no versionado).
git clean -fd
```

Alternativa sin mover HEAD ni borrar nada:

```bash
git restore --source=Fase-1-2.0 -- .
```

> Los marcadores de `Temp/` no están versionados. Los scripts de configuración detectan si los objetos ya existen en la escena y, si es así, no los reconstruyen. Por eso restaurar etiqueta no debería mover tus botones manuales.

## Decisiones que debemos conservar

- **No tocar los fondos** ya ajustados manualmente en `MenuPrincipal`, `GameOver` y `JuegoPrincipal`.
- **No tocar los botones del `JuegoPrincipal` que se acomodaron manualmente**. En especial los del teléfono y la zona de entregas.
- **No cambiar el alpha del negro del menú de Pausa** (`0.8`).
- Pausa aditiva: `Time.timeScale = 0`, `OscurecidoJuego` con alpha `0.28` en `JuegoPrincipal`.
- Animaciones con **Clips Animator de Unity**, no scripts de frames.
- `coffee INICIO` y `CAFY FINAL` son nubes en loop continuo.
- Teléfono: clic sube, sprite base con botones `Pedidos`/`Recetas`, segundo clic baja. Reposo muestra solo la parte de arriba.
- **Recetas informativas**: las tarjetas no son clicables; solo muestran nombre e ingredientes.
- **Preparación**: los ingredientes se clickean y se apilan en `FilaIngredientes`; clic en la ficha de la fila los quita; no se permiten duplicados.
- **Minijuego**: los pasos se generan según los ingredientes elegidos y su orden.
- **Todavía no hay lógica de vidas, puntos, pedidos entrantes ni entrega real**. Guardar/desechar/entregar es solo visual.

## Qué incluye

### Navegación y HUD
- `MenuPrincipal`, `JuegoPrincipal`, `Pausa`, `GameOver`.
- `GestorEscenas`, `PerfilJugador`, `EstadoPartida`, `HUDJuego`.
- `HUDJuego` abre/cierra pausa con ESC.

### Animaciones
Carpeta `Assets/Animaciones/`: `CoffeeInicio`, `CafyFinal`, `Motero`, `Jarabe`, `TelefonoSube`, `TelefonoBaja`, `TelefonoNoti`, `TelefonoArribaQuieto`, `TelefonoAbajoQuieto`, `ExitoCafetera` (+ controladores).

Generadas por:
- `Assets/Editor/GenerarAnimacionesCAFY.cs`
- `Assets/Editor/GenerarMinijuegoCAFY.cs`

### Teléfono
- `ControladorTelefono.cs` con subida/bajada, panel de opciones y notificación.
- `btnRecetas` abre `PanelRecetas`.
- `btnPedidos` sigue como gancho visual.

### Minijuego y pedido
- `ControladorMinijuego.cs`: barra de precisión; velocidad, ancho de zona y color cambian por paso.
- `ControladorPedido.cs`: guarda, descarta y entrega visualmente; muestra nombre de la receta/armado de ingredientes.
- `PanelMiniJuego` con `Titulo`, `Instruccion`, `BarraFondo/ZonaVerde/Cursor`, `Feedback`, `btnAccionar`, `ArteExito`.
- `PedidoEntregar` invisible para colocar sobre el cuadro gris (ya ajustado manualmente).

### Recetas
- `Assets/scripts/Receta.cs` → ScriptableObject.
- `Assets/Recetas/` → `Espresso`, `Capuchino`, `Mocha`, `Café azucarado`, `Café con caramelo`.
- `PanelRecetas` en el Canvas, cerrado por defecto, tarjetas informativas generadas en runtime.
- `Assets/Editor/ConfigurarRecetasCAFY.cs`.

### Preparación
- `ControladorPreparacion.cs`.
- Ingredientes `Cafe`, `Azucar`, `Leche`, `Chocolate`, `Jarabe` convertidos en botones.
- `FilaIngredientes` con fichas clicables para quitar.
- `btnPreparar` llama a `ControladorPreparacion.Preparar()`.
- `Assets/Editor/ConfigurarPreparacionCAFY.cs`.

## Mapeo de ingredientes a pasos

| Ingrediente | Paso del minijuego | Animación |
|---|---|---|
| Café | Extracción | `ExitoCafetera` |
| Azúcar | Endulzado | Solo texto |
| Leche | Vaporizado de leche | Falta sprite |
| Chocolate | Latte art | Falta sprite |
| Jarabe | Toque de jarabe | `Jarabe.controller` |

## Pendiente (NO está en esta fase)

- Sprites nuevos: `ExitoLeche_0..11`, `ExitoChocolate_0..11`, `PedidoListo` (opcional).
- Quitar `(próximamente)` de `Pedidos` cuando haya lógica de pedidos entrantes.
- Conectar `EstadoPartida` con puntos y vidas.
- Cablear `EstadoPartida.PartidaPerdida` con `GestorEscenas.IrAGameOver()`.
- Contenido funcional de la cafetera/minijuego más allá de la barra.

## Verificación

- `dotnet build` de los scripts con las DLL del editor: **0 errores, 0 advertencias**.
- Fase 1: commit `177b15c`, etiqueta `Fase-1`.
- Fase 2: commit `e5af8be`, etiqueta `Fase-2`.
