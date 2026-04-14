// ============================================================
// Classe extraída: responsável APENAS por montar itens do pedido
// ============================================================
// Antes: essa lógica estava no meio do método gigante.
// Agora: classe própria, testável isoladamente.

namespace KeepItSmall.Good;

public class OrderItemBuilder
{
    private readonly IProductRepository _productRepository;
    private readonly IInventoryService _inventoryService;
    private readonly DiscountCalculator _discountCalculator;

    public OrderItemBuilder(
        IProductRepository productRepository,
        IInventoryService inventoryService,
        DiscountCalculator discountCalculator)
    {
        _productRepository = productRepository;
        _inventoryService = inventoryService;
        _discountCalculator = discountCalculator;
    }

    public async Task<OrderItemsResult> BuildAsync(
        List<OrderItemRequest> itemRequests,
        Customer customer)
    {
        var items = new List<OrderItem>();
        var totalAmount = 0m;

        foreach (var request in itemRequests)
        {
            var result = await BuildSingleItemAsync(request, customer);
            if (!result.Success)
                return OrderItemsResult.Fail(result.Error);

            items.Add(result.Item);
            totalAmount += result.Item.Total;
        }

        return OrderItemsResult.Ok(items, totalAmount);
    }

    private async Task<SingleItemResult> BuildSingleItemAsync(
        OrderItemRequest request,
        Customer customer)
    {
        var product = await _productRepository.GetByIdAsync(request.ProductId);
        if (product == null)
            return SingleItemResult.Fail($"Produto {request.ProductId} não encontrado");

        if (!product.IsAvailable)
            return SingleItemResult.Fail($"Produto {product.Name} indisponível");

        var stock = await _inventoryService.GetStockAsync(product.Id);
        if (stock < request.Quantity)
            return SingleItemResult.Fail($"Estoque insuficiente para {product.Name}");

        var discount = _discountCalculator.Calculate(customer, product, request.Quantity);
        var unitPrice = product.Price * (1 - discount);

        var item = new OrderItem
        {
            ProductId = product.Id,
            ProductName = product.Name,
            Quantity = request.Quantity,
            UnitPrice = unitPrice,
            Discount = discount,
            Total = unitPrice * request.Quantity
        };

        return SingleItemResult.Ok(item);
    }
}

// Cada método faz UMA coisa:
// - BuildAsync: orquestra a construção de todos os itens
// - BuildSingleItemAsync: valida e monta UM item
//
// O DiscountCalculator foi extraído para outra classe (abaixo).
