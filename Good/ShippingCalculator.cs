// ============================================================
// Classe extraída: responsável APENAS por calcular frete
// ============================================================

namespace KeepItSmall.Good;

public class ShippingCalculator
{
    private const decimal FreeShippingThreshold = 200m;
    private const decimal ExpressShippingCost = 25.90m;
    private const decimal StandardShippingCost = 12.50m;

    public decimal Calculate(decimal orderTotal, string shippingMethod)
    {
        if (orderTotal >= FreeShippingThreshold)
            return 0m;

        return shippingMethod == "Express"
            ? ExpressShippingCost
            : StandardShippingCost;
    }
}

// 10 linhas de lógica. Constantes nomeadas (sem números mágicos).
// Se a regra de frete mudar, você mexe AQUI — não num método de 100 linhas.
