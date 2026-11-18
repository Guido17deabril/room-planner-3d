using Godot;

namespace RoomPlanner3D.UI
{
    /// <summary>
    /// TopBarController — Controla la barra superior de la UI.
    ///
    /// Contiene:
    /// - Etiqueta de título del proyecto y dimensiones del cuarto.
    /// - Botón Vista 2D/3D.
    /// - Botones Deshacer, Guardar JSON y Cargar JSON.
    /// - Feedback visual de confirmación tras guardado.
    /// </summary>
    public partial class TopBarController : PanelContainer
    {
        // ── Nodos internos ────────────────────────────────────────────────────

        private Label?        _titleLabel;
        private Label?        _dimensionsLabel;
        private OptionButton? _cameraModeOption;
        private Button?       _viewToggleButton;
        private Button?       _undoButton;
        private Button?       _saveButton;
        private Button?       _loadButton;
        private Label?        _saveConfirmLabel;
        private FileDialog?   _fileDialog;

        private bool  _is3DMode       = true;
        private bool  _isSavingMode   = false;
        private float _confirmTimer   = 0f;
        private bool  _showingConfirm = false;

        // ── Ciclo de vida ─────────────────────────────────────────────────────

        public override void _Ready()
        {
            _titleLabel        = GetNodeOrNull<Label>("HBox/TitleLabel");
            _dimensionsLabel   = GetNodeOrNull<Label>("HBox/DimensionsLabel");
            _cameraModeOption  = GetNodeOrNull<OptionButton>("HBox/CameraModeOption");
            _viewToggleButton  = GetNodeOrNull<Button>("HBox/ViewToggleButton");
            _undoButton        = GetNodeOrNull<Button>("HBox/UndoButton");
            _saveButton        = GetNodeOrNull<Button>("HBox/SaveButton");
            _loadButton        = GetNodeOrNull<Button>("HBox/LoadButton");
            _saveConfirmLabel  = GetNodeOrNull<Label>("HBox/SaveConfirmLabel");

            // Configurar FileDialog para guardar y cargar archivos .json
            _fileDialog = new FileDialog
            {
                Name            = "LayoutFileDialog",
                Access          = FileDialog.AccessEnum.Filesystem,
                UseNativeDialog = true,
                Filters         = new[] { "*.json ; Archivos de RoomPlan JSON" }
            };
            _fileDialog.FileSelected += OnFileSelected;
            AddChild(_fileDialog);

            // Configurar selector de cámara
            if (_cameraModeOption != null)
            {
                _cameraModeOption.Clear();
                _cameraModeOption.AddItem("🎥 Órbita 3D", 0);
                _cameraModeOption.AddItem("📐 Cenital (2D)", 1);
                _cameraModeOption.AddItem("📐 Isométrica", 2);
                _cameraModeOption.AddItem("👁 Frontal", 3);
                _cameraModeOption.Select(0);

                _cameraModeOption.Connect(OptionButton.SignalName.ItemSelected,
                    Callable.From((long index) => OnCameraModeSelected((int)index)));
            }

            // Conectar botones (Godot signals → Callable)
            _viewToggleButton?.Connect(BaseButton.SignalName.Pressed,
                new Callable(this, MethodName.OnViewTogglePressed));
            _undoButton?.Connect(BaseButton.SignalName.Pressed,
                Callable.From(() => UIEvents.EmitUndoRequested()));
            _saveButton?.Connect(BaseButton.SignalName.Pressed,
                new Callable(this, MethodName.OnSavePressed));
            _loadButton?.Connect(BaseButton.SignalName.Pressed,
                new Callable(this, MethodName.OnLoadPressed));

            // Estado inicial
            if (_saveConfirmLabel != null)
                _saveConfirmLabel.Visible = false;
        }

        public override void _Process(double delta)
        {
            if (!_showingConfirm) return;
            _confirmTimer -= (float)delta;
            if (_confirmTimer <= 0f)
            {
                _showingConfirm = false;
                if (_saveConfirmLabel != null)
                    _saveConfirmLabel.Visible = false;
            }
        }

        // ── API pública ───────────────────────────────────────────────────────

        /// <summary>Actualiza el texto de dimensiones en la TopBar.</summary>
        public void UpdateRoomInfo(float width, float length, float height)
        {
            if (_dimensionsLabel != null)
                _dimensionsLabel.Text = $"{width:F1}m × {length:F1}m × {height:F1}m";
        }

        /// <summary>Muestra una confirmación no invasiva de guardado.</summary>
        public void ShowSaveConfirmation(string path)
        {
            if (_saveConfirmLabel == null) return;
            string filename = System.IO.Path.GetFileName(path);
            _saveConfirmLabel.Text    = $"✓ Guardado: {filename}";
            _saveConfirmLabel.Visible = true;
            _showingConfirm           = true;
            _confirmTimer             = 3.5f; // 3.5 segundos visible
        }

        // ── Handlers de botones ───────────────────────────────────────────────

        private void OnViewTogglePressed()
        {
            _is3DMode = !_is3DMode;
            if (_viewToggleButton != null)
                _viewToggleButton.Text = _is3DMode ? "🎥 Vista 3D" : "📐 Vista 2D";

            if (_cameraModeOption != null)
                _cameraModeOption.Select(_is3DMode ? 0 : 1);

            UIEvents.EmitViewModeChanged(_is3DMode);
        }

        private void OnCameraModeSelected(int index)
        {
            if (_cameraModeOption == null) return;
            string selectedText = _cameraModeOption.GetItemText(index);
            _is3DMode = index != 1; // 1 es Cenital (2D)
            if (_viewToggleButton != null)
                _viewToggleButton.Text = _is3DMode ? "🎥 Vista 3D" : "📐 Vista 2D";

            UIEvents.EmitCameraModeChanged(selectedText);
        }

        private void OnSavePressed()
        {
            if (_fileDialog != null)
            {
                _isSavingMode = true;
                _fileDialog.FileMode = FileDialog.FileModeEnum.SaveFile;
                _fileDialog.Title = "Guardar Diseño de Habitación (JSON)";
                _fileDialog.CurrentFile = "habitacion.json";
                _fileDialog.PopupCentered(new Vector2I(800, 500));
            }
            else
            {
                string defaultPath = System.IO.Path.Combine(
                    System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments),
                    "RoomPlanner3D_layout.json"
                );
                UIEvents.EmitSaveLayoutRequested(defaultPath);
            }
        }

        private void OnLoadPressed()
        {
            if (_fileDialog != null)
            {
                _isSavingMode = false;
                _fileDialog.FileMode = FileDialog.FileModeEnum.OpenFile;
                _fileDialog.Title = "Cargar Diseño de Habitación (JSON)";
                _fileDialog.PopupCentered(new Vector2I(800, 500));
            }
            else
            {
                string defaultPath = System.IO.Path.Combine(
                    System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments),
                    "RoomPlanner3D_layout.json"
                );
                if (System.IO.File.Exists(defaultPath))
                    UIEvents.EmitLoadLayoutRequested(defaultPath);
            }
        }

        private void OnFileSelected(string path)
        {
            if (_isSavingMode)
                UIEvents.EmitSaveLayoutRequested(path);
            else
                UIEvents.EmitLoadLayoutRequested(path);
        }
    }
}
