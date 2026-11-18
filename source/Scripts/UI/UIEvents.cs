using System;
using Godot;

namespace RoomPlanner3D.UI
{
    /// <summary>
    /// UIEvents — Bus de eventos estático que desacopla completamente la capa
    /// de presentación (UI) del mundo 3D.
    ///
    /// Flujo:
    ///   UI → emite eventos → Controllers 3D escuchan y actúan.
    ///   Controllers 3D → emiten señales Godot → UIManager escucha y actualiza UI.
    /// </summary>
    public static class UIEvents
    {
        // ── Catálogo → Mundo 3D ───────────────────────────────────────────────

        /// <summary>
        /// Se emite cuando el usuario hace clic en un ítem del catálogo.
        /// Payload: (furnitureTypeId, label).
        /// </summary>
        public static event Action<string, string>? FurnitureSpawnRequested;

        // ── Inspector → RoomBuilder ───────────────────────────────────────────

        /// <summary>Se emite cuando el usuario cambia dimensiones de habitación.</summary>
        public static event Action<float, float, float>? RoomResizeRequested;

        /// <summary>Se emite cuando el usuario cambia colores de superficies.</summary>
        public static event Action<Color, Color, Color, Color, Color>? RoomRepaintRequested;

        /// <summary>
        /// Se emite para generar paleta cromática armónica.
        /// Payload: (baseColor, harmonyMode).
        /// </summary>
        public static event Action<Color, string>? ColorHarmonyRequested;

        // ── Inspector → FurnitureController ──────────────────────────────────

        /// <summary>Se emite cuando el usuario rota el mueble seleccionado.</summary>
        public static event Action<float>? FurnitureRotateRequested;

        /// <summary>Se emite cuando el usuario elimina el mueble seleccionado.</summary>
        public static event Action? FurnitureDeleteRequested;

        /// <summary>Se emite cuando el usuario cambia el color del mueble seleccionado.</summary>
        public static event Action<Color>? FurnitureColorChangeRequested;

        /// <summary>Se emite para cargar una textura/foto personalizada al mueble seleccionado.</summary>
        public static event Action<string>? FurnitureTextureChangeRequested;

        /// <summary>Se emite para ajustar las medidas exactas del mueble seleccionado (ancho, profundidad, alto).</summary>
        public static event Action<float, float, float>? FurnitureResizeRequested;

        // ── TopBar → Persistencia y Acciones Globales ────────────────────────

        /// <summary>Se emite cuando el usuario presiona Deshacer.</summary>
        public static event Action? UndoRequested;

        /// <summary>Se emite cuando el usuario quiere guardar el layout a JSON.</summary>
        public static event Action<string>? SaveLayoutRequested;

        /// <summary>Se emite cuando el usuario quiere cargar un JSON.</summary>
        public static event Action<string>? LoadLayoutRequested;

        // ── TopBar → Cámara ───────────────────────────────────────────────────

        /// <summary>Se emite al cambiar modo de vista 2D ↔ 3D.</summary>
        public static event Action<bool>? ViewModeChanged;

        /// <summary>Se emite al seleccionar un preset de cámara (Órbita, Cenital, Isométrica, Frontal).</summary>
        public static event Action<string>? CameraModeChanged;

        /// <summary>Se emite cuando el usuario selecciona una plantilla rápida de habitación.</summary>
        public static event Action<string>? TemplateRequested;

        // ── Botón IA Optimizer ────────────────────────────────────────────────

        /// <summary>Se emite cuando el usuario dispara el auto-layout.</summary>
        public static event Action? AutoLayoutRequested;

        // ── Métodos de emisión ────────────────────────────────────────────────

        public static void EmitUndoRequested()
            => UndoRequested?.Invoke();

        public static void EmitFurnitureSpawnRequested(string typeId, string label)
            => FurnitureSpawnRequested?.Invoke(typeId, label);

        public static void EmitRoomResizeRequested(float w, float l, float h)
            => RoomResizeRequested?.Invoke(w, l, h);

        public static void EmitRoomRepaintRequested(Color floor, Color north, Color south, Color east, Color west)
            => RoomRepaintRequested?.Invoke(floor, north, south, east, west);

        public static void EmitColorHarmonyRequested(Color baseColor, string mode)
            => ColorHarmonyRequested?.Invoke(baseColor, mode);

        public static void EmitFurnitureRotateRequested(float degrees)
            => FurnitureRotateRequested?.Invoke(degrees);

        public static void EmitFurnitureDeleteRequested()
            => FurnitureDeleteRequested?.Invoke();

        public static void EmitFurnitureColorChangeRequested(Color c)
            => FurnitureColorChangeRequested?.Invoke(c);

        public static void EmitFurnitureTextureChangeRequested(string imagePath)
            => FurnitureTextureChangeRequested?.Invoke(imagePath);

        public static void EmitFurnitureResizeRequested(float width, float depth, float height)
            => FurnitureResizeRequested?.Invoke(width, depth, height);

        public static void EmitSaveLayoutRequested(string path)
            => SaveLayoutRequested?.Invoke(path);

        public static void EmitLoadLayoutRequested(string path)
            => LoadLayoutRequested?.Invoke(path);

        public static void EmitViewModeChanged(bool is3D)
            => ViewModeChanged?.Invoke(is3D);

        public static void EmitCameraModeChanged(string mode)
            => CameraModeChanged?.Invoke(mode);

        public static void EmitTemplateRequested(string templateName)
            => TemplateRequested?.Invoke(templateName);

        public static void EmitAutoLayoutRequested()
            => AutoLayoutRequested?.Invoke();
    }
}
