// ============================================================
// DESAFIO 1.4 — Keep Methods / Classes / Files Small
// ============================================================
// Este método faz TUDO: valida, busca, calcula, salva e notifica.
// Sua missão: quebre em métodos e/ou classes menores.
// O método principal deve virar um "índice" de ~15 linhas.
//
// Dicas:
// - Cada "bloco" comentado deve virar um método ou classe
// - Lógica de cálculo pode ir para classes próprias
// - O método principal deve ler como uma história
// - Não precisa implementar as interfaces, só a estrutura
//
// Crie o(s) arquivo(s) ChallengeSolved.cs com sua solução.

namespace KeepItSmall.Challenge;

public class SubscriptionService
{
    private readonly IUserRepository _userRepo;
    private readonly IPlanRepository _planRepo;
    private readonly IPaymentGateway _payment;
    private readonly IEmailService _email;
    private readonly ILogger _logger;

    public SubscriptionService(
        IUserRepository userRepo,
        IPlanRepository planRepo,
        IPaymentGateway payment,
        IEmailService email,
        ILogger logger)
    {
        _userRepo = userRepo;
        _planRepo = planRepo;
        _payment = payment;
        _email = email;
        _logger = logger;
    }

    public async Task<SubscriptionResult> SubscribeAsync(int userId, string planCode, string coupon)
    {
        // --- Validar usuário ---
        var user = await _userRepo.GetByIdAsync(userId);
        if (user == null)
            return SubscriptionResult.Fail("Usuário não encontrado");
        if (!user.IsActive)
            return SubscriptionResult.Fail("Usuário inativo");
        if (user.HasActiveSubscription)
            return SubscriptionResult.Fail("Já possui assinatura ativa");

        // --- Buscar e validar plano ---
        var plan = await _planRepo.GetByCodeAsync(planCode);
        if (plan == null)
            return SubscriptionResult.Fail("Plano não encontrado");
        if (!plan.IsAvailable)
            return SubscriptionResult.Fail("Plano indisponível");

        // --- Calcular preço ---
        var price = plan.MonthlyPrice;

        if (plan.HasAnnualDiscount && plan.BillingCycle == "Annual")
            price = plan.MonthlyPrice * 12 * 0.80m;
        else if (plan.BillingCycle == "Annual")
            price = plan.MonthlyPrice * 12;

        decimal couponDiscount = 0;
        if (!string.IsNullOrEmpty(coupon))
        {
            if (coupon == "FIRST50")
                couponDiscount = price * 0.50m;
            else if (coupon == "FRIEND20")
                couponDiscount = price * 0.20m;
            else if (coupon.StartsWith("CUSTOM"))
            {
                var pct = int.Parse(coupon.Replace("CUSTOM", "")) / 100m;
                couponDiscount = price * pct;
            }
        }

        var finalPrice = price - couponDiscount;
        if (finalPrice < 0) finalPrice = 0;

        // --- Processar pagamento ---
        _logger.LogInformation("Processando pagamento de {Price} para usuário {UserId}",
            finalPrice, userId);

        var paymentResult = await _payment.ChargeAsync(user.PaymentMethodId, finalPrice);
        if (!paymentResult.Success)
        {
            _logger.LogWarning("Pagamento falhou: {Error}", paymentResult.Error);
            return SubscriptionResult.Fail($"Pagamento falhou: {paymentResult.Error}");
        }

        // --- Criar assinatura ---
        var subscription = new Subscription
        {
            UserId = user.Id,
            PlanCode = plan.Code,
            PlanName = plan.Name,
            Price = finalPrice,
            CouponUsed = coupon,
            StartDate = DateTime.UtcNow,
            EndDate = plan.BillingCycle == "Annual"
                ? DateTime.UtcNow.AddYears(1)
                : DateTime.UtcNow.AddMonths(1),
            TransactionId = paymentResult.TransactionId,
            Status = "Active"
        };

        await _userRepo.SaveSubscriptionAsync(subscription);
        user.HasActiveSubscription = true;
        await _userRepo.UpdateAsync(user);

        // --- Enviar notificações ---
        await _email.SendWelcomeAsync(user.Email, plan.Name);
        _logger.LogInformation("Assinatura criada: {SubId}", subscription.Id);

        if (plan.Code == "Enterprise")
            await _email.SendOnboardingAsync(user.Email);

        return SubscriptionResult.Ok(subscription.Id);
    }
}
