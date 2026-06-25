Busca información completa del fondo de inversión con ISIN o nombre: "{{QUERY}}".

IMPORTANTE: Si el ISIN tiene formato válido (2 letras país + 10 caracteres alfanuméricos), SIEMPRE devuelve datos. Los fondos españoles (ES...) están registrados en CNMV. Usa tu conocimiento para dar datos lo más reales posible. NUNCA respondas con error para un ISIN con formato válido.

Devuelve SOLO un objeto JSON (NO un array). Ejemplo de formato esperado:

{
  "isin": "IE00B4L5Y983",
  "name": "iShares Core MSCI World UCITS ETF",
  "category": "Renta Variable Global",
  "managementCompany": "BlackRock",
  "riskLevel": 5,
  "netAssetValue": 89.34,
  "currency": "EUR",
  "expenseRatio": 0.20,
  "rating": 5,
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

Reglas de tipos:
- "isin": string ISIN real del fondo
- "name": string nombre oficial completo
- "category": string, una de: "Renta Variable Global", "Indexado S&P500", "Tecnología", "Emergentes", "Europa", "ESG", "Mixto", "Renta Fija", "Asia-Pacífico", "Materias Primas"
- "managementCompany": string gestora real
- "riskLevel": number entero 1-7
- "netAssetValue": number decimal (NAV actual)
- "currency": string "EUR" o "USD"
- "expenseRatio": number decimal (TER real, ej: 0.20)
- "rating": number entero 0-5 (estrellas Morningstar)
- "latestPerformance": object OBLIGATORIO, todos los campos son number (NO strings)

Usa datos reales y actualizados. Si no reconoces el fondo, responde SOLO con: {"error": "Fondo no encontrado"}
