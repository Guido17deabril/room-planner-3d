using Godot;
using RoomPlanner3D.Skills;

namespace RoomPlanner3D.UI
{
    /// <summary>
    /// InspectorController — Panel lateral derecho con tres secciones en TabContainer:
    ///
    /// Tab 0 "Habitación": Sliders/SpinBoxes para Ancho, Largo, Alto.
    /// Tab 1 "Materiales": ColorPickerButtons para suelo y paredes + botón Armonía.
    /// Tab 2 "Mueble":     Visible solo cuando hay mueble seleccionado. Botones
    ///                     Rotar +45°, +90°, Eliminar y ColorPicker individual.
    ///
    /// Se comunica únicamente a través del bus UIEvents.
    /// </summary>
    public partial class InspectorController : PanelContainer
    {
        // ── Nodos de Tab "Habitación" ─────────────────────────────────────────

        private HSlider? _widthSlider;
        private SpinBox? _widthSpin;
        private HSlider? _lengthSlider;
        private SpinBox? _lengthSpin;
        private HSlider? _heightSlider;
        private SpinBox? _heightSpin;
        private Button?  _applyRoomButton;

        // ── Nodos de Tab "Materiales" ─────────────────────────────────────────

        private ColorPickerButton? _floorColorPicker;
        private ColorPickerButton? _wallNorthColorPicker;
        private ColorPickerButton? _wallSouthColorPicker;
        private ColorPickerButton? _wallEastColorPicker;
        private ColorPickerButton? _wallWestColorPicker;
        private Button?            _harmonyButton;
        private OptionButton?      _harmonyModeOption;

        // ── Nodos de Tab "Mueble" ─────────────────────────────────────────────

        private Label?             _furnitureNameLabel;
        private Label?             _furniturePosLabel;
        private Button?            _rotate45Button;
        private Button?            _rotate90Button;
        private Button?            _deleteButton;
        private ColorPickerButton? _furnitureColorPicker;
        private SpinBox?           _furnWidthSpin;
        private SpinBox?           _furnDepthSpin;
        private SpinBox?           _furnHeightSpin;
        private Button?            _loadTextureButton;
        private Label?             _textureStatusLabel;
        private FileDialog?        _textureFileDialog;
        private Label?             _collisionWarningLabel;
        private TabContainer?      _tabContainer;

        // ── Estado ────────────────────────────────────────────────────────────

        private bool _suppressCallbacks = false;
        private float _collisionWarningTimer = 0f;

        // ── Ciclo de vida ─────────────────────────────────────────────────────

