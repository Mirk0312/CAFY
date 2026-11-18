# Fase 1 — Estado base de CAFY

Punto de restauración acordado. Todo lo que aparece aquí está **probado en Unity y compilando con 0 errores**. Si un cambio futuro no nos gusta o rompe algo, se vuelve a este estado.

## Commit / etiqueta

- Commit: el que tiene por mensaje `Fase 1: ...`
- Etiqueta: `Fase-1`
- Rama en que se creó: `KM`

## Cómo volver a Fase 1

```bash
# Descarta cambios en archivos ya versionados y coloca HEAD en Fase 1
git reset --hard Fase-1

# Si además creamos archivos nuevos después de Fase 1, bórralos (¡CUIDADO! borra todo lo no versionado)
git clean -fd
```

Alternativa sin mover HEAD ni borrar nada:

```bash
git restore --source=Fase-1 -- .
```

> Antes de ejecutar cualquier restauración, guarda y cierra Unity para que no vuelva a escribir escenas encima.

## Qué incluye Fase 1

### Menú y navegación
- `MenuPrincipal`, `JuegoPrincipal`, `Pausa` y `GameOver` conectados por `GestorEscenas`.
- Nombre del jugador centralizado en `PerfilJugador` (PlayerPrefs). Ya no se sobrescribe con "Cafetero" al volver del panel de preparación.
- `EstadoPartida` (vidas, nivel, puntos, nombre, eventos) en memoria, preparado para la lógica futura. Todavía nadie lo consume salvo `GestorEscenas.IniciarPartidaNueva`.
- `HUDJuego`: muestra `Jugador: <nombre>` y abre/cierra la pausa con **ESC** (Input System), solo en `JuegoPrincipal`.
- Pausa aditiva: `Time.timeScale = 0`, cámara/EventSystem/luz de la escena `Pausa` desactivados, Canvas con `sortingOrder = 100`.
- Dobbá capa de oscurecido: `FondoPausa` (alpha `0.8`, sin tocar) dentro del Canvas de `Pausa`, y `OscurecidoJuego` (alpha `0.28`) creado en runtime dentro del Canvas de `JuegoPrincipal` y removido al reanudar.

### Animaciones (Animator de Unity, no código de frames)
Carpeta `Assets/Animaciones/` con 9 clips y 6 controladores generados por `Assets/Editor/GenerarAnimacionesCAFY.cs` (`Herramientas > CAFY > Generar animaciones y teléfono`):

| Asset | Uso | Configuración |
| --- | --- | --- |
| `CoffeeInicio` | nubes del `MenuPrincipal` | 3 frames, 2.5 fps, loop |
| `CafyFinal` | nubes de `GameOver` | 6 frames, 2.5 fps, loop |
| `Motero` | moto en `JuegoPrincipal` | 5 frames, 8 fps, loop |
| `Jarabe` | jarabe en `JuegoPrincipal` | 3 frames, 8 fps, loop |
| `TelefonoSube` | subida del teléfono | 12 fps, one-shot, **sin el frame `_0`** |
| `TelefonoBaja` | bajada del teléfono | 13 fps, one-shot |
| `TelefonoArribaQuieto` | teléfono abierto (`SpriteTelefonoBase`) | quieto |
| `TelefonoAbajoQuieto` | reposo (`SpriteTelefonoAbajo_12`, solo la parte de arriba) | quieto |
| `TelefonoNoti` | notificación de pedidos (sin activar) | 6 fps, loop |

El generador es idéntico y está actualizado con todas las correcciones: no reincorpora el frame `_0`, usa el último frame de bajada para el reposo, fija el ancho del panel y asigna el sprite correcto al `Image`. Si se borra el marcador `Temp/cafy_animaciones_generadas`, se vuelve a ejecutar solo al importar.

### Teléfono (solo visual)
- `ControladorTelefono.cs`: clic → sube; segundo clic → baja. `Button` persistente cableado al `Image` del `Telefono`.
- `PanelOpcionesTelefono` con `TextoOpciones`, `btnPedidos` y `btnRecetas` (TextMeshPro + imágenes UI). Los botones solo escriben texto de prueba; **no hay lógica de pedidos/recetas aún**.
- `NotificacionPedido` creada como gancho para futuros encargos (`MostrarNotificacion(bool)`), oculta por defecto.
- Correcciones visuales de esta fase:
  - Botones subidos dentro del panel.
  - Reposo muestra solo la parte de arriba (`SpriteTelefonoAbajo_12`).
  - `SpriteTelefonoBase.png.meta` con rect `x:0, width:32` para que el teléfono abierto mida lo mismo que el oculto (~252 px en un `RectTransform` de 505 px).
  - Panel de botones reducido a `0.25–0.75` del ancho del teléfono y notificación reubicada sobre el cuerpo.

### Fondos
- Imágenes de fondo integradas en `MenuPrincipal`, `GameOver` y `JuegoPrincipal`, en la posición ajustada manualmente. **No mover sus RectTransforms.**

## Pendiente (NO está en Fase 1)
- Lógica de pedidos, preparación, entrega y vidas.
- Cablear `EstadoPartida.PartidaPerdida` con `GestorEscenas.IrAGameOver()`.
- Animación de la cafetera y del minijuego (los paneles existen pero no tienen contenido funcional).
- Quitar el texto `(próximamente)` de los botones del teléfono cuando llegue su lógica.

## Verificación
- `dotnet build` de los scripts con las DLL del editor: **0 errores, 0 advertencias**.
- Clips, controladores y cableado verificados en los `.unity` y en los logs de Unity.
