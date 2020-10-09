using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Azure.Core;

namespace SteamAchievementTracker.Services
{
    public interface IApiKeyService
    {
        SecretClient client { get; set; }

        string apiKey { get; set; }
        string GetApiKey();
    }
}
