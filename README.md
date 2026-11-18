# RoomPlanner3D — Documentación de Arquitectura

> Aplicación 3D de diseño, remodelación y distribución inteligente de habitaciones con Auto-Layout ergonómico.
> **Motor**: Godot 4.3 · **Lenguaje**: C# (.NET 8) · **Renderer**: GL Compatibility (OpenGL 3)

---

## Índice
1. [Estructura del Proyecto](#1-estructura-del-proyecto)
2. [Arquitectura de Agentes](#2-arquitectura-de-agentes)
3. [Skills (Módulos Utilitarios)](#3-skills-módulos-utilitarios)
4. [Scripts Principales de Godot](#4-scripts-principales-de-godot)
5. [Modelo de Datos](#5-modelo-de-datos)
6. [Capa de Presentación e Interfaz Gráfica (UI/UX)](#6-capa-de-presentación-e-interfaz-gráfica-uiux)
7. [Cómo abrir la solución en Godot 4](#7-cómo-abrir-la-solución-en-godot-4)
8. [Compilar y Verificar](#8-compilar-y-verificar)
9. [Convenciones y Normas Ergonómicas](#9-convenciones-y-normas-ergonómicas)

---

## 1. Estructura del Proyecto

```
RoomPlanner3D/
├── agents/                     # Especificaciones de agentes IA
│   ├── SpacePlannerAI.md       # Auto-layout y ergonomía
│   ├── InteriorStylist.md      # Paletas de color y estilos
│   ├── RoomArchitect.md        # Construcción geométrica
│   └── Persistence.md          # Serialización JSON
│
├── skills/                     # Copia de módulos C# (referencia rápida)
│   ├── SkillGridSnapping.cs
│   ├── SkillCollisionAudit.cs
│   ├── SkillColorHarmony.cs
│   ├── SkillLayoutOptimizer.cs
│   └── SkillJsonParser.cs
│
├── entregables/
│   ├── schema_ejemplo.json     # Habitación de ejemplo completa
│   └── README.md               # Este archivo
│
└── source/                     # Proyecto Godot
    ├── project.godot           # Configuración del motor (GL Compatibility)
    ├── RoomPlanner3D.csproj    # Proyecto .NET 8
    ├── Scenes/                 # Escenas .tscn
    │   ├── main.tscn           # Escena principal 3D + UI
    │   └── UI/                 # Componentes gráficos modulares
    │       ├── UIManager.tscn
    │       ├── TopBar.tscn
    │       ├── CatalogDrawer.tscn
    │       ├── InspectorPanel.tscn
    │       ├── IAOptimizerButton.tscn
    │       └── ChatPanel.tscn
    ├── Scripts/
    │   ├── RoomBuilder.cs      # Generación procedural de habitación
    │   ├── CameraController.cs # Cámara orbital y presets de vista
    │   ├── FurnitureController.cs # Interacción con muebles 3D
    │   ├── Skills/             # Módulos utilitarios puros
    │   │   ├── SkillGridSnapping.cs
    │   │   ├── SkillCollisionAudit.cs
    │   │   ├── SkillColorHarmony.cs
    │   │   ├── SkillLayoutOptimizer.cs
    │   │   └── SkillJsonParser.cs
    │   └── UI/                 # Controladores y capa de presentación
    │       ├── UIEvents.cs     # Bus de eventos estático desacoplado
    │       ├── UIManager.cs    # Orquestador raíz CanvasLayer
    │       ├── TopBarController.cs
    │       ├── CatalogController.cs
    │       ├── FurnitureCatalog.cs
    │       ├── InspectorController.cs
    │       ├── IAOptimizerController.cs
    │       └── ChatController.cs
    └── Assets/
        ├── Materials/
        ├── Textures/
        └── Meshes/
```

---

## 2. Arquitectura de Agentes

| Agente | Archivo | Responsabilidad |
|--------|---------|----------------|
| **SpacePlannerAI** | `agents/SpacePlannerAI.md` | Auto-layout, análisis espacial y puntuación ergonómica |
| **InteriorStylist** | `agents/InteriorStylist.md` | Paletas 60-30-10, estilos decorativos, materiales |
| **RoomArchitect** | `agents/RoomArchitect.md` | Geometría procedural, muros, suelo, modos Build/Resize/Repaint |
| **Persistence** | `agents/Persistence.md` | Guardar/cargar `.roomplan` (JSON), checksum SHA-256 |

---

## 3. Skills (Módulos Utilitarios)

Clases `static` puras en `source/Scripts/Skills/`:

### `SkillGridSnapping.cs`
- Raycast al plano `Y=0` desde `Camera3D` y posición de pantalla.
- Snapping configurable: `GridFine = 0.05 m`, `GridNormal = 0.10 m`.
- Conversión bidireccional mundo ↔ celda de grid.

### `SkillCollisionAudit.cs`
- Auditoría AABB con `Aabb.Intersects()` y `MinSurfaceDistance()`.
- Constantes: `MinPassageway = 0.70 m`, `MinAccessClearance = 0.90 m`.
- `AuditAll()` para escena completa; `ViolatesMinDistance()` para validación previa.

### `SkillColorHarmony.cs`
- Conversión HSL ↔ `Color` de Godot.
- Modos: Complementario, Análogo, Triádico, Split-Complementario, Tetrádico.
- Paleta `Palette6030` con dominante / secundario / acento.
- Ratio de contraste WCAG aproximado.

### `SkillLayoutOptimizer.cs`
- Heurística greedy con candidatos perimetrales y de grid.
- Respeta pasillos `>= 0.70 m`, despejes de acceso `>= 0.90 m`.
- Anclaje perimetral configurable por mueble (`PreferredWall`).
- Puntuación ergonómica 0–100.

### `SkillJsonParser.cs`
- Serialización/deserialización con `System.Text.Json`.
- Checksum SHA-256 automático.
- Conversión `Color` ↔ hex HTML, `PositionData` ↔ `Vector3`.

---

## 4. Scripts Principales de Godot

### `RoomBuilder.cs` — `Node3D` con `[Tool]`

```
Exportados: RoomWidth, RoomLength, RoomHeight, WallThickness
            FloorColor, WallNorthColor, WallSouthColor, WallEastColor, WallWestColor
Métodos:    Build(), Resize(w,l,h), Repaint(...), Demolish(), ApplyLayout(data)
Señales:    RoomBuilt(width, length, height), RoomDemolished()
```

Genera `StaticBody3D + MeshInstance3D + CollisionShape3D` para suelo y 4 muros,
cada uno con su propio `StandardMaterial3D` independiente.

### `CameraController.cs` — `Camera3D`

```
Exportados: ZoomMin/Max/Speed/Smoothness, OrbitSpeed, PitchMin/Max, PivotOffset
Controles:  Botón derecho + arrastrar → orbitar (yaw libre, pitch clamped [-80°, -5°])
            Rueda del ratón → zoom suave interpolado
Métodos:    SetPivot(pos), ResetView(), SetZoom(dist)
```

### `FurnitureController.cs` — `Node3D`

```
Controles:  Clic izquierdo → seleccionar
            Clic + arrastrar → mover con snapping
            R → rotar 45°  |  Shift+R → rotar 90°
            Escape → deseleccionar
Señales:    FurnitureSelected, FurnitureDeselected, FurnitureMoved,
            FurnitureRotated, CollisionDetected
Métodos:    RegisterFurniture(node), SpawnFurniture(data)
```

Feedback visual: **azul** = seleccionado, **rojo** = colisión detectada.

---

## 5. Modelo de Datos

`entregables/schema_ejemplo.json` — habitación de ejemplo:
- Dimensiones: **4.5 m × 6.0 m × 2.6 m**
- Paleta contemporánea: dominante `#E8DCC8`, secundario `#7A6652`, acento `#3A7CA5`
- Pared oeste como **accent wall** en azul
- 6 muebles posicionados (puntuación ergonómica global: **89/100**)
- Ratio de ocupación: **31.2 %**, pasillo mínimo: **0.72 m**

Clase C# raíz: `RoomLayoutData` en `SkillJsonParser.cs`.

---

## 6. Capa de Presentación e Interfaz Gráfica (UI/UX)

La capa de presentación implementa una interfaz gráfica moderna, responsiva y de temática oscura (*Modern Dark CAD UI*), diseñada para maximizar el área visible de la habitación 3D manteniendo un rendimiento fluido de 60 FPS sin sobrecarga a la GPU.

### Jerarquía de Nodos y Escenas UI

La orquestación de la UI está centralizada en un `CanvasLayer` con `layer = 10` que se instancia directamente en `main.tscn`:

```
UIManager (CanvasLayer) [Script: UIManager.cs]
├── TopBar (PanelContainer) [Script: TopBarController.cs]
│   └── HBox (HBoxContainer)
│       ├── AppIcon & TitleLabel ("🏠 RoomPlanner 3D")
│       ├── VSeparator
│       ├── DimensionsLabel ("4.5m × 6.0m × 2.6m")
│       ├── Spacer (Control, expand)
│       ├── CameraModeOption (OptionButton: Órbita, Cenital 2D, Isométrica, Frontal)
│       ├── ViewToggleButton (Button: 🎥 3D ↔ 📐 2D)
│       ├── UndoButton (Button: ↩ Deshacer)
│       ├── SaveButton & LoadButton (💾 Guardar / 📂 Cargar JSON)
│       └── SaveConfirmLabel ("✓ Guardado: layout.json")
│
├── CatalogDrawer (PanelContainer, anclado a la izquierda) [Script: CatalogController.cs]
│   └── HBoxOuter (HBoxContainer)
│       ├── PanelContent (VBoxContainer retráctil)
│       │   ├── CatalogHeader ("📦 Catálogo")
│       │   └── TabContainer (Categorías: Dormitorio, Oficina, Living, 🌟 Plantillas)
│       │       └── [Categoría / Plantillas] (ScrollContainer → GridContainer 2 cols con tarjetas)
│       └── ToggleButton (Button: ◀ / ▶ Catálogo)
│
├── InspectorPanel (PanelContainer, anclado a la derecha) [Script: InspectorController.cs]
│   └── TabContainer (3 Pestañas)
│       ├── Tab 0 "Habitación" (ScrollContainer)
│       │   └── Sliders + SpinBoxes sincronizados en tiempo real (Ancho, Largo, Alto)
│       ├── Tab 1 "Materiales" (ScrollContainer)
│       │   ├── ColorPickerButtons para Suelo y Paredes (Norte, Sur, Este, Oeste)
│       │   └── Selector de Modo de Armonía + Botón "🎨 Generar Paleta"
│       └── Tab 2 "Mueble" (ScrollContainer)
│           ├── Nombre del mueble activo & Posición (X, Z)
│           ├── Botones de Rotación rápida: ↻ +45° y ↻ +90°
│           ├── Color individual del mueble (ColorPickerButton)
│           ├── Alerta visual de proximidad/colisión
│           └── Botón "🗑 Eliminar Mueble"
│
├── IAOptimizerButton (PanelContainer, anclado inferior central) [Script: IAOptimizerController.cs]
│   └── VBox (VBoxContainer)
│       ├── MainButton ("✨ Optimizar Espacio (IA)", pulsación suave en Idle)
│       ├── StatusLabel ("Calculando...", con indicador de progreso)
│       └── ScoreLabel ("Ergonomía: 89/100", flash verde tras calcular)
│
└── ChatPanel (Control, anclado inferior derecho) [Script: ChatController.cs]
    ├── ToggleButton ("💬 Asistente IA", píldora flotante)
    └── ChatWindow (PanelContainer translúcido, colapsable)
        └── VBox
            ├── Header ("🤖 Diseñador IA RoomPlanner", botones 🗑 Limpiar y ✕ Minimizar)
            ├── Scroll (Burbujas de chat interactivas con Markdown)
            ├── Chips (Píldoras rápidas: ❄️ Aire, 🪟 Ventana, 🛏 Cama, 📐 5x6m, ✨ Optimizar)
            └── InputBar (LineEdit + Botón ➤ Enviar)
```

### Arquitectura Desacoplada por Señales (Event Bus Pattern)

La UI y el mundo 3D no se referencian mutuamente de forma acoplada. La comunicación se realiza mediante el bus estático `UIEvents` (eventos C# de tipado estricto) y señales nativas de Godot 4:

```
[Componentes UI] ─────(UIEvents: Action)─────► [UIManager] ─────(Métodos 3D)─────► [Nodos 3D]
     ▲                                                │                                 │
     │                                                │                                 │
     └────────────(Actualización UI)──────────────────┴────────(Godot Signals)──────────┘
```

#### Flujo de Señales y Eventos

| Origen | Evento / Señal | Destino | Acción Realizada |
|--------|----------------|---------|------------------|
| **CatalogController** | `UIEvents.FurnitureSpawnRequested` | `UIManager` → `FurnitureController` | Instancia el mueble seleccionado según `FurnitureCatalog`, lo posiciona sobre el plano del suelo y lo registra en la pila de deshacer. |
| **CatalogController** | `UIEvents.TemplateRequested` | `UIManager` | Carga una plantilla arquitectónica completa (Dormitorio, Oficina, Living o Vaciar) con muebles, medidas y colores armonizados. |
| **ChatController** | Lenguaje Natural → `UIEvents` | `UIManager` & Controllers 3D | Interpreta comandos y preguntas del usuario ("pon un aire acondicionado", "agrega una ventana", "hazla de 5x6", "optimiza") y dispara las acciones correspondientes con feedback conversacional. |
| **InspectorController** | `UIEvents.RoomResizeRequested` | `UIManager` → `RoomBuilder` | Redimensiona paredes, suelo y cuadrícula CAD proceduralmente en tiempo real y reubica el pivote de la cámara. |
| **InspectorController** | `UIEvents.RoomRepaintRequested` | `UIManager` → `RoomBuilder` | Actualiza albedo de materiales StandardMaterial3D de suelo y muros sin reconstruir geometrías. |
| **InspectorController** | `UIEvents.ColorHarmonyRequested` | `UIManager` → `SkillColorHarmony` | Computa paleta 60-30-10 armónica y repinta la habitación de acuerdo a las reglas estéticas. |
| **InspectorController** | `UIEvents.FurnitureRotateRequested` | `UIManager` → Mueble 3D activo | Rota el objeto seleccionado en su eje Y con snapping angular y registro en Undo. |
| **InspectorController** | `UIEvents.FurnitureDeleteRequested` | `UIManager` → Mueble 3D activo | Libera el nodo (`QueueFree()`), deselecciona y guarda estado para posible Undo. |
| **InspectorController** | `UIEvents.FurnitureColorChangeRequested` | `UIManager` → Mueble 3D activo | Actualiza y preserva el color personalizado del mueble en tiempo real. |
| **InspectorController** | `UIEvents.FurnitureResizeRequested` | `UIManager` → `FurnitureController` | Modifica en tiempo real las dimensiones 3D exactas (Ancho, Profundidad, Alto) de la malla y colisión del mueble. |
| **InspectorController** | `UIEvents.FurnitureTextureChangeRequested` | `UIManager` → `FurnitureController` | Abre selector de archivos e importa texturas/fotografías reales (.png, .jpg, .webp) mapeadas directamente sobre el material del mueble. |
| **TopBarController** | `UIEvents.CameraModeChanged` | `UIManager` → `CameraController` | Alterna entre presets de cámara: Órbita Libre, Cenital (2D top-down), Isométrica y Frontal. |
| **TopBarController** | `UIEvents.UndoRequested` | `UIManager` (`_undoStack`) | Deshace la última acción (spawn, resize, rotación, repaint o delete). |
| **TopBarController** | `UIEvents.SaveLayoutRequested` | `UIManager` → `SkillJsonParser` | Abre `FileDialog` nativo y exporta la habitación, muros, puntos de acceso y todos los muebles a JSON con hash SHA-256. |
| **TopBarController** | `UIEvents.LoadLayoutRequested` | `UIManager` → `SkillJsonParser` | Abre `FileDialog` nativo, reconstruye la habitación e instancia todos los muebles guardados. |
| **IAOptimizerButton** | `UIEvents.AutoLayoutRequested` | `UIManager` → `SkillLayoutOptimizer` | Ejecuta la heurística greedy ergonómica respetando despejes de puertas y ventanas, anclaje perimetral según catálogo y devuelve el score ergonómico. |
| **FurnitureController** | Godot `FurnitureSelected` | `UIManager` → `InspectorController` | Muestra pestaña de mueble activo, sincroniza posición y carga su color en el picker. |
| **FurnitureController** | Godot `FurnitureDeselected` | `UIManager` → `InspectorController` | Limpia inspector y retorna a la pestaña de habitación. |
| **FurnitureController** | Godot `CollisionDetected` | `UIManager` → `InspectorController` | Dispara alerta visual temporal "⚠ Colisión detectada" en color ámbar. |
| **RoomBuilder** | Godot `RoomBuilt` | `UIManager` → `TopBarController` & `Inspector` | Sincroniza labels de dimensiones y centra el pivote de la cámara automáticamente. |

---

## 7. Cómo abrir la solución en Godot 4

### Requisitos
- **Godot 4.3** con soporte C# — [godotengine.org/download](https://godotengine.org/download)
- **.NET 8 SDK** — [dotnet.microsoft.com/download/dotnet/8.0](https://dotnet.microsoft.com/download/dotnet/8.0)

### Pasos

1. Abrir Godot → **Importar proyecto** → navegar a `source/` → seleccionar `project.godot`.
2. Godot detectará el `.csproj` y compilará los scripts C# automáticamente.
3. Verificar en **Editor → Editor Settings → Mono → Builds** que el SDK sea .NET 8.

### Escena principal sugerida (`source/Scenes/Main.tscn`)

```
Main (Node3D)
├── CameraPivot (Node3D)
│   └── Camera3D  [script: CameraController.cs]
├── DirectionalLight3D
├── WorldEnvironment
├── RoomNode (Node3D)  [script: RoomBuilder.cs]
└── FurnitureRoot (Node3D)  [script: FurnitureController.cs]
```

---

## 8. Compilar y Verificar

```powershell
cd source
dotnet build RoomPlanner3D.csproj
```

**Resultado esperado:**
```
Build succeeded.
    0 Error(s)
    0 Warning(s)
```

> Nota: la compilación standalone depende del NuGet feed de Godot (`Godot.NET.Sdk`).
> Si falla por red, abrir el proyecto en Godot para compilar de forma integrada.

---

## 9. Convenciones y Normas Ergonómicas

| Constante | Valor | Descripción |
|-----------|-------|-------------|
| `MinPassageway` | `0.70 m` | Distancia mínima entre muebles |
| `MinAccessClearance` | `0.90 m` | Despeje frente a puertas/ventanas |
| `GridFine` | `0.05 m` | Snapping de precisión fina |
| `GridNormal` | `0.10 m` | Snapping estándar |
| `WallThickness` | `0.15 m` | Grosor de muros por defecto |
| Cama ↔ pared lateral | `0.60 m` | Espacio mínimo de acceso |
| Ratio de ocupación | `25–40 %` | Zona óptima para layout |

### Regla 60-30-10
- **60 %** Dominante — paredes y suelo (tonos neutros, claros)
- **30 %** Secundario — muebles principales
- **10 %** Acento — textiles y detalles decorativos

---

*Generado automáticamente — 2026-10-29*
