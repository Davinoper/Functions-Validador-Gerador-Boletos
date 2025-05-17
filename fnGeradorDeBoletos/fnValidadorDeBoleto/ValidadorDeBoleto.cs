using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace FnValidadorDeBoleto;

public class ValidadorDeBoleto
{
    private const int BarcodeLength = 44;
    private const int DateStartIndex = 3;
    private const int DateLength = 8;
    private static readonly CultureInfo InvariantCulture = CultureInfo.InvariantCulture;

    private readonly ILogger<ValidadorDeBoleto> _logger;

    public ValidadorDeBoleto(ILogger<ValidadorDeBoleto> logger) => _logger = logger;

    [Function("barcode-validate")]
    public async Task<IActionResult> BarcodeValidate(
        [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req)
    {
        var request = await DeserializeRequestAsync(req);

        if (request.Barcode is null or { Length: 0 })
            return BadRequest("Barcode está vazio.");

        if (request.Barcode!.Length != BarcodeLength)
            return Invalid("O barcode deve ter 44 dígitos.");

        if (!TryParseDate(request.Barcode, out var dueDate))
            return Invalid("A data de vencimento é inválida.");

        return Ok(new
        {
            valido = true,
            message = "Boleto validado!",
            dataVencimento = dueDate.ToString("dd-MM-yyyy")
        });
    }

    #region Helpers

    private static async Task<BarcodeRequest> DeserializeRequestAsync(HttpRequest req)
    {
        using var reader = new StreamReader(req.Body);
        var body = await reader.ReadToEndAsync();
        return JsonConvert.DeserializeObject<BarcodeRequest>(body) ?? new BarcodeRequest(string.Empty);
    }

    private static bool TryParseDate(string barcode, out DateTime dueDate)
    {
        var dateSlice = barcode.Substring(DateStartIndex, DateLength);
        return DateTime.TryParseExact(
            dateSlice,
            "yyyyMMdd",
            InvariantCulture,
            DateTimeStyles.None,
            out dueDate);
    }

    private static IActionResult BadRequest(string message)
        => new BadRequestObjectResult(new { valido = false, message });

    private static IActionResult Invalid(string message)
        => new BadRequestObjectResult(new { valido = false, message });

    private static IActionResult Ok(object payload)
        => new OkObjectResult(payload);

    private record BarcodeRequest(string? Barcode);
    #endregion
}
