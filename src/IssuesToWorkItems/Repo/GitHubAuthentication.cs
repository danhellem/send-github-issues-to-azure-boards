using Microsoft.Extensions.Options;
using SyncGitHubIssuesToWorkItems.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace SyncGitHubIssuesToWorkItems.Repo
{
    public class GitHubAuthentication : IGitHubAuthentication
    {
        private const string _sha1Prefix = "sha1=";
        private string _secret = "";

        private IOptions<AppSettings> _appSettings;

        public GitHubAuthentication(IOptions<AppSettings> appSettings)
        {
            _appSettings = appSettings;
            _secret = _appSettings.Value.GitHub_Secret;
        }

        /// <summary>
        /// check payload and signature for valid secret
        /// </summary>
        /// <param name="payload"></param>
        /// <param name="signatureWithPrefix"></param>
        /// <returns>true or false</returns>
        public bool IsValidGitHubWebHookRequest(string payload, string signatureWithPrefix)
        {
            if (signatureWithPrefix.StartsWith(_sha1Prefix, StringComparison.OrdinalIgnoreCase))
            {
                var signature = signatureWithPrefix.Substring(_sha1Prefix.Length);
                var secret = Encoding.ASCII.GetBytes(_secret); //get secret from app settings
                var payloadBytes = Encoding.ASCII.GetBytes(payload);

                using (var hmSha1 = new HMACSHA1(secret))
                {
                    var hash = hmSha1.ComputeHash(payloadBytes);
                    var hashString = ToHexString(hash);

                    if (hashString.Equals(signature))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public string ToHexString(byte[] bytes)
        {
            var builder = new StringBuilder(bytes.Length * 2);
            foreach (byte b in bytes)
            {
                builder.AppendFormat("{0:x2}", b);
            }

            return builder.ToString();
        }
    }

    public interface IGitHubAuthentication
    {
        bool IsValidGitHubWebHookRequest(string payload, string signatureWithPrefix);
        string ToHexString(byte[] bytes);
    }
}
