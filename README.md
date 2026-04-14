# 1.4 Keep Methods, Classes & Files Small (Mantenha Pequeno)

> **Fase 1 — Escrevendo Código Limpo** | Roadmap: Software Design & Architecture

## O Conceito

Métodos pequenos fazem **uma coisa só**. Classes pequenas têm **uma responsabilidade só**. Quando algo cresce demais, é sinal de que está acumulando responsabilidades — e precisa ser dividido.

**Analogia:** Pense numa gaveta de cozinha. Se você guarda talheres, temperos, pilhas, canetas e remédios na mesma gaveta, encontrar qualquer coisa vira uma busca. Gavetas com propósito claro — talheres aqui, temperos ali — permitem achar tudo de olhos fechados. **Métodos e classes grandes são gavetas bagunçadas.**

## Referências de Tamanho

| O quê | Ideal | Sinal de alerta | Problema sério |
|-------|-------|------------------|----------------|
| Método | 5-20 linhas | 30+ linhas | 50+ linhas |
| Classe | ~200 linhas | 300+ linhas | 500+ linhas |
| Parâmetros do construtor | 2-3 | 5+ | 8+ (God Class) |

## Sinais de que Está Grande Demais

### Métodos

- Comentários tipo `// --- Bloco 1: Validação ---` separando seções
- Múltiplos níveis de nesting (resolvido no tópico 1.3)
- Precisa rolar a tela para ver o método inteiro
- Difícil dar um nome preciso (porque faz muitas coisas)

### Classes

- Construtor com 5+ dependências injetadas
- Métodos públicos que não se relacionam entre si
- Nome vago: `AdminController`, `UtilService`, `Manager`
- Muitos devs mexem no mesmo arquivo (conflitos de merge)

## Técnica: Extrair Métodos

O método gigante geralmente tem "blocos" separados por comentários. Cada bloco vira um método:

```csharp
// ANTES: método de 100 linhas
public async Task<OrderResult> CreateOrderAsync(CreateOrderRequest request)
{
    // --- Validação do cliente ---
    // ... 15 linhas ...

    // --- Montagem dos itens ---
    // ... 30 linhas ...

    // --- Cálculo do frete ---
    // ... 10 linhas ...

    // --- Pagamento ---
    // ... 15 linhas ...

    // --- Persistência ---
    // ... 15 linhas ...

    // --- Notificação ---
    // ... 10 linhas ...
}

// DEPOIS: método orquestrador de 20 linhas
public async Task<OrderResult> CreateOrderAsync(CreateOrderRequest request)
{
    var customer = await ValidateCustomerAsync(request.CustomerId);
    var itemsResult = await _orderItemBuilder.BuildAsync(request.Items, customer);
    var shippingCost = _shippingCalculator.Calculate(itemsResult.TotalAmount, method);
    var paymentResult = await _paymentProcessor.ChargeAsync(customer, totalAmount);
    var order = await PersistOrderAsync(customer, items, total, shipping, txId);
    await _orderNotifier.NotifyOrderCreatedAsync(customer, order);
    return OrderResult.Ok(order.Id);
}
```

O método principal agora **lê como um índice** — você entende o fluxo todo sem se perder nos detalhes.

## Técnica: Extrair Classes

Se o método usa lógica que pode ser isolada (cálculo de desconto, cálculo de frete), extraia para uma classe própria:

```csharp
// ANTES: lógica de desconto perdida num método de 100 linhas
if (customer.IsPremium && product.Category == "Electronics")
    discount = 0.10m;
else if (customer.IsPremium)
    discount = 0.05m;

// DEPOIS: classe focada, testável isoladamente
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
```

**Benefício:** para testar o cálculo de desconto, você não precisa mais mockar email, pagamento, banco de dados...

## Estrutura do Projeto

```text
04-keep-it-small/
  Bad/
    OrderService.cs        # Método de ~100 linhas com 7 responsabilidades
    DebugController.cs     # God Class: 10+ endpoints, 10 dependências
  Good/
    OrderService.cs        # Método orquestrador de ~20 linhas
    OrderItemBuilder.cs    # Classe extraída: monta itens do pedido
    DiscountCalculator.cs  # Classe extraída: calcula descontos (12 linhas)
    ShippingCalculator.cs  # Classe extraída: calcula frete (10 linhas)
  README.md
```

## Exemplos Reais do Código de Trabalho (Aiko)

### Ruim: Métodos gigantes

| Método | Arquivo | Linhas |
|--------|---------|--------|
| `CreateValidLowDataMessages()` | LowDataMessageApplicationService.cs | ~180 |
| `SyncDataLowDataServerMessage()` | SyncDataApplicationService.cs | ~143 |
| `Translate()` | DebugController.cs | ~189 |

O `SyncDataApplicationService.cs` tem **1180 linhas** — mistura locking, queries, parsing, sync e logging no mesmo arquivo.

### Ruim: God Class

`DebugController.cs` (432 linhas) — 10+ endpoints cobrindo sites, gateways, equipamentos, mensagens e sync. Deveria ser dividido em controllers menores por domínio.

### Bom: Métodos pequenos e focados (CQRS Query Handlers)

```csharp
// GetEnabledSitesQueryHandler.cs — 8 linhas
public async Task<List<SiteDto>> Handle(GetEnabledSitesQuery query, CancellationToken ct)
{
    var sites = await _siteRepository.GetEnabledAsync();
    return sites.Select(s => new SiteDto(s.Id, s.Name)).ToList();
}
```

Cada query handler faz **uma coisa só**: busca dados e mapeia. Simples, testável, focado.

## Regra de Ouro

> **Um método deve fazer uma coisa, fazê-la bem, e fazê-la somente.**
>
> Se você precisa de comentários para separar "blocos" dentro de um método,
> cada bloco deveria ser um método separado.

> **Se o construtor tem mais de 3-4 dependências, a classe provavelmente faz coisas demais.**

## Checklist

- [ ] Métodos têm no máximo 20-30 linhas?
- [ ] Cada método faz uma coisa só?
- [ ] O construtor tem no máximo 3-4 dependências?
- [ ] O nome da classe descreve uma responsabilidade clara?
- [ ] Não existem "blocos" separados por comentários dentro de métodos?
- [ ] Lógica de cálculo/validação está em classes próprias?
- [ ] É possível testar cada parte isoladamente?

## Referência

- Clean Code (Robert C. Martin) — Cap. 3: Functions, Cap. 10: Classes
- [Roadmap.sh — Clean Code](https://roadmap.sh/software-design-architecture)
