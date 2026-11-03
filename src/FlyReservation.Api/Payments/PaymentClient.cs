using FlyReservation.Api.Exceptions;
using Polly.CircuitBreaker;
using Polly.Timeout;

namespace FlyReservation.Api.Payments
{
    // Talks to the external payment provider over HTTP. HttpClient comes from IHttpClientFactory (typed client)
    public class PaymentClient : IPaymentClient
    {
        private readonly HttpClient _httpClient;

        public PaymentClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<string> ChargeAsync(string reference, decimal amount, CancellationToken cancellationToken = default)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "payments")
            {
                Content = JsonContent.Create(new ChargeRequest(reference, amount))
            };
            // The same key on every attempt: when a timed-out attempt was in fact charged, the retry gets that charge back
            request.Headers.Add("Idempotency-Key", reference);

            HttpResponseMessage response;
            try
            {
                response = await _httpClient.SendAsync(request, cancellationToken);
                response.EnsureSuccessStatusCode();
            }
            // Retries are used up, a timeout was hit, or the circuit is open: the provider is not usable right now
            catch (Exception ex) when (ex is HttpRequestException or TimeoutRejectedException or BrokenCircuitException)
            {
                throw new PaymentUnavailableException("The payment service is not available. Please try again later.", ex);
            }

            var charge = await response.Content.ReadFromJsonAsync<ChargeResponse>(cancellationToken);
            return charge!.TransactionId;
        }

        private record ChargeRequest(string Reference, decimal Amount);
        private record ChargeResponse(string TransactionId);
    }
}
