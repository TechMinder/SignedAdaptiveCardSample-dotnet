// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Program.cs" company="Microsoft">
//   Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace SignedAdaptiveCardSample
{
    using System;
    using System.Configuration;
    using System.IdentityModel.Tokens.Jwt;
    using System.IO;
    using System.Net;
    using System.Net.Mail;
    using System.Security.Claims;
    using System.Security.Cryptography;
    using Microsoft.Exchange.WebServices.Autodiscover;
    using Microsoft.Exchange.WebServices.Data;
    using Microsoft.IdentityModel.Tokens;
    using Newtonsoft.Json;

    /// <summary>
    /// The sample program generating Actionable Message email body with signed Adaptive Card
    /// </summary>
    class Program
    {
        /// <summary>
        /// Get the private key to sign the card
        /// </summary>
        /// <returns>RSA private key</returns>
        static SecurityKey GetSecurityKeyFromRSAPrivateKeyXml()
        {

            //var rsap = new RSACryptoServiceProvider(2048);
            //var keys = rsap.ToXmlString(true);
            //var pubkey = rsap.ToXmlString(false);
           


            // This is the Outlook Actionable Message developer key, which is only valid in self-sending senario.
            // Production services should generate their own key pairs and register the public key with Actionable Message team.
            string rsaPrivateKeyXml = "<you can copy full xml here or just private key xml>";

            var rsa = new RSACryptoServiceProvider();
            rsa.FromXmlString(rsaPrivateKeyXml);

            return new RsaSecurityKey(rsa);
        }

        /// <summary>
        /// Generate the Actionable Message email body with signed Adaptive Card
        /// </summary>
        /// <param name="args">Command line args</param>
        static void Main(string[] args)
        {
            SecurityKey securityKey = GetSecurityKeyFromRSAPrivateKeyXml();
           // SigningCredentials signingCredentials = new SigningCredentials(securityKey, SecurityAlgorithms.None);

            JwtSecurityTokenHandler handler = new JwtSecurityTokenHandler();
            handler.SetDefaultTimesOnTokenCreation = false;

            string adaptiveCardRawJson = File.ReadAllText("card.json");
            string minifiedCard = JsonConvert.SerializeObject(JsonConvert.DeserializeObject(adaptiveCardRawJson));

            // The Actionable Message provider ID generated during provider registration
            string originator = "<Copy this from the developer registration portal>"; //https://outlook.office.com/connectors/oam/publish

            // Recipients of the email
            string[] recipients = { "test1@test.com", "test@test.com" };

            // Sender of the email
            string sender = "hitecha@cdw.com";

            ClaimsIdentity  subject = new ClaimsIdentity(
                new Claim[]
                {
                    new Claim("sender", sender),
                    new Claim("originator", originator),
                    new Claim("recipientsSerialized", JsonConvert.SerializeObject(recipients)),
                    new Claim("adaptiveCardSerialized", minifiedCard)
                });

            var descriptor = new SecurityTokenDescriptor
            {
                Issuer = "me",
                Audience = "you",
                IssuedAt = DateTime.Now,
                NotBefore = DateTime.Now,
                Expires = DateTime.Now.AddDays(30),
                Subject = subject,
                SigningCredentials = new SigningCredentials(securityKey, SecurityAlgorithms.RsaSha256Signature, SecurityAlgorithms.Sha256Digest)
            };

           

            JwtSecurityToken token = handler.CreateJwtSecurityToken(descriptor);

            string emailBody = File.ReadAllText("signed_adaptive_template.html");

            emailBody = emailBody.Replace("{{signedCardPayload}}", token.RawData);

            Console.WriteLine(emailBody);
            SendEmail(emailBody);
            Console.Read();
        }

        
        public static void SendEmail(string emailBody)
        {
            MailMessage mailMessage= new MailMessage();
            mailMessage.From = new
               MailAddress("noreply@test.com");
            mailMessage.To.Add("test2@test.com");
            mailMessage.Subject = "Test Subject";
            mailMessage.Body = emailBody;//"Testing Office365 Email";
            mailMessage.IsBodyHtml = true;
            SmtpClient client = new SmtpClient("<add smtp url>");           
            //client.Port = 587;
           // client.Host = "smtp.office365.com";
           // client.EnableSsl = true;
            try
            {
                client.Send(mailMessage);

            }
            catch (Exception ex)
            {

                Console.Write(ex.Message);
            }
           
        }
    }
}
