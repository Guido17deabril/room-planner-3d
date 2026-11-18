using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Godot;
using RoomPlanner3D.Skills;

namespace RoomPlanner3D.UI
{
    /// <summary>
    /// ChatController — Asistente de IA conversacional integrado en la aplicación.
    /// 
    /// Permite al usuario interactuar mediante lenguaje natural en español:
    /// - Agregar muebles, climatización (aires acondicionados, radiadores) y aberturas (ventanas, puertas).
    /// - Redimensionar la habitación (ej. "hazla de 5x6", "4 metros de ancho por 6 de largo").
    /// - Cambiar colores o solicitar paletas cromáticas armónicas 60-30-10.
    /// - Disparar auto-layout ergonómico ("optimiza el espacio").
    /// - Responder preguntas sobre medidas, muebles presentes y auditoría de colisiones.
    /// - Rotar o eliminar el mueble seleccionado.
    /// - Alternar vistas (2D, 3D, Isométrica).
    /// </summary>
    public partial class ChatController : Control
    {
        // ── Nodos de interfaz ─────────────────────────────────────────────────

        private Button?          _toggleButton;
        private PanelContainer?  _chatWindow;
        private Button?          _closeButton;
        private Button?          _clearButton;
        private ScrollContainer? _scrollContainer;
        private VBoxContainer?   _messagesContainer;
        private LineEdit?        _inputEdit;
        private Button?          _sendButton;
        private HBoxContainer?   _chipsContainer;

        private bool _isOpen = false;

        // Referencia al orquestador raíz
        private UIManager? _uiManager;

        // ── Ciclo de vida ─────────────────────────────────────────────────────

        public override void _Ready()
        {
            _toggleButton      = GetNodeOrNull<Button>("ToggleButton");
            _chatWindow        = GetNodeOrNull<PanelContainer>("ChatWindow");
            _closeButton       = GetNodeOrNull<Button>("ChatWindow/VBox/Header/CloseButton");
            _clearButton       = GetNodeOrNull<Button>("ChatWindow/VBox/Header/ClearButton");
            _scrollContainer   = GetNodeOrNull<ScrollContainer>("ChatWindow/VBox/Scroll");
            _messagesContainer = GetNodeOrNull<VBoxContainer>("ChatWindow/VBox/Scroll/Messages");
            _inputEdit         = GetNodeOrNull<LineEdit>("ChatWindow/VBox/InputBar/LineEdit");
            _sendButton        = GetNodeOrNull<Button>("ChatWindow/VBox/InputBar/SendButton");
            _chipsContainer    = GetNodeOrNull<HBoxContainer>("ChatWindow/VBox/ChipsScroll/Chips");

            _uiManager = GetParentOrNull<UIManager>();

            // Conectar eventos de botones
            _toggleButton?.Connect(BaseButton.SignalName.Pressed,
                new Callable(this, MethodName.OnTogglePressed));
            _closeButton?.Connect(BaseButton.SignalName.Pressed,
                new Callable(this, MethodName.OnTogglePressed));
            _clearButton?.Connect(BaseButton.SignalName.Pressed,
                new Callable(this, MethodName.OnClearPressed));
            _sendButton?.Connect(BaseButton.SignalName.Pressed,
                new Callable(this, MethodName.OnSendPressed));

            if (_inputEdit != null)
            {
                _inputEdit.Connect(LineEdit.SignalName.TextSubmitted,
                    Callable.From((string text) => OnTextSubmitted(text)));
            }

            // Conectar chips de sugerencias rápidas
            SetupChips();

            // Mensaje de bienvenida inicial
            AddAssistantMessage(
                "¡Hola! Soy tu **Diseñador IA de RoomPlanner3D** 🏠✨\n\n" +
                "Puedes pedirme en lenguaje natural:\n" +
                "• **Muebles y confort:** *\"Pon un aire acondicionado\"*, *\"Agrega una cama doble\"*, *\"Pon una ventana\"*\n" +
                "• **Medidas:** *\"Haz la habitación de 5x6\"*, *\"Ancho 4.5 largo 6\"*\n" +
                "• **Diseño:** *\"Optimiza el espacio\"*, *\"Aplica una paleta azul\"*\n" +
                "• **Consultas:** *\"¿Qué muebles hay?\"*, *\"¿Hay colisiones?\"*\n\n" +
                "¿En qué te ayudo hoy?"
            );

            // Estado inicial colapsado
            SetChatOpen(false);
        }

