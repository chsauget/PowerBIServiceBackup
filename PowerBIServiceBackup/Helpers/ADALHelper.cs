using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.Rest;
using System;
using System.Threading.Tasks;

namespace PowerBIServiceBackup.Helpers
{
    public static class ADALHelper
    {
        //Retrieve the ADAL Token  https://docs.microsoft.com/fr-fr/power-bi/developer/get-azuread-access-token#access-token-for-non-power-bi-users-app-owns-data
        public static TokenCredentials GetToken(string PowerBILogin, string PowerBIPassword, string AuthenticationContextUrl, string PowerBIRessourceUrl, string ClientId)
        {
            UserCredential credential = new UserPasswordCredential(PowerBILogin, PowerBIPassword);

            // Authenticate using created credentials
            AuthenticationContext authenticationContext = new AuthenticationContext(AuthenticationContextUrl);

            Task<AuthenticationResult> authenticationResultTask = authenticationContext.AcquireTokenAsync(PowerBIRessourceUrl, ClientId, credential);

            AuthenticationResult authenticationResult = authenticationResultTask.Result;
            if (authenticationResult == null)
            {
                throw new Exception("Authentication Failed.");
            }
            else
            {
                return new TokenCredentials(authenticationResult.AccessToken, "Bearer");
            }

        }
    }
}
