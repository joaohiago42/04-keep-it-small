// ============================================================
// EXEMPLO RUIM: Método gigante que faz tudo
// ============================================================
// Inspirado no CreateValidLowDataMessages() real (~180 linhas)
// e no SyncDataApplicationService (~1180 linhas).
//
// Problemas:
// 1. Um método com 80+ linhas fazendo validação, cálculo,
//    persistência, notificação e logging
// 2. Impossível testar uma parte sem executar tudo
// 3. Impossível reutilizar pedaços da lógica
// 4. Difícil de entender o que o método faz "por cima"

namespace KeepItSmall.Bad;

public class OrderService
{
    private readonly IOrderRepository _orderRepository;
    private readonly IProductRepository _productRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IPaymentGateway _paymentGateway;
    private readonly IEmailService _emailService;
    private readonly IInventoryService _inventoryService;
    private readonly ILogger<OrderService> _logger;

    public OrderService(
        IOrderRepository orderRepository,
        IProductRepository productRepository,
        ICustomerRepository customerRepository,
        IPaymentGateway paymentGateway,
        IEmailService emailService,
        IInventoryService inventoryService,
        ILogger<OrderService> logger)
    {
        _orderRepository = orderRepository;
        _productRepository = productRepository;
        _customerRepository = customerRepository;
        _paymentGateway = paymentGateway;
        _emailService = emailService;
        _inventoryService = inventoryService;
        _logger = logger;
    }

    // Este método faz TUDO: valida, calcula, cobra, salva, notifica
    public async Task<OrderResult> CreateOrderAsync(CreateOrderRequest request)
    {
        // --- Bloco 1: Validação do cliente ---
        _logger.LogInformation("Criando pedido para cliente {CustomerId}", request.CustomerId);
        var customer = await _customerRepository.GetByIdAsync(request.CustomerId);
        if (customer == null)
            return OrderResult.Fail("Cliente não encontrado");

        if (!customer.IsActive)
            return OrderResult.Fail("Cliente inativo");

        if (customer.HasPendingDebt)
            return OrderResult.Fail("Cliente com débito pendente");

        // --- Bloco 2: Validação e cálculo dos itens ---
        var orderItems = new List<OrderItem>();
        decimal totalAmount = 0;

        foreach (var itemRequest in request.Items)
        {
            var product = await _productRepository.GetByIdAsync(itemRequest.ProductId);
            if (product == null)
                return OrderResult.Fail($"Produto {itemRequest.ProductId} não encontrado");

            if (!product.IsAvailable)
                return OrderResult.Fail($"Produto {product.Name} indisponível");

            var stock = await _inventoryService.GetStockAsync(product.Id);
            if (stock < itemRequest.Quantity)
                return OrderResult.Fail($"Estoque insuficiente para {product.Name}");

            var discount = 0m;
            if (customer.IsPremium && product.Category == "Electronics")
                discount = 0.10m;
            else if (customer.IsPremium)
                discount = 0.05m;
            else if (itemRequest.Quantity >= 10)
                discount = 0.03m;

            var unitPrice = product.Price * (1 - discount);
            var itemTotal = unitPrice * itemRequest.Quantity;
            totalAmount += itemTotal;

            orderItems.Add(new OrderItem
            {
                ProductId = product.Id,
                ProductName = product.Name,
                Quantity = itemRequest.Quantity,
                UnitPrice = unitPrice,
                Discount = discount,
                Total = itemTotal
            });
        }

        // --- Bloco 3: Aplicar frete ---
        decimal shippingCost;
        if (totalAmount >= 200)
            shippingCost = 0;
        else if (request.ShippingMethod == "Express")
            shippingCost = 25.90m;
        else
            shippingCost = 12.50m;

        totalAmount += shippingCost;

        // --- Bloco 4: Processar pagamento ---
        var paymentResult = await _paymentGateway.ChargeAsync(
            customer.PaymentMethodId, totalAmount);

        if (!paymentResult.Success)
        {
            _logger.LogWarning("Pagamento falhou para cliente {CustomerId}: {Error}",
                customer.Id, paymentResult.ErrorMessage);
            return OrderResult.Fail($"Pagamento falhou: {paymentResult.ErrorMessage}");
        }

        // --- Bloco 5: Criar e salvar o pedido ---
        var order = new Order
        {
            CustomerId = customer.Id,
            Items = orderItems,
            TotalAmount = totalAmount,
            ShippingCost = shippingCost,
            PaymentTransactionId = paymentResult.TransactionId,
            Status = OrderStatus.Confirmed,
            CreatedAt = DateTime.UtcNow
        };

        await _orderRepository.AddAsync(order);

        // --- Bloco 6: Atualizar estoque ---
        foreach (var item in orderItems)
        {
            await _inventoryService.DeductStockAsync(item.ProductId, item.Quantity);
        }

        // --- Bloco 7: Enviar notificações ---
        await _emailService.SendOrderConfirmationAsync(customer.Email, order);
        _logger.LogInformation("Pedido {OrderId} criado com sucesso", order.Id);

        if (customer.IsPremium)
        {
            await _emailService.SendPremiumBonusNotificationAsync(customer.Email, order);
        }

        return OrderResult.Ok(order.Id);
    }
}

// Problemas:
// - 1 método com 7 responsabilidades distintas
// - ~100 linhas de lógica misturando validação, cálculo, I/O e notificação
// - Impossível testar o cálculo de desconto sem criar mocks de email, pagamento, etc.
// - Se a regra de frete mudar, você mexe no mesmo método que calcula desconto
// - Cada "bloco" marcado com comentário é um sinal de que deveria ser um método
//
// Regra prática: se você precisa de comentários tipo "--- Bloco X ---"
// para separar partes do método, cada bloco deveria ser um método separado.