        public override void _Ready()
        {
            _tabContainer = GetNodeOrNull<TabContainer>("TabContainer");

            // Tab Habitación (Sliders + SpinBoxes sincronizados)
            _widthSlider     = GetNodeOrNull<HSlider>("TabContainer/Habitacion/VBox/WidthHBox/WidthSlider");
            _widthSpin       = GetNodeOrNull<SpinBox>("TabContainer/Habitacion/VBox/WidthHBox/WidthSpin");
            _lengthSlider    = GetNodeOrNull<HSlider>("TabContainer/Habitacion/VBox/LengthHBox/LengthSlider");
            _lengthSpin      = GetNodeOrNull<SpinBox>("TabContainer/Habitacion/VBox/LengthHBox/LengthSpin");
            _heightSlider    = GetNodeOrNull<HSlider>("TabContainer/Habitacion/VBox/HeightHBox/HeightSlider");
            _heightSpin      = GetNodeOrNull<SpinBox>("TabContainer/Habitacion/VBox/HeightHBox/HeightSpin");
            _applyRoomButton = GetNodeOrNull<Button>("TabContainer/Habitacion/VBox/ApplyButton");

            // Sincronización bidireccional y actualización en tiempo real
            _widthSlider?.Connect(Range.SignalName.ValueChanged,
                Callable.From((double val) => OnWidthChanged(val, fromSlider: true)));
            _widthSpin?.Connect(Range.SignalName.ValueChanged,
                Callable.From((double val) => OnWidthChanged(val, fromSlider: false)));

            _lengthSlider?.Connect(Range.SignalName.ValueChanged,
                Callable.From((double val) => OnLengthChanged(val, fromSlider: true)));
            _lengthSpin?.Connect(Range.SignalName.ValueChanged,
                Callable.From((double val) => OnLengthChanged(val, fromSlider: false)));

            _heightSlider?.Connect(Range.SignalName.ValueChanged,
                Callable.From((double val) => OnHeightChanged(val, fromSlider: true)));
            _heightSpin?.Connect(Range.SignalName.ValueChanged,
                Callable.From((double val) => OnHeightChanged(val, fromSlider: false)));

            // Tab Materiales
            _floorColorPicker     = GetNodeOrNull<ColorPickerButton>("TabContainer/Materiales/VBox/FloorColorPicker");
            _wallNorthColorPicker = GetNodeOrNull<ColorPickerButton>("TabContainer/Materiales/VBox/WallNorthColorPicker");
            _wallSouthColorPicker = GetNodeOrNull<ColorPickerButton>("TabContainer/Materiales/VBox/WallSouthColorPicker");
            _wallEastColorPicker  = GetNodeOrNull<ColorPickerButton>("TabContainer/Materiales/VBox/WallEastColorPicker");
            _wallWestColorPicker  = GetNodeOrNull<ColorPickerButton>("TabContainer/Materiales/VBox/WallWestColorPicker");
            _harmonyButton        = GetNodeOrNull<Button>("TabContainer/Materiales/VBox/HarmonyButton");
            _harmonyModeOption    = GetNodeOrNull<OptionButton>("TabContainer/Materiales/VBox/HarmonyModeOption");

            // Tab Mueble
            _furnitureNameLabel   = GetNodeOrNull<Label>("TabContainer/Mueble/VBox/NameLabel");
            _furniturePosLabel    = GetNodeOrNull<Label>("TabContainer/Mueble/VBox/PosLabel");
            _rotate45Button       = GetNodeOrNull<Button>("TabContainer/Mueble/VBox/Rotate45Button");
            _rotate90Button       = GetNodeOrNull<Button>("TabContainer/Mueble/VBox/Rotate90Button");
            _deleteButton         = GetNodeOrNull<Button>("TabContainer/Mueble/VBox/DeleteButton");
            _furnitureColorPicker = GetNodeOrNull<ColorPickerButton>("TabContainer/Mueble/VBox/FurnitureColorPicker");
            _furnWidthSpin        = GetNodeOrNull<SpinBox>("TabContainer/Mueble/VBox/GridDims/FurnWidthSpin");
            _furnDepthSpin        = GetNodeOrNull<SpinBox>("TabContainer/Mueble/VBox/GridDims/FurnDepthSpin");
            _furnHeightSpin       = GetNodeOrNull<SpinBox>("TabContainer/Mueble/VBox/GridDims/FurnHeightSpin");
            _loadTextureButton    = GetNodeOrNull<Button>("TabContainer/Mueble/VBox/LoadTextureButton");
            _textureStatusLabel   = GetNodeOrNull<Label>("TabContainer/Mueble/VBox/TextureStatusLabel");
            _collisionWarningLabel= GetNodeOrNull<Label>("TabContainer/Mueble/VBox/CollisionWarning");

            // Configurar FileDialog para texturas
            _textureFileDialog = new FileDialog
            {
                Name            = "TextureFileDialog",
                Access          = FileDialog.AccessEnum.Filesystem,
                FileMode        = FileDialog.FileModeEnum.OpenFile,
                UseNativeDialog = true,
                Filters         = new[] { "*.png, *.jpg, *.jpeg, *.webp ; Imágenes de Muebles (*.png, *.jpg, *.webp)" }
            };
            _textureFileDialog.FileSelected += OnTextureFileSelected;
            AddChild(_textureFileDialog);

            _furnWidthSpin?.Connect(Range.SignalName.ValueChanged, Callable.From((double _) => OnFurnDimensionsChanged()));
            _furnDepthSpin?.Connect(Range.SignalName.ValueChanged, Callable.From((double _) => OnFurnDimensionsChanged()));
            _furnHeightSpin?.Connect(Range.SignalName.ValueChanged, Callable.From((double _) => OnFurnDimensionsChanged()));
            _loadTextureButton?.Connect(BaseButton.SignalName.Pressed, Callable.From(() =>
            {
                _textureFileDialog.PopupCentered(new Vector2I(800, 600));
            }));

            // Conectar handlers (Godot signals → Callable)
            _applyRoomButton?.Connect(BaseButton.SignalName.Pressed,
                new Callable(this, MethodName.OnApplyRoomPressed));
            _harmonyButton?.Connect(BaseButton.SignalName.Pressed,
                new Callable(this, MethodName.OnHarmonyPressed));
            _rotate45Button?.Connect(BaseButton.SignalName.Pressed,
                Callable.From(() => UIEvents.EmitFurnitureRotateRequested(45f)));
            _rotate90Button?.Connect(BaseButton.SignalName.Pressed,
                Callable.From(() => UIEvents.EmitFurnitureRotateRequested(90f)));
            _deleteButton?.Connect(BaseButton.SignalName.Pressed,
                Callable.From(() => UIEvents.EmitFurnitureDeleteRequested()));

            _floorColorPicker?.Connect(ColorPickerButton.SignalName.ColorChanged,
                Callable.From((Color _) => EmitRepaint()));
            _wallNorthColorPicker?.Connect(ColorPickerButton.SignalName.ColorChanged,
                Callable.From((Color _) => EmitRepaint()));
            _wallSouthColorPicker?.Connect(ColorPickerButton.SignalName.ColorChanged,
                Callable.From((Color _) => EmitRepaint()));
            _wallEastColorPicker?.Connect(ColorPickerButton.SignalName.ColorChanged,
                Callable.From((Color _) => EmitRepaint()));
            _wallWestColorPicker?.Connect(ColorPickerButton.SignalName.ColorChanged,
                Callable.From((Color _) => EmitRepaint()));
            _furnitureColorPicker?.Connect(ColorPickerButton.SignalName.ColorChanged,
                Callable.From((Color c) => OnFurnitureColorChanged(c)));

            // Poblar OptionButton de modos de armonía
            if (_harmonyModeOption != null)
            {
                _harmonyModeOption.AddItem("Análogo");
                _harmonyModeOption.AddItem("Complementario");
                _harmonyModeOption.AddItem("Triádico");
                _harmonyModeOption.AddItem("Split-Complementario");
                _harmonyModeOption.AddItem("Tetrádico");
            }

            HideFurnitureSection();

            if (_collisionWarningLabel != null)
                _collisionWarningLabel.Visible = false;
        }

