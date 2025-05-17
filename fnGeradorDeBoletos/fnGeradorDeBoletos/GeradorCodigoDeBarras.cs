using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using BarcodeStandard;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SkiaSharp;

namespace FnGeradorDeBoletos;

public sealed class GeradorCodigoDeBarras
{
    private const int BarcodeLength = 44;
    private const string BankCode = "001";
    private const string QueueName = "gerador-codigo-de-barras";
    private static readonly CultureInfo PtBr = new("pt-BR");

    private readonly ILogger<GeradorCodigoDeBarras> _logger;
    private readonly string _serviceBusConnection;

    public GeradorCodigoDeBarras(ILogger<GeradorCodigoDeBarras> logger)
    {
        _logger = logger;
        _serviceBusConnection = Environment.GetEnvironmentVariable("ServiceBusConnection")
                                ?? throw new InvalidOperationException(
                                    "Variável de ambiente ServiceBusConnection não definida");
    }

    [Function("barcode-generate")]
    public async Task<IActionResult> GenerateBarcode(
        [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req)
    {
        try
        {
            var request = await ParseRequestAsync(req);
            if (!request.IsValid(out var validationMsg))
                return BadRequest(validationMsg);

            var barcodeString = BuildBarcodeString(request);
            var barcodeImage = EncodeBarcodeImage(barcodeString);
            var response = new BarcodeResponse(
                                    barcodeString,
                                    request.Valor,
                                    request.DataVencimento,
                                    barcodeImage);

            await SendToQueueAsync(response);
            _logger.LogInformation("Barcode gerado: {Barcode}", barcodeString);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao gerar boleto");
            return BadRequest($"Erro ao gerar boleto. {ex.Message}");
        }
    }

    #region Helpers
    private static async Task<GenerateBarcodeRequest> ParseRequestAsync(HttpRequest req)
    {
        using var reader = new StreamReader(req.Body);
        var body = await reader.ReadToEndAsync();

        return JsonConvert.DeserializeObject<GenerateBarcodeRequest>(body)
               ?? new GenerateBarcodeRequest();
    }

    private static string BuildBarcodeString(GenerateBarcodeRequest req)
    {
        var baseCode = $"{BankCode}{req.DataVencimento:yyyyMMdd}{req.Valor:0}";
        return baseCode.Length < BarcodeLength
            ? baseCode.PadRight(BarcodeLength, '0')
            : baseCode[..BarcodeLength];
    }

    private static string EncodeBarcodeImage(string barcode)
    {
        var encoder = new Barcode().Encode(BarcodeStandard.Type.Code128, barcode);
        using var encoded = encoder.Encode(SKEncodedImageFormat.Png, 100);
        return Convert.ToBase64String(encoded.ToArray());
    }

    private async Task SendToQueueAsync(BarcodeResponse response)
    {
        await using var client = new ServiceBusClient(_serviceBusConnection);
        var sender = client.CreateSender(QueueName);
        var message = new ServiceBusMessage(JsonConvert.SerializeObject(response));

        await sender.SendMessageAsync(message);
        _logger.LogInformation("Mensagem enviada à fila {QueueName}", QueueName);
    }

    private static IActionResult BadRequest(string msg)
        => new BadRequestObjectResult(new { valido = false, message = msg });

    private static IActionResult Ok(object payload) => new OkObjectResult(payload);
    #endregion

    #region DTOs
    private sealed record GenerateBarcodeRequest
    {
        public decimal Valor { get; init; }
        public DateTime DataVencimento { get; init; }

        public bool IsValid(out string? message)
        {
            if (Valor <= 0)
            {
                message = "Valor inválido.";
                return false;
            }

            if (DataVencimento == default)
            {
                message = "Data de vencimento inválida.";
                return false;
            }

            message = null;
            return true;
        }
    }

    private sealed record BarcodeResponse(
        string Barcode,
        decimal ValorOriginal,
        DateTime DataVencimento,
        string ImageBase64);
    #endregion
}
