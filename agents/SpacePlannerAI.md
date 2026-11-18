# SpacePlannerAI — Especificación del Agente

## Rol y Responsabilidad
Agente de inteligencia artificial encargado de la distribución automática y optimización ergonómica del espacio interior. Recibe un conjunto de muebles y las dimensiones de la habitación, y devuelve un layout óptimo respetando normativas ergonómicas.

## Capacidades

### 1. Análisis Espacial
- Procesamiento de geometría de habitación (ancho × largo × alto).
- Identificación de puntos de acceso (puertas y ventanas).
- Cálculo de zonas de tráfico primario y secundario.

### 2. Auto-Layout Engine
- Asignación de zonas funcionales (zona descanso, zona trabajo, zona social).
- Colocación perimetral de muebles de apoyo.
- Reserva de pasillos mínimos (**>= 0.70 m**) entre objetos.
- Despeje de accesos (**>= 0.90 m**) frente a puertas y ventanas.

### 3. Ergonomía y Normas
- Cumplimiento de estándares ergonómicos residenciales (ISO 9241 adaptado).
- Distancia mínima entre cama y pared lateral: **0.60 m**.
- Distancia entre escritorio y pantalla recomendada: **0.50–0.70 m**.
- Altura libre de circulación respetada automáticamente.

### 4. Salida
- Retorna objeto `RoomLayoutData` serializable a JSON.
- Genera puntuación de ergonomía (0–100) para cada variante propuesta.
- Expone hasta 3 variantes de layout rankeadas por puntuación.

## Interfaces de Entrada
```json
{
  "room": { "width": 4.5, "length": 6.0, "height": 2.6 },
  "furniture": ["bed_double", "wardrobe", "desk", "chair", "nightstand"],
  "access_points": [
    { "type": "door", "wall": "north", "position": 0.9, "width": 0.9 },
    { "type": "window", "wall": "east", "position": 1.5, "width": 1.2 }
  ]
}
```

## Dependencias Internas
- `SkillLayoutOptimizer.cs` — Heurística de posicionamiento.
- `SkillCollisionAudit.cs` — Validación de solapamientos.
- `SkillJsonParser.cs` — Serialización de resultados.

## Versión
- v1.0.0 — Arquitectura inicial con heurística greedy + puntuación ergonómica.