        // ── Manejo de ventana ─────────────────────────────────────────────────

        private void OnTogglePressed()
        {
            SetChatOpen(!_isOpen);
        }

        private void SetChatOpen(bool open)
        {
            _isOpen = open;
            if (_chatWindow != null)
                _chatWindow.Visible = _isOpen;
            if (_toggleButton != null)
                _toggleButton.Visible = !_isOpen;

            if (_isOpen)
                _inputEdit?.GrabFocus();
        }

        private void OnClearPressed()
        {
            if (_messagesContainer == null) return;
            foreach (Node child in _messagesContainer.GetChildren())
                child.QueueFree();

            AddAssistantMessage("Historial limpio. ¿Qué cambio deseas hacer en la habitación?");
        }

        private void SetupChips()
        {
            if (_chipsContainer == null) return;

            AddChip("❄️ Aire Acondicionado", "Pon un aire acondicionado split");
            AddChip("🪟 Ventana", "Pon una ventana en la pared");
            AddChip("🛏 Cama Doble", "Pon una cama doble");
            AddChip("🖥 Escritorio", "Agrega un escritorio");
            AddChip("📐 5x6m", "Haz la habitación de 5x6");
            AddChip("✨ Optimizar", "Optimiza la distribución del espacio");
        }

        private void AddChip(string label, string prompt)
        {
            if (_chipsContainer == null) return;

            var chip = new Button
            {
                Text              = label,
                TooltipText       = prompt,
                CustomMinimumSize = new Vector2(0, 26)
            };

            var style = new StyleBoxFlat
            {
                BgColor                 = new Color(0.18f, 0.22f, 0.35f, 0.85f),
                BorderWidthLeft         = 1,
                BorderWidthTop          = 1,
                BorderWidthRight        = 1,
                BorderWidthBottom       = 1,
                BorderColor             = new Color(0.35f, 0.45f, 0.70f, 0.65f),
                CornerRadiusTopLeft     = 12,
                CornerRadiusTopRight    = 12,
                CornerRadiusBottomLeft  = 12,
                CornerRadiusBottomRight = 12,
                ContentMarginLeft       = 8,
                ContentMarginRight      = 8,
                ContentMarginTop        = 2,
                ContentMarginBottom     = 2
            };

            chip.AddThemeStyleboxOverride("normal", style);
            chip.AddThemeFontSizeOverride("font_size", 10);
            chip.AddThemeColorOverride("font_color", new Color(0.85f, 0.90f, 1.0f));

            chip.Pressed += () =>
            {
                if (_inputEdit != null)
                {
                    _inputEdit.Text = prompt;
                    OnSendPressed();
                }
            };

            _chipsContainer.AddChild(chip);
        }

        // ── Envío de mensajes ─────────────────────────────────────────────────

        private void OnSendPressed()
        {
            if (_inputEdit == null) return;
            string text = _inputEdit.Text.Trim();
            if (string.IsNullOrEmpty(text)) return;

            _inputEdit.Text = string.Empty;
            ProcessUserInput(text);
        }

        private void OnTextSubmitted(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return;
            if (_inputEdit != null) _inputEdit.Text = string.Empty;
            ProcessUserInput(text.Trim());
        }

        // ── Motor de Procesamiento de Lenguaje Natural (NLP) ──────────────────

        private void ProcessUserInput(string rawInput)
        {
            AddUserMessage(rawInput);

            string input = rawInput.ToLowerInvariant();

            // 1. AYUDA / QUÉ PUEDES HACER
            if (input.Contains("ayuda") || input.Contains("que puedes") || input.Contains("qué puedes") || input.Contains("comandos"))
            {
                AddAssistantMessage(
                    "💡 **Comandos que entiendo y puedo ejecutar:**\n\n" +
                    "• **Muebles:** *\"Pon un aire acondicionado\"*, *\"Pon una ventana\"*, *\"Agrega una cama doble\"*, *\"Pon un sofá\"*, *\"Pon un escritorio\"*, *\"Pon una puerta\"*\n" +
                    "• **Dimensiones:** *\"Haz el cuarto de 5x6\"*, *\"Ancho 4 largo 5.5 alto 2.8\"*\n" +
                    "• **Optimización:** *\"Optimiza el espacio\"*, *\"Organiza los muebles con IA\"*\n" +
                    "• **Mueble activo:** *\"Gira 90 grados\"*, *\"Gira 45°\"*, *\"Elimina el mueble\"*\n" +
                    "• **Colores:** *\"Aplica una paleta armónica\"*, *\"Armonía complementaria\"*, *\"Modo triádico\"*\n" +
                    "• **Vistas:** *\"Vista 2D\"*, *\"Vista 3D\"*, *\"Isométrica\"*, *\"Frontal\"*\n" +
                    "• **Plantillas:** *\"Plantilla dormitorio\"*, *\"Plantilla oficina\"*, *\"Plantilla living\"*\n" +
                    "• **Consultas:** *\"¿Cuánto mide el cuarto?\"*, *\"¿Qué muebles tengo?\"*, *\"¿Hay colisiones?\"*"
                );
                return;
            }

            // 2. AIRE ACONDICIONADO / CLIMATIZACIÓN
            if (input.Contains("aire") || input.Contains("acondicionado") || input.Contains("clima") || input.Contains("split"))
            {
                UIEvents.EmitFurnitureSpawnRequested("ac_split", "Aire Acondicionado Split");
                AddAssistantMessage("❄️ **Aire Acondicionado Split instalado** en la pared superior (altura 2.05m).\nPuedes hacer clic en él y arrastrarlo a lo largo del muro deseado.");
                return;
            }

            if (input.Contains("radiador") || input.Contains("calefaccion") || input.Contains("estufa"))
            {
                UIEvents.EmitFurnitureSpawnRequested("radiator", "Radiador Térmico");
                AddAssistantMessage("♨️ **Radiador térmico instalado** junto a la pared.");
                return;
            }

            if (input.Contains("ventilador"))
            {
                UIEvents.EmitFurnitureSpawnRequested("ceiling_fan", "Ventilador de Techo");
                AddAssistantMessage("🌀 **Ventilador de techo añadido** en la parte superior.");
                return;
            }

            // 3. VENTANAS Y PUERTAS
            if (input.Contains("ventana") || input.Contains("ventanal"))
            {
                UIEvents.EmitFurnitureSpawnRequested("window_wall", "Ventana Panorámica");
                AddAssistantMessage("🪟 **Ventana Panorámica añadida** a 0.90m de altura de antepecho.\nEl optimizador de espacio respetará el despeje lumínico frente a ella.");
                return;
            }

            if (input.Contains("puerta") || input.Contains("acceso"))
            {
                UIEvents.EmitFurnitureSpawnRequested("door_interior", "Puerta Interior");
                AddAssistantMessage("🚪 **Puerta Interior añadida**.\nEl optimizador respetará el despeje de acceso reglamentario (>= 0.90m).");
                return;
            }

            // 4. DIMENSIONES DE HABITACIÓN (ej. "5x6", "5 por 6", "4.5x6", "ancho 5 largo 7")
            var matchDim = Regex.Match(input, @"(\d+([.,]\d+)?)\s*(?:x|por|×)\s*(\d+([.,]\d+)?)");
            if (matchDim.Success)
            {
                float w = float.Parse(matchDim.Groups[1].Value.Replace(',', '.'), System.Globalization.CultureInfo.InvariantCulture);
                float l = float.Parse(matchDim.Groups[3].Value.Replace(',', '.'), System.Globalization.CultureInfo.InvariantCulture);
                w = Mathf.Clamp(w, 2.0f, 20.0f);
                l = Mathf.Clamp(l, 2.0f, 20.0f);
                float h = 2.6f;

                // Si menciona alto
                var matchH = Regex.Match(input, @"alto\s*(\d+([.,]\d+)?)");
                if (matchH.Success)
                    h = float.Parse(matchH.Groups[1].Value.Replace(',', '.'), System.Globalization.CultureInfo.InvariantCulture);

                UIEvents.EmitRoomResizeRequested(w, l, h);
                AddAssistantMessage($"📐 **Habitación redimensionada** a **{w:F1}m** de ancho × **{l:F1}m** de largo × **{h:F1}m** de alto.\nSuperficie útil: **{w * l:F1} m²**.");
                return;
            }

            // 5. AUTO-LAYOUT / OPTIMIZACIÓN
            if (input.Contains("optimiza") || input.Contains("organiza") || input.Contains("distribuye") || input.Contains("auto layout") || input.Contains("ordenar"))
            {
                UIEvents.EmitAutoLayoutRequested();
                AddAssistantMessage("✨ **Optimizador de Espacio IA ejecutado.**\nHe recalculado la distribución ergonómica respetando distancias mínimas y zonas de paso.");
                return;
            }

            // 6. ROTACIÓN DE MUEBLE
            if (input.Contains("gira") || input.Contains("rota") || input.Contains("rotar"))
            {
                float deg = 90f;
                if (input.Contains("45")) deg = 45f;
                if (input.Contains("180")) deg = 180f;

                UIEvents.EmitFurnitureRotateRequested(deg);
                AddAssistantMessage($"↻ **Mueble rotado +{deg}°** en sentido horario.");
                return;
            }

            // 7. ELIMINAR MUEBLE / LIMPIAR
            if (input.Contains("borra") || input.Contains("elimina") || input.Contains("quita"))
            {
                if (input.Contains("todo") || input.Contains("sala") || input.Contains("habitacion"))
                {
                    UIEvents.EmitTemplateRequested("vaciar");
                    AddAssistantMessage("🧹 **Habitación vaciada por completo.**");
                }
                else
                {
                    UIEvents.EmitFurnitureDeleteRequested();
                    AddAssistantMessage("🗑 **Mueble seleccionado eliminado.**");
                }
                return;
            }

            // 8. PALETA Y ARMONÍA CROMÁTICA
            if (input.Contains("paleta") || input.Contains("color") || input.Contains("armonia") || input.Contains("armonía"))
            {
                string mode = "Analogous";
                if (input.Contains("complemen")) mode = "Complementary";
                else if (input.Contains("triad") || input.Contains("triád")) mode = "Triadic";
                else if (input.Contains("split")) mode = "SplitComplementary";
                else if (input.Contains("tetra")) mode = "Tetradic";

                Color baseColor = input.Contains("azul") ? new Color(0.2f, 0.45f, 0.7f)
                               : input.Contains("verde") ? new Color(0.25f, 0.6f, 0.4f)
                               : input.Contains("calid") || input.Contains("cálid") ? new Color(0.85f, 0.6f, 0.35f)
                               : new Color(0.78f, 0.72f, 0.60f);

                UIEvents.EmitColorHarmonyRequested(baseColor, mode);
                AddAssistantMessage($"🎨 **Paleta armónica ({mode}) generada y aplicada** siguiendo la norma estética 60-30-10.");
                return;
            }

            // 9. VISTAS Y CÁMARA
            if (input.Contains("2d") || input.Contains("cenital") || input.Contains("planta"))
            {
                UIEvents.EmitCameraModeChanged("📐 Cenital (2D)");
                AddAssistantMessage("📐 Cámara cambiada a **Vista Cenital 2D (Top-Down)**.");
                return;
            }
            if (input.Contains("3d") || input.Contains("orbita") || input.Contains("órbita"))
            {
                UIEvents.EmitCameraModeChanged("🎥 Órbita 3D");
                AddAssistantMessage("🎥 Cámara restablecida a **Vista Órbita 3D interactiva**.");
                return;
            }
            if (input.Contains("isomet") || input.Contains("isométrica"))
            {
                UIEvents.EmitCameraModeChanged("📐 Isométrica");
                AddAssistantMessage("📐 Cámara cambiada a **Perspectiva Isométrica CAD**.");
                return;
            }

            // 10. PLANTILLAS PREDISEÑADAS
            if (input.Contains("dormitorio"))
            {
                UIEvents.EmitTemplateRequested("dormitorio");
                AddAssistantMessage("🛏 **Plantilla de Dormitorio Completo cargada**.");
                return;
            }
            if (input.Contains("oficina"))
            {
                UIEvents.EmitTemplateRequested("oficina");
                AddAssistantMessage("💼 **Plantilla de Oficina Moderna cargada**.");
                return;
            }
            if (input.Contains("living") || input.Contains("sala"))
            {
                UIEvents.EmitTemplateRequested("living");
                AddAssistantMessage("🛋 **Plantilla de Living Elegante cargada**.");
                return;
            }

            // 11. CONSULTAS DE ESTADO
            if (input.Contains("cuanto mide") || input.Contains("cuánto mide") || input.Contains("medida") || input.Contains("dimension"))
            {
                var summary = GetRoomMetrics();
                AddAssistantMessage($"📊 **Dimensiones actuales:**\n• Ancho: **{summary.Width:F1} m**\n• Largo: **{summary.Length:F1} m**\n• Alto: **{summary.Height:F1} m**\n• Superficie total: **{summary.Area:F1} m²**");
                return;
            }

            if (input.Contains("que muebles") || input.Contains("qué muebles") || input.Contains("cuantos muebles") || input.Contains("cuántos muebles"))
            {
                var summary = GetRoomMetrics();
                AddAssistantMessage($"📦 **Mobiliario en escena ({summary.FurnitureCount} objetos):**\n" + summary.FurnitureList);
                return;
            }

            if (input.Contains("colision") || input.Contains("colisión") || input.Contains("problema"))
            {
                AddAssistantMessage("🔍 **Auditoría ergonómica en curso:**\n• Pasillo mínimo reglamentario: **0.70 m**\n• Despeje de puertas/ventanas: **0.90 m**\nSi hay dos muebles solapados, el objeto activo se resaltará en color rojo de alerta.");
                return;
            }

            // 12. BÚSQUEDA DIRECTA EN EL CATÁLOGO (ej. "pon una cama", "agrega una mesa", etc.)
            foreach (var entry in FurnitureCatalog.Entries)
            {
                string labelLower = entry.Label.ToLowerInvariant();
                string idClean    = entry.Id.Replace('_', ' ');

                if (input.Contains(labelLower) || input.Contains(idClean) || (entry.Id == "bed_double" && input.Contains("cama")))
                {
                    UIEvents.EmitFurnitureSpawnRequested(entry.Id, entry.Label);
                    AddAssistantMessage($"{entry.Emoji} **{entry.Label} añadido a la habitación.**\nDimensiones: {entry.Width:F2}m × {entry.Depth:F2}m × {entry.Height:F2}m.");
                    return;
                }
            }

            // Fallback amigable
            AddAssistantMessage(
                $"No estoy seguro de cómo interpretar *\"{rawInput}\"*.\n\n" +
                "Prueba con comandos como:\n" +
                "• *\"Pon un aire acondicionado\"*\n" +
                "• *\"Pon una ventana\"*\n" +
                "• *\"Pon una cama doble\"*\n" +
                "• *\"Haz el cuarto de 5x6\"*\n" +
                "• *\"Optimiza el espacio\"*\n" +
                "o escribe *\"Ayuda\"* para ver la lista completa."
            );
        }

