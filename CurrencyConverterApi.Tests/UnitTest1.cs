using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.VisualStudio.TestPlatform.TestHost;

namespace CurrencyConverterApi.Tests
{
    public class UnitTest1: IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;

        public UnitTest1(WebApplicationFactory<Program> factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task Test1()
        {
            var client = _factory.CreateClient();

            var response = await client.GetStringAsync("/convert?sourceCurrency=usd&targetCurrency=inr&amount=2");

            Assert.Equal(@"{
  ""exchangeRate"": 74,
  ""convertedAmount"": 148
}", response);
        }
    }
}