using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Azure.Core;

namespace SteamAchievementTracker.Services
{
    public class ApiKeyService : IApiKeyService
    {

        public SecretClient client { get; set; }

        public string apiKey { get; set; }

        public ApiKeyService()
        {
            SecretClientOptions options = new SecretClientOptions()
            {
                Retry =
                    {
                        Delay= TimeSpan.FromSeconds(2),
                        MaxDelay = TimeSpan.FromSeconds(16),
                        MaxRetries = 5,
                        Mode = RetryMode.Exponential
                    }
            };
            client = new SecretClient(new Uri("https://steamachievementtracker.vault.azure.net/"), new DefaultAzureCredential(), options);
        }

        public ApiKeyService(string ApiKey)
        {
            apiKey = ApiKey;
        }

        public string GetApiKey()
        {
            if(String.IsNullOrEmpty(apiKey))
            {
                KeyVaultSecret secret = client.GetSecret("APIKEY");

                apiKey = secret.Value;
            }

            return apiKey;
            
        }
    }
}
