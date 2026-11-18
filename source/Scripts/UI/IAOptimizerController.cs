using Godot;

namespace RoomPlanner3D.UI
{
    /// <summary>
    /// IAOptimizerController — Botón flotante de Auto-Layout IA.
    ///
    /// Visual: botón prominente con gradiente, ícono de IA y etiqueta.
    /// Estados:
    /// - Idle: pulsa suavemente con una animación de escala (breathe).
    /// - Calculating: spinner/rotación y texto "Calculando...".
    /// - Done: flash de color verde con score ergonómico por 3 segundos.
    ///
    /// Emite UIEvents.AutoLayoutRequested al ser presionado.
    /// </summary>
    public partial class IAOptimizerController : PanelContainer
    {
        // ── Nodos internos ────────────────────────────────────────────────────

        private Button? _mainButton;
        private Label?  _scoreLabel;
        private Label?  _statusLabel;

        // ── Estado de animación ───────────────────────────────────────────────

        private enum AnimState { Idle, Calculating, Done }
        private AnimState _state        = AnimState.Idle;
        private float     _animTimer    = 0f;
        private float     _breathePhase = 0f;
        private bool      _isCalculating = false;

        // Colores del botón según estado
        private static readonly Color ColorIdle        = new(0.18f, 0.52f, 0.89f);  // azul
        private static readonly Color ColorCalculating = new(0.85f, 0.65f, 0.10f);  // ámbar
        private static readonly Color ColorDone        = new(0.20f, 0.75f, 0.45f);  // verde

        // ── Ciclo de vida ─────────────────────────────────────────────────────

        public override void _Ready()
        {
            _mainButton  = GetNodeOrNull<Button>("VBox/MainButton");
            _scoreLabel  = GetNodeOrNull<Label>("VBox/ScoreLabel");
            _statusLabel = GetNodeOrNull<Label>("VBox/StatusLabel");

            _mainButton?.Connect(BaseButton.SignalName.Pressed,
                new Callable(this, MethodName.OnMainButtonPressed));

            if (_scoreLabel  != null) _scoreLabel.Visible  = false;
            if (_statusLabel != null) _statusLabel.Text    = "Optimizar Espacio (IA)";
        }

        public override void _Process(double delta)
        {
            _animTimer    += (float)delta;
            _breathePhase += (float)delta;

            switch (_state)
            {
                case AnimState.Idle:
                    AnimateIdle();
                    break;

                case AnimState.Calculating:
                    AnimateCalculating();
                    break;

                case AnimState.Done:
                    _animTimer -= (float)delta; // se resta para contar hacia atrás
                    if (_animTimer <= 0f)
                        TransitionToIdle();
                    break;
            }
        }

        // ── API pública ───────────────────────────────────────────────────────

        /// <summary>Inicia la animación de cálculo.</summary>
        public void StartCalculationAnimation()
        {
            _state           = AnimState.Calculating;
            _isCalculating   = true;
            _animTimer       = 0f;

            if (_statusLabel != null) _statusLabel.Text    = "Calculando...";
            if (_scoreLabel  != null) _scoreLabel.Visible  = false;
            if (_mainButton  != null) _mainButton.Disabled = true;
        }

        /// <summary>Detiene la animación y muestra el score final.</summary>
        public void StopCalculationAnimation(float ergonomicScore)
        {
            _state           = AnimState.Done;
            _isCalculating   = false;
            _animTimer       = 3.5f; // segundos de feedback

            if (_statusLabel != null) _statusLabel.Text    = "¡Layout Optimizado!";
            if (_scoreLabel  != null)
            {
                _scoreLabel.Text    = $"Ergonomía: {ergonomicScore:F0}/100";
                _scoreLabel.Visible = true;
            }
            if (_mainButton  != null)
            {
                _mainButton.Disabled = false;
                ApplyButtonColor(ColorDone);
            }
        }

        // ── Animaciones ───────────────────────────────────────────────────────

        private void AnimateIdle()
        {
            if (_mainButton == null) return;

            // Efecto "breathe" suave en escala
            float scale = 1.0f + Mathf.Sin(_breathePhase * 1.8f) * 0.015f;
            _mainButton.Scale = new Vector2(scale, scale);

            // Pulso de color muy sutil
            float t     = (Mathf.Sin(_breathePhase * 0.9f) + 1f) * 0.5f;
            Color pulse = ColorIdle.Lerp(ColorIdle.Lightened(0.12f), t);
            ApplyButtonColor(pulse);
        }

        private void AnimateCalculating()
        {
            if (_mainButton == null) return;

            // Pulsación rápida ámbar
            float t     = (Mathf.Sin(_animTimer * 6f) + 1f) * 0.5f;
            Color pulse = ColorCalculating.Lerp(ColorCalculating.Lightened(0.20f), t);
            ApplyButtonColor(pulse);

            // Spinner simulado en el texto
            int frame = (int)(_animTimer * 4f) % 4;
            string[] frames = { "⠋", "⠙", "⠸", "⠴" };
            if (_statusLabel != null)
                _statusLabel.Text = $"{frames[frame]} Calculando...";
        }

        private void TransitionToIdle()
        {
            _state = AnimState.Idle;
            if (_statusLabel != null) _statusLabel.Text   = "Optimizar Espacio (IA)";
            if (_scoreLabel  != null) _scoreLabel.Visible = false;
            if (_mainButton  != null) _mainButton.Scale   = Vector2.One;
            ApplyButtonColor(ColorIdle);
        }

        // ── Handlers ──────────────────────────────────────────────────────────

        private void OnMainButtonPressed()
        {
            if (_isCalculating) return;
            UIEvents.EmitAutoLayoutRequested();
        }

        // ── Utilidades ────────────────────────────────────────────────────────

        private void ApplyButtonColor(Color color)
        {
            if (_mainButton == null) return;
            var style = _mainButton.GetThemeStylebox("normal") as StyleBoxFlat;
            if (style == null)
            {
                style = new StyleBoxFlat
                {
                    BgColor         = color,
                    CornerRadiusTopLeft     = 12,
                    CornerRadiusTopRight    = 12,
                    CornerRadiusBottomLeft  = 12,
                    CornerRadiusBottomRight = 12
                };
                _mainButton.AddThemeStyleboxOverride("normal", style);
                _mainButton.AddThemeStyleboxOverride("hover",  style);
            }
            else
            {
                style.BgColor = color;
            }
        }
    }
}
