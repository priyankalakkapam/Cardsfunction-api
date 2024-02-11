using Microsoft.Azure.KeyVault;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Status_HyperpayFunction.Utils
{
    public static class KeyVaultService
    {
        private static string ClientId { get; set; }
        private static string ClientSecret { get; set; }
        private static KeyVaultClient _KeyVaultClient = null;
        public static KeyVaultClient KeyVaultClient
        {
            get
            {
                if (_KeyVaultClient == null)
                {
                    _KeyVaultClient = new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(new KeyVaultClient.AuthenticationCallback(GetToken)));
                }
                return _KeyVaultClient;
            }
        }
        public static string GetSecret(string clientId, string clientSecret, string keySecretUri)
        {
            ClientId = clientId;
            ClientSecret = clientSecret;
            var secretbundle = KeyVaultClient.GetSecretAsync(keySecretUri).Result;
            return secretbundle.Value;
        }
        private static async Task<string> GetToken(string authority, string resource, string scope)
        {
            var authContext = new Microsoft.IdentityModel.Clients.ActiveDirectory.AuthenticationContext(authority);
            ClientCredential clientCred = new ClientCredential(clientId: ClientId, clientSecret: ClientSecret);
            AuthenticationResult result = await authContext.AcquireTokenAsync(resource, clientCred);
            if (result == null)
                throw new InvalidOperationException("Failed to obtain the JWT token");
            return result.AccessToken;
        }
    }
}