        // ── Renderizado de burbujas de chat ───────────────────────────────────

        private void AddUserMessage(string text)
        {
            if (_messagesContainer == null) return;

            var bubble = new PanelContainer();
            var style = new StyleBoxFlat
            {
                BgColor                 = new Color(0.18f, 0.42f, 0.75f, 0.95f),
                CornerRadiusTopLeft     = 10,
                CornerRadiusTopRight    = 10,
                CornerRadiusBottomLeft  = 10,
                CornerRadiusBottomRight = 2,
                ContentMarginLeft       = 10,
                ContentMarginRight      = 10,
                ContentMarginTop        = 6,
                ContentMarginBottom     = 6
            };
            bubble.AddThemeStyleboxOverride("panel", style);
            bubble.SizeFlagsHorizontal = SizeFlags.ShrinkEnd;

            var label = new RichTextLabel
            {
                BbcodeEnabled       = true,
                Text                = text,
                FitContent          = true,
                ScrollActive        = false,
                CustomMinimumSize   = new Vector2(0, 0),
                AutowrapMode        = TextServer.AutowrapMode.WordSmart
            };
            label.AddThemeFontSizeOverride("normal_font_size", 11);
            label.AddThemeColorOverride("default_color", Colors.White);

            bubble.AddChild(label);
            _messagesContainer.AddChild(bubble);

            ScrollToBottom();
        }