        public override void _Process(double delta)
        {
            if (_collisionWarningTimer <= 0f) return;
            _collisionWarningTimer -= (float)delta;
            if (_collisionWarningTimer <= 0f && _collisionWarningLabel != null)
                _collisionWarningLabel.Visible = false;
        }

        // ── API pública ───────────────────────────────────────────────────────

        /// <summary>Actualiza los Sliders y SpinBoxes de habitación sin disparar eventos.</summary>
        public void UpdateRoomDimensions(float width, float length, float height)
        {
            _suppressCallbacks = true;
            if (_widthSpin    != null) _widthSpin.Value    = width;
            if (_widthSlider  != null) _widthSlider.Value  = width;
            if (_lengthSpin   != null) _lengthSpin.Value   = length;
            if (_lengthSlider != null) _lengthSlider.Value = length;
            if (_heightSpin   != null) _heightSpin.Value   = height;
            if (_heightSlider != null) _heightSlider.Value = height;
            _suppressCallbacks = false;
        }

        /// <summary>Muestra la sección de mueble seleccionado.</summary>
        public void ShowFurnitureSection(Node3D furniture)
        {
            if (_furnitureNameLabel != null)
                _furnitureNameLabel.Text = $"🪑 {furniture.Name}";
            UpdateFurniturePosition(furniture.GlobalPosition);

            // Cargar color actual del mueble seleccionado
            MeshInstance3D? mesh = furniture is MeshInstance3D m
                ? m : furniture.GetNodeOrNull<MeshInstance3D>("Mesh");
            if (mesh?.MaterialOverride is StandardMaterial3D mat && _furnitureColorPicker != null)
            {
                _suppressCallbacks = true;
                _furnitureColorPicker.Color = mat.AlbedoColor;
                _suppressCallbacks = false;
            }

            // Cargar dimensiones actuales
            _suppressCallbacks = true;
            float w = furniture.HasMeta("Width") ? furniture.GetMeta("Width").AsSingle() : 1.0f;
            float d = furniture.HasMeta("Depth") ? furniture.GetMeta("Depth").AsSingle() : 1.0f;
            float h = furniture.HasMeta("Height") ? furniture.GetMeta("Height").AsSingle() : 1.0f;

            if (_furnWidthSpin  != null) _furnWidthSpin.Value  = w;
            if (_furnDepthSpin  != null) _furnDepthSpin.Value  = d;
            if (_furnHeightSpin != null) _furnHeightSpin.Value = h;

            if (_textureStatusLabel != null)
            {
                if (furniture.HasMeta("TexturePath"))
                {
                    string path = furniture.GetMeta("TexturePath").AsString();
                    _textureStatusLabel.Text = $"Textura: {System.IO.Path.GetFileName(path)}";
                }
                else
                {
                    _textureStatusLabel.Text = "Sin textura personalizada";
                }
            }
            _suppressCallbacks = false;

            // Cambiar al tab de Mueble automáticamente
            if (_tabContainer != null)
                _tabContainer.CurrentTab = 2;
        }

