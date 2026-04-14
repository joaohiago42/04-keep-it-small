// ============================================================
// Classe extraída: responsável APENAS por calcular descontos
// ============================================================
// Antes: if/else de desconto perdido dentro do método gigante.
// Agora: classe pura, fácil de testar com diferentes cenários.

namespace KeepItSmall.Good;

public class DiscountCalculator
{
    public decimal Calculate(Customer customer, Product product, int quantity)
    {
        if (customer.IsPremium && product.Category == "Electronics")
            return 0.10m;

        if (customer.IsPremium)
            return 0.05m;

        if (quantity >= 10)
            return 0.03m;

        return 0m;
    }
}

// 12 linhas. Uma responsabilidade. Zero dependências externas.
// Testável com um simples:
//
//   var calculator = new DiscountCalculator();
//   var discount = calculator.Calculate(premiumCustomer, electronicsProduct, 1);
//   Assert.Equal(0.10m, discount);
//
// Na versão ruim, para testar esse cálculo você precisava
// mockar IOrderRepository, IPaymentGateway, IEmailService...
