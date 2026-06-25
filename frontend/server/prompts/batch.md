Dame un JSON array con {{BATCH_SIZE}} fondos de inversión reales europeos de las categorías: {{CATEGORIES}}.
Posiciones del ranking: {{RANK_START}} a {{RANK_END}}.
NO repitas fondos de otros lotes. Usa fondos DISTINTOS con ISINs reales diferentes.

IMPORTANTE: Devuelve SOLO el array JSON, sin texto antes ni después.

Ejemplo de UN elemento del array (usa este formato exacto para cada fondo):

[
  {
    "id": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
    "isin": "IE00B4L5Y983",
    "name": "iShares Core MSCI World UCITS ETF",
    "category": "Renta Variable Global",
    "managementCompany": "BlackRock",
    "riskLevel": 5,
    "netAssetValue": 89.34,
    "currency": "EUR",
    "expenseRatio": 0.20,
    "rating": 5,
    "rankingPosition": {{RANK_START}},
    "lastUpdated": "2026-06-25T10:00:00Z",
    "latestPerformance": {
      "return1Month": 2.1,
      "return3Months": 5.4,
      "return6Months": 8.7,
      "return1Year": 18.5,
      "return3Years": 42.3,
      "return5Years": 78.1,
      "volatility": 14.2,
      "sharpeRatio": 1.3,
      "recordedAt": "2026-06-25T10:00:00Z"
    }
  }
]

Reglas de tipos:
- "id": string UUID v4
- "isin": string, ISIN real del fondo (ej: "IE00B4L5Y983", "LU0392494562")
- "name": string, nombre oficial completo
- "category": string, una de: "Renta Variable Global", "Indexado S&P500", "Tecnología", "Emergentes", "Europa", "ESG", "Mixto", "Renta Fija", "Asia-Pacífico", "Materias Primas"
- "managementCompany": string (BlackRock, Vanguard, Amundi, Fidelity, etc.)
- "riskLevel": number entero 1-7
- "netAssetValue": number decimal (NAV actual en la moneda indicada)
- "currency": string "EUR" o "USD"
- "expenseRatio": number decimal (TER real, ej: 0.20 para 0.20%)
- "rating": number entero 0-5 (estrellas Morningstar)
- "rankingPosition": number entero (posición en el ranking)
- "lastUpdated": string ISO8601
- "latestPerformance": object OBLIGATORIO con:
  - "return1Month" a "return5Years": number decimal (porcentaje, ej: 18.5 para 18.5%)
  - "volatility": number decimal (porcentaje)
  - "sharpeRatio": number decimal
  - "recordedAt": string ISO8601

Todos los valores numéricos DEBEN ser números, NO strings. El campo "latestPerformance" es OBLIGATORIO.
Usa ISINs, TERs y datos de rendimiento reales y actualizados.