        /// <summary>Oculta la sección de mueble (tab deshabilitado) y retorna a tab 0.</summary>
        public void HideFurnitureSection()
        {
            if (_furnitureNameLabel != null)
                _furnitureNameLabel.Text = "Sin selección";
            if (_furniturePosLabel != null)
                _furniturePosLabel.Text  = "—";

            if (_tabContainer != null && _tabContainer.CurrentTab == 2)
                _tabContainer.CurrentTab = 0;
        }

        /// <summary>Actualiza la posición mostrada en el inspector.</summary>
        public void UpdateFurniturePosition(Vector3 pos)
        {
            if (_furniturePosLabel != null)
                _furniturePosLabel.Text = $"Pos: ({pos.X:F2}, {pos.Z:F2})";
        }

        /// <summary>Actualiza la rotación mostrada.</summary>
        public void UpdateFurnitureRotation(float degrees)
        {
            if (_furnitureNameLabel != null && _furnitureNameLabel.Text.Contains("°"))
            {
                // El nombre ya tiene grados; solo actualizar si hay label de rotación
            }
        }

        /// <summary>Muestra advertencia de colisión por 2 segundos.</summary>
        public void ShowCollisionWarning()
        {
            if (_collisionWarningLabel == null) return;
            _collisionWarningLabel.Text    = "⚠ Colisión detectada";
            _collisionWarningLabel.Visible = true;
            _collisionWarningTimer         = 2.5f;
        }

