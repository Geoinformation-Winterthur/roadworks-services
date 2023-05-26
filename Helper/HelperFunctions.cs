// <copyright company="Vermessungsamt Winterthur">
//      Author: Edgar Butwilowski
//      Copyright (c) Vermessungsamt Winterthur. All rights reserved.
// </copyright>
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using roadwork_portal_service.Configuration;
using roadwork_portal_service.Model;

namespace roadwork_portal_service.Helper
{
    public class HelperFunctions
    {
        public static string hashPassphrase(string passphrase)
        {
            string hashedPassphrase = Convert.ToBase64String(KeyDerivation.Pbkdf2(
                password: passphrase,
                salt: AppConfig.salt,
                prf: KeyDerivationPrf.HMACSHA256,
                iterationCount: 100000,
                numBytesRequested: 256 / 8
            ));
            return hashedPassphrase;
        }

        public static ConfigurationData getConfigDataFromDb(){

            return null;            
        }

    }
}
