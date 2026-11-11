using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace FlyReservation.IntegrationTests
{
    // Stands in for the payment provider: answers with the given status codes in order,
    // then keeps repeating the last one. Records every call it receives
    public class FakePaymentHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode[] _statusCodes;
        private int _calls;

        public FakePaymentHandler(params HttpStatusCode[] statusCodes)
        {
            _statusCodes = statusCodes;
        }

        public int Calls => _calls;
        public ConcurrentQueue<string?> IdempotencyKeys { get; } = new();
        public ConcurrentQueue<decimal> Amounts { get; } = new();

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var call = Interlocked.Increment(ref _calls);
            IdempotencyKeys.Enqueue(request.Headers.TryGetValues("Idempotency-Key", out var keys) ? keys.Single() : null);

            // The amount the API asked the provider to charge.
            // ReadAsStringAsync (not ReadFromJsonAsync): a retry sends the same request again, the body must be readable twice
            var body = await request.Content!.ReadAsStringAsync(cancellationToken);
            using var json = JsonDocument.Parse(body);
            Amounts.Enqueue(json.RootElement.GetProperty("amount").GetDecimal());

            var statusCode = _statusCodes[Math.Min(call, _statusCodes.Length) - 1];
            return new HttpResponseMessage(statusCode)
            {
                Content = JsonContent.Create(new { transactionId = "tx_test" })
            };
        }
    }
}