        /// <summary>Aplica los colores de una paleta cromática a los ColorPickers.</summary>
        public void ApplyHarmonyPalette(SkillColorHarmony.Palette6030 palette)
        {
            _suppressCallbacks = true;
            if (_floorColorPicker     != null) _floorColorPicker.Color     = palette.Dominant;
            if (_wallNorthColorPicker != null) _wallNorthColorPicker.Color  = palette.Dominant;
            if (_wallSouthColorPicker != null) _wallSouthColorPicker.Color  = palette.Dominant;
            if (_wallEastColorPicker  != null) _wallEastColorPicker.Color   = palette.Secondary;
            if (_wallWestColorPicker  != null) _wallWestColorPicker.Color   = palette.Accent;
            _suppressCallbacks = false;
        }

        // ── Handlers ──────────────────────────────────────────────────────────

        private void OnWidthChanged(double val, bool fromSlider)
        {
            if (_suppressCallbacks) return;
            _suppressCallbacks = true;
            if (fromSlider && _widthSpin != null) _widthSpin.Value = val;
            else if (!fromSlider && _widthSlider != null) _widthSlider.Value = val;
            _suppressCallbacks = false;

            EmitDimensionChange();
        }

        private void OnLengthChanged(double val, bool fromSlider)
        {
            if (_suppressCallbacks) return;
            _suppressCallbacks = true;
            if (fromSlider && _lengthSpin != null) _lengthSpin.Value = val;
            else if (!fromSlider && _lengthSlider != null) _lengthSlider.Value = val;
            _suppressCallbacks = false;

            EmitDimensionChange();
        }

        private void OnHeightChanged(double val, bool fromSlider)
        {
            if (_suppressCallbacks) return;
            _suppressCallbacks = true;
            if (fromSlider && _heightSpin != null) _heightSpin.Value = val;
            else if (!fromSlider && _heightSlider != null) _heightSlider.Value = val;
            _suppressCallbacks = false;

            EmitDimensionChange();
        }

        private void EmitDimensionChange()
        {
            if (_suppressCallbacks) return;
            float w = (float)(_widthSpin?.Value ?? 4.5);
            float l = (float)(_lengthSpin?.Value ?? 6.0);
            float h = (float)(_heightSpin?.Value ?? 2.6);
            UIEvents.EmitRoomResizeRequested(w, l, h);
        }

        private void OnApplyRoomPressed()
        {
            EmitDimensionChange();
        }

        private void OnHarmonyPressed()
        {
            if (_floorColorPicker == null) return;
            string mode = _harmonyModeOption?.Selected switch
            {
                1 => "Complementary",
                2 => "Triadic",
                3 => "SplitComplementary",
                4 => "Tetradic",
                _ => "Analogous"
            };
            UIEvents.EmitColorHarmonyRequested(_floorColorPicker.Color, mode);
        }

        private void EmitRepaint()
        {
            if (_suppressCallbacks) return;
            UIEvents.EmitRoomRepaintRequested(
                _floorColorPicker?.Color     ?? Colors.White,
                _wallNorthColorPicker?.Color  ?? Colors.White,
                _wallSouthColorPicker?.Color  ?? Colors.White,
                _wallEastColorPicker?.Color   ?? Colors.White,
                _wallWestColorPicker?.Color   ?? Colors.White
            );
        }

        private void OnFurnitureColorChanged(Color color)
        {
            UIEvents.EmitFurnitureColorChangeRequested(color);
        }

        private void OnFurnDimensionsChanged()
        {
            if (_suppressCallbacks) return;
            float w = (float)(_furnWidthSpin?.Value ?? 1.0);
            float d = (float)(_furnDepthSpin?.Value ?? 1.0);
            float h = (float)(_furnHeightSpin?.Value ?? 1.0);
            UIEvents.EmitFurnitureResizeRequested(w, d, h);
        }

        private void OnTextureFileSelected(string path)
        {
            if (_textureStatusLabel != null)
                _textureStatusLabel.Text = $"Textura: {System.IO.Path.GetFileName(path)}";
            UIEvents.EmitFurnitureTextureChangeRequested(path);
        }
    }
}