        private void AddAssistantMessage(string text)
        {
            if (_messagesContainer == null) return;

            var bubble = new PanelContainer();
            var style = new StyleBoxFlat
            {
                BgColor                 = new Color(0.13f, 0.15f, 0.22f, 0.95f),
                BorderWidthLeft         = 1,
                BorderWidthTop          = 1,
                BorderWidthRight        = 1,
                BorderWidthBottom       = 1,
                BorderColor             = new Color(0.28f, 0.32f, 0.48f, 0.70f),
                CornerRadiusTopLeft     = 10,
                CornerRadiusTopRight    = 10,
                CornerRadiusBottomLeft  = 2,
                CornerRadiusBottomRight = 10,
                ContentMarginLeft       = 10,
                ContentMarginRight      = 10,
                ContentMarginTop        = 8,
                ContentMarginBottom     = 8
            };
            bubble.AddThemeStyleboxOverride("panel", style);
            bubble.SizeFlagsHorizontal = SizeFlags.ExpandFill;

            var label = new RichTextLabel
            {
                BbcodeEnabled       = true,
                Text                = MarkdownToBbCode(text),
                FitContent          = true,
                ScrollActive        = false,
                AutowrapMode        = TextServer.AutowrapMode.WordSmart
            };
            label.AddThemeFontSizeOverride("normal_font_size", 11);
            label.AddThemeColorOverride("default_color", new Color(0.90f, 0.93f, 1.0f));

            bubble.AddChild(label);
            _messagesContainer.AddChild(bubble);

            ScrollToBottom();
        }

