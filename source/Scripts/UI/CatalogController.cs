using System.Collections.Generic;
using Godot;

namespace RoomPlanner3D.UI
{
    /// <summary>
    /// CatalogController — Controla el drawer retráctil del catálogo de muebles.
    ///
    /// Muestra muebles organizados por categoría (Dormitorio, Oficina, Living).
    /// Al hacer clic en un ítem, emite UIEvents.FurnitureSpawnRequested.
    /// El panel es retráctil: un botón de pestaña alterna su visibilidad.
    /// </summary>
    public partial class CatalogController : PanelContainer
    {
        // ── Nodos internos ────────────────────────────────────────────────────

        private Button?        _toggleButton;
        private VBoxContainer? _panelContent;
        private TabContainer?  _tabContainer;

        private bool _isExpanded = true;

        // Contenedores por categoría
        private readonly Dictionary<string, GridContainer> _categoryGrids = new();

        // ── Ciclo de vida ─────────────────────────────────────────────────────

        public override void _Ready()
        {
            _toggleButton = GetNodeOrNull<Button>("ToggleButton");
            _panelContent = GetNodeOrNull<VBoxContainer>("PanelContent");
            _tabContainer = GetNodeOrNull<TabContainer>("PanelContent/TabContainer");

            _toggleButton?.Connect(BaseButton.SignalName.Pressed,
                new Callable(this, MethodName.OnTogglePressed));

            BuildCatalogUI();
        }

        // ── Construcción de UI ────────────────────────────────────────────────

        private void BuildCatalogUI()
        {
            if (_tabContainer == null) return;

            // Limpiar tabs existentes
            foreach (Node child in _tabContainer.GetChildren())
                child.QueueFree();
            _categoryGrids.Clear();

            // Agrupar entries por categoría
            var categories = new Dictionary<string, List<FurnitureCatalog.CatalogEntry>>();
            foreach (var entry in FurnitureCatalog.Entries)
            {
                if (!categories.ContainsKey(entry.Category))
                    categories[entry.Category] = new List<FurnitureCatalog.CatalogEntry>();
                categories[entry.Category].Add(entry);
            }

            // Crear una pestaña por categoría
            foreach (var (category, entries) in categories)
            {
                var scroll = new ScrollContainer { Name = category };
                scroll.SetCustomMinimumSize(new Vector2(0, 200));

                var grid = new GridContainer { Columns = 2 };
                grid.AddThemeConstantOverride("h_separation", 8);
                grid.AddThemeConstantOverride("v_separation", 8);
                _categoryGrids[category] = grid;

                foreach (var entry in entries)
                    grid.AddChild(CreateFurnitureButton(entry));

                scroll.AddChild(grid);
                _tabContainer.AddChild(scroll);
            }

            // Pestaña adicional: Plantillas prediseñadas
            var tplScroll = new ScrollContainer { Name = "Plantillas" };
            tplScroll.SetCustomMinimumSize(new Vector2(0, 200));
            var tplGrid = new GridContainer { Columns = 2 };
            tplGrid.AddThemeConstantOverride("h_separation", 8);
            tplGrid.AddThemeConstantOverride("v_separation", 8);

            tplGrid.AddChild(CreateTemplateButton("dormitorio", "🛏 Dormitorio\nCompleto", "Carga dormitorio con cama doble, mesas de noche, armario y escritorio."));
            tplGrid.AddChild(CreateTemplateButton("oficina", "💼 Oficina\nModerna", "Carga oficina con escritorio amplio, silla, estantería y archivero."));
            tplGrid.AddChild(CreateTemplateButton("living", "🛋 Living\nElegante", "Carga living con sofá 3 plazas, mesa de centro, TV stand y sillón."));
            tplGrid.AddChild(CreateTemplateButton("vaciar", "🧹 Vaciar\nEspacio", "Elimina todos los muebles para comenzar desde cero."));

            tplScroll.AddChild(tplGrid);
            _tabContainer.AddChild(tplScroll);
        }

