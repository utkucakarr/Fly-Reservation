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

            var response = await _httpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var charge = await response.Content.ReadFromJsonAsync<ChargeResponse>(cancellationToken);
            return charge!.TransactionId;
        }

        private record ChargeRequest(string Reference, decimal Amount);
        private record ChargeResponse(string TransactionId);
    }
}