        private static string MarkdownToBbCode(string md)
        {
            // Conversión básica de negritas markdown **texto** a [b]texto[/b]
            string res = Regex.Replace(md, @"\*\*(.*?)\*\*", "[b]$1[/b]");
            res = Regex.Replace(res, @"\*(.*?)\*", "[i]$1[/i]");
            return res;
        }

        private void ScrollToBottom()
        {
            Callable.From(() =>
            {
                if (_scrollContainer != null)
                    _scrollContainer.ScrollVertical = (int)_scrollContainer.GetVScrollBar().MaxValue;
            }).CallDeferred();
        }

        // ── Métricas y Consulta de Habitación ─────────────────────────────────

        private struct RoomSummary
        {
            public float Width;
            public float Length;
            public float Height;
            public float Area;
            public int FurnitureCount;
            public string FurnitureList;
        }

        private RoomSummary GetRoomMetrics()
        {
            float w = 4.5f, l = 6.0f, h = 2.6f;
            int count = 0;
            var items = new List<string>();

            var mainNode = GetTree().Root.GetNodeOrNull<Node3D>("Main");
            if (mainNode != null)
            {
                var room = mainNode.GetNodeOrNull<RoomBuilder>("RoomNode");
                if (room != null)
                {
                    w = room.RoomWidth;
                    l = room.RoomLength;
                    h = room.RoomHeight;
                }

                var furnRoot = mainNode.GetNodeOrNull<FurnitureController>("FurnitureRoot");
                if (furnRoot != null)
                {
                    foreach (Node child in furnRoot.GetChildren())
                    {
                        if (child is Node3D n && n.IsInGroup("furniture"))
                        {
                            count++;
                            items.Add($"• {n.Name}");
                        }
                    }
                }
            }

            return new RoomSummary
            {
                Width          = w,
                Length         = l,
                Height         = h,
                Area           = w * l,
                FurnitureCount = count,
                FurnitureList  = items.Count > 0 ? string.Join("\n", items) : "(No hay muebles todavía)"
            };
        }
    }
}

