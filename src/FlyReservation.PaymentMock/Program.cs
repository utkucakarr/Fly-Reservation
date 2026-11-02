using System.Collections.Concurrent;
using System.Net.NetworkInformation;

// A fake payment provider for development. Its behaviour can be changed while it runs:
// POST /chaos { "delayMs": 3000, "failureRate": 0.5 }
var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

var chaos = new ChaosSettings(DelayMs: 3000, FailureRate: 0.5);
var charges = new ConcurrentQueue<Charge>();
var chargesByKey = new ConcurrentDictionary<string, Charge>();

app.MapPost("/chaos", (ChaosSettings settings) =>
{
    chaos = settings;
    app.Logger.LogInformation("Chaos: delay {DelayMs} ms, failure rate {FailureRate}", settings.DelayMs, settings.FailureRate);
    return Results.Ok(chaos);
});

// Every charge ever made: shows double charges at a glance
app.MapGet("/charges", () => charges);

app.MapPost("/payments", async (PaymentRequest request, HttpRequest http) =>
{
    var settings = chaos;

    if (Random.Shared.NextDouble() < settings.FailureRate)
    {
        app.Logger.LogWarning("Payment {Reference}: simulated failure, 503", request.Reference);
        return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
    }

    var idempotencyKey = http.Headers["Idempotency-Key"].ToString();
    if (idempotencyKey.Length > 0 && chargesByKey.TryGetValue(idempotencyKey, out var previous))
    {
        app.Logger.LogInformation("Payment {Reference}: key seen before, returning {TransactionId} again", request.Reference, previous.TransactionId);
        await Task.Delay(settings.DelayMs);
        return Results.Ok(previous);
    }

    var charge = new Charge($"tx_{Guid.NewGuid():N}"[..11], request.Reference, request.Amount, DateTime.UtcNow);
    charges.Enqueue(charge);
    if (idempotencyKey.Length > 0)
        chargesByKey[idempotencyKey] = charge;
    app.Logger.LogInformation("Payment {Reference}: charged {Amount}, {TransactionId}", request.Reference, request.Amount, charge.TransactionId);

    // The money is already taken. A slow answer here means "charged, but the response is late"
    await Task.Delay(settings.DelayMs);
    return Results.Ok(charge);
});

app.Run();

record ChaosSettings(int DelayMs, double FailureRate);
record PaymentRequest(string Reference, decimal Amount);
record Charge(string TransactionId, string Reference, decimal Amount, DateTime CreatedAt);