        private Button CreateTemplateButton(string templateId, string label, string tooltip)
        {
            var btn = new Button
            {
                Name              = $"tpl_{templateId}",
                Text              = label,
                CustomMinimumSize = new Vector2(88, 76),
                TooltipText       = tooltip
            };

            var normalStyle = new StyleBoxFlat
            {
                BgColor                 = new Color(0.16f, 0.18f, 0.26f, 0.90f),
                BorderWidthLeft         = 1,
                BorderWidthTop          = 1,
                BorderWidthRight        = 1,
                BorderWidthBottom       = 1,
                BorderColor             = new Color(0.35f, 0.42f, 0.65f, 0.80f),
                CornerRadiusTopLeft     = 8,
                CornerRadiusTopRight    = 8,
                CornerRadiusBottomLeft  = 8,
                CornerRadiusBottomRight = 8,
                ContentMarginLeft       = 4,
                ContentMarginRight      = 4,
                ContentMarginTop        = 6,
                ContentMarginBottom     = 6
            };

            var hoverStyle = new StyleBoxFlat
            {
                BgColor                 = new Color(0.24f, 0.30f, 0.48f, 0.95f),
                BorderWidthLeft         = 1,
                BorderWidthTop          = 1,
                BorderWidthRight        = 1,
                BorderWidthBottom       = 1,
                BorderColor             = new Color(0.55f, 0.70f, 1.0f, 1.0f),
                CornerRadiusTopLeft     = 8,
                CornerRadiusTopRight    = 8,
                CornerRadiusBottomLeft  = 8,
                CornerRadiusBottomRight = 8,
                ContentMarginLeft       = 4,
                ContentMarginRight      = 4,
                ContentMarginTop        = 6,
                ContentMarginBottom     = 6
            };

            btn.AddThemeStyleboxOverride("normal", normalStyle);
            btn.AddThemeStyleboxOverride("hover", hoverStyle);
            btn.AddThemeColorOverride("font_color", new Color(0.92f, 0.95f, 1.0f));
            btn.AddThemeFontSizeOverride("font_size", 11);

            string capturedId = templateId;
            btn.Pressed += () => UIEvents.EmitTemplateRequested(capturedId);
            return btn;
        }

        private Button CreateFurnitureButton(FurnitureCatalog.CatalogEntry entry)
        {
            var btn = new Button
            {
                Name              = $"btn_{entry.Id}",
                Text              = $"{entry.Emoji}\n{entry.Label}",
                CustomMinimumSize = new Vector2(88, 76),
                TooltipText       = $"{entry.Label}\n{entry.Width:F2}m × {entry.Depth:F2}m × {entry.Height:F2}m"
            };

            // Estilos estilo CAD moderno
            var normalStyle = new StyleBoxFlat
            {
                BgColor                 = new Color(0.13f, 0.14f, 0.20f, 0.90f),
                BorderWidthLeft         = 1,
                BorderWidthTop          = 1,
                BorderWidthRight        = 1,
                BorderWidthBottom       = 1,
                BorderColor             = new Color(0.28f, 0.30f, 0.44f, 0.70f),
                CornerRadiusTopLeft     = 8,
                CornerRadiusTopRight    = 8,
                CornerRadiusBottomLeft  = 8,
                CornerRadiusBottomRight = 8,
                ContentMarginLeft       = 4,
                ContentMarginRight      = 4,
                ContentMarginTop        = 6,
                ContentMarginBottom     = 6
            };

            var hoverStyle = new StyleBoxFlat
            {
                BgColor                 = new Color(0.20f, 0.24f, 0.38f, 0.95f),
                BorderWidthLeft         = 1,
                BorderWidthTop          = 1,
                BorderWidthRight        = 1,
                BorderWidthBottom       = 1,
                BorderColor             = new Color(0.45f, 0.58f, 0.90f, 0.95f),
                CornerRadiusTopLeft     = 8,
                CornerRadiusTopRight    = 8,
                CornerRadiusBottomLeft  = 8,
                CornerRadiusBottomRight = 8,
                ContentMarginLeft       = 4,
                ContentMarginRight      = 4,
                ContentMarginTop        = 6,
                ContentMarginBottom     = 6
            };

            var pressedStyle = new StyleBoxFlat
            {
                BgColor                 = new Color(0.12f, 0.38f, 0.70f, 0.95f),
                BorderWidthLeft         = 1,
                BorderWidthTop          = 1,
                BorderWidthRight        = 1,
                BorderWidthBottom       = 1,
                BorderColor             = new Color(0.55f, 0.75f, 1.0f, 1.0f),
                CornerRadiusTopLeft     = 8,
                CornerRadiusTopRight    = 8,
                CornerRadiusBottomLeft  = 8,
                CornerRadiusBottomRight = 8,
                ContentMarginLeft       = 4,
                ContentMarginRight      = 4,
                ContentMarginTop        = 6,
                ContentMarginBottom     = 6
            };

            btn.AddThemeStyleboxOverride("normal", normalStyle);
            btn.AddThemeStyleboxOverride("hover", hoverStyle);
            btn.AddThemeStyleboxOverride("pressed", pressedStyle);
            btn.AddThemeColorOverride("font_color", new Color(0.90f, 0.92f, 0.98f));
            btn.AddThemeFontSizeOverride("font_size", 11);

            // Capturar entry.Id en el lambda (closure)
            string capturedId    = entry.Id;
            string capturedLabel = entry.Label;
            btn.Pressed += () => UIEvents.EmitFurnitureSpawnRequested(capturedId, capturedLabel);

            return btn;
        }

        // ── Toggle ────────────────────────────────────────────────────────────

        private void OnTogglePressed()
        {
            _isExpanded = !_isExpanded;
            if (_panelContent != null)
                _panelContent.Visible = _isExpanded;
            if (_toggleButton != null)
                _toggleButton.Text = _isExpanded ? "◀ Catálogo" : "▶";
        }
    }
}
