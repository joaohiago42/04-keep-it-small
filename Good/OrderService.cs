// ============================================================
// EXEMPLO BOM: Método orquestrador com métodos pequenos e focados
// ============================================================
// O mesmo OrderService reescrito.
// Cada "bloco" do método original virou um método próprio.
// O método principal agora é um "índice" que conta a história.

namespace KeepItSmall.Good;

public class OrderService
{
    private readonly ICustomerRepository _customerRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly OrderItemBuilder _orderItemBuilder;
    private readonly ShippingCalculator _shippingCalculator;
    private readonly PaymentProcessor _paymentProcessor;
    private readonly OrderNotifier _orderNotifier;
    private readonly IInventoryService _inventoryService;

    public OrderService(
        ICustomerRepository customerRepository,
        IOrderRepository orderRepository,
        OrderItemBuilder orderItemBuilder,
        ShippingCalculator shippingCalculator,
        PaymentProcessor paymentProcessor,
        OrderNotifier orderNotifier,
        IInventoryService inventoryService)
    {
        _customerRepository = customerRepository;
        _orderRepository = orderRepository;
        _orderItemBuilder = orderItemBuilder;
        _shippingCalculator = shippingCalculator;
        _paymentProcessor = paymentProcessor;
        _orderNotifier = orderNotifier;
        _inventoryService = inventoryService;
    }

    public async Task<OrderResult> CreateOrderAsync(CreateOrderRequest request)
    {
        var customer = await ValidateCustomerAsync(request.CustomerId);
        if (customer == null)
            return OrderResult.Fail("Cliente inválido ou inativo");

        var itemsResult = await _orderItemBuilder.BuildAsync(request.Items, customer);
        if (!itemsResult.Success)
            return OrderResult.Fail(itemsResult.Error);

        var shippingCost = _shippingCalculator.Calculate(itemsResult.TotalAmount, request.ShippingMethod);
        var totalAmount = itemsResult.TotalAmount + shippingCost;

        var paymentResult = await _paymentProcessor.ChargeAsync(customer, totalAmount);
        if (!paymentResult.Success)
            return OrderResult.Fail($"Pagamento falhou: {paymentResult.ErrorMessage}");

        var order = await PersistOrderAsync(customer, itemsResult.Items, totalAmount, shippingCost, paymentResult.TransactionId);
        await _inventoryService.DeductStockForOrderAsync(order);
        await _orderNotifier.NotifyOrderCreatedAsync(customer, order);

        return OrderResult.Ok(order.Id);
    }

    private async Task<Customer?> ValidateCustomerAsync(int customerId)
    {
        var customer = await _customerRepository.GetByIdAsync(customerId);

        if (customer == null || !customer.IsActive || customer.HasPendingDebt)
            return null;

        return customer;
    }

    private async Task<Order> PersistOrderAsync(
        Customer customer,
        List<OrderItem> items,
        decimal totalAmount,
        decimal shippingCost,
        string transactionId)
    {
        var order = new Order
        {
            CustomerId = customer.Id,
            Items = items,
            TotalAmount = totalAmount,
            ShippingCost = shippingCost,
            PaymentTransactionId = transactionId,
            Status = OrderStatus.Confirmed,
            CreatedAt = DateTime.UtcNow
        };

        await _orderRepository.AddAsync(order);
        return order;
    }
}

// Compare com a versão ruim:
//
// RUIM: 1 método de ~100 linhas com 7 responsabilidades
// BOM:  1 método orquestrador de ~20 linhas + métodos focados
//
// O método principal agora lê como uma história:
//   1. Valida o cliente
//   2. Monta os itens
//   3. Calcula o frete
//   4. Processa o pagamento
//   5. Salva o pedido
//   6. Atualiza o estoque
//   7. Notifica
//
// Cada passo pode ser testado, reutilizado e modificado isoladamente.
