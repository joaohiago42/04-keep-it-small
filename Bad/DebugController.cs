// ============================================================
// EXEMPLO RUIM: God Class — controller com responsabilidades demais
// ============================================================
// Inspirado no DebugController.cs real (432 linhas, 10+ endpoints)
// que mistura site, gateway, equipamento, mensagens e debug.
//
// Problemas:
// 1. Uma classe com responsabilidades de 5 domínios diferentes
// 2. Construtor com 10+ dependências (sinal clássico de God Class)
// 3. Impossível entender o que a classe faz lendo só o nome
// 4. Qualquer mudança em qualquer domínio mexe neste arquivo

namespace KeepItSmall.Bad;

[ApiController]
[Route("api/[controller]")]
public class AdminController : ControllerBase
{
    private readonly ISiteRepository _siteRepository;
    private readonly IGatewayRepository _gatewayRepository;
    private readonly IEquipmentRepository _equipmentRepository;
    private readonly IMessageService _messageService;
    private readonly ISyncService _syncService;
    private readonly IPaymentService _paymentService;
    private readonly IReportService _reportService;
    private readonly IUserService _userService;
    private readonly INotificationService _notificationService;
    private readonly ILogger<AdminController> _logger;

    public AdminController(
        ISiteRepository siteRepository,
        IGatewayRepository gatewayRepository,
        IEquipmentRepository equipmentRepository,
        IMessageService messageService,
        ISyncService syncService,
        IPaymentService paymentService,
        IReportService reportService,
        IUserService userService,
        INotificationService notificationService,
        ILogger<AdminController> logger)
    {
        _siteRepository = siteRepository;
        _gatewayRepository = gatewayRepository;
        _equipmentRepository = equipmentRepository;
        _messageService = messageService;
        _syncService = syncService;
        _paymentService = paymentService;
        _reportService = reportService;
        _userService = userService;
        _notificationService = notificationService;
        _logger = logger;
    }

    // Sites
    [HttpPost("sites")]
    public async Task<IActionResult> CreateSite(CreateSiteRequest request) { /* ... */ }

    [HttpGet("sites/{id}")]
    public async Task<IActionResult> GetSite(int id) { /* ... */ }

    // Gateways
    [HttpPost("gateways")]
    public async Task<IActionResult> SetupGateway(SetupGatewayRequest request) { /* ... */ }

    // Equipamentos
    [HttpGet("equipment/{id}/status")]
    public async Task<IActionResult> GetEquipmentStatus(int id) { /* ... */ }

    // Mensagens
    [HttpPost("messages/send")]
    public async Task<IActionResult> SendMessage(SendMessageRequest request) { /* ... */ }

    [HttpPost("messages/translate")]
    public async Task<IActionResult> TranslateMessage(TranslateRequest request) { /* ... */ }

    // Sync
    [HttpPost("sync/trigger")]
    public async Task<IActionResult> TriggerSync(TriggerSyncRequest request) { /* ... */ }

    // Pagamentos
    [HttpGet("payments/{id}")]
    public async Task<IActionResult> GetPayment(int id) { /* ... */ }

    // Relatórios
    [HttpGet("reports/daily")]
    public async Task<IActionResult> GenerateDailyReport() { /* ... */ }

    // Usuários
    [HttpPost("users/{id}/deactivate")]
    public async Task<IActionResult> DeactivateUser(int id) { /* ... */ }

    // Notificações
    [HttpPost("notifications/broadcast")]
    public async Task<IActionResult> BroadcastNotification(BroadcastRequest request) { /* ... */ }
}

// Sinais de God Class:
// 1. Construtor com 10 dependências — cada uma é uma responsabilidade
// 2. Endpoints de 6 domínios diferentes no mesmo controller
// 3. O nome "AdminController" é vago demais — admin de quê?
// 4. Qualquer dev que mexe em sites, gateways, mensagens ou pagamentos
//    vai criar conflito de merge neste arquivo
//
// Regra prática: se o construtor tem mais de 3-4 dependências,
// a classe provavelmente faz coisas demais.
