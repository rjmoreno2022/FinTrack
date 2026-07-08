namespace FinTrack.Domain.Enums;

/// <summary>
/// Fuente proveedora de la tasa de cambio.
/// </summary>
public enum RateSource
{
    BCV = 1,         // Banco Central de Venezuela
    Parallel = 2,    // Mercado paralelo
    BinanceP2P = 3,  // Binance P2P
    Manual = 4       // Introducida manualmente por el usuario
}
