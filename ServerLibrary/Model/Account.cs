using System;
using System.Collections.Generic;

namespace HerhangiOT.ServerLibrary.Model
{
    public class Account
    {
        public string Password { get; set; }
        public DateTime? PremiumUntil { get; set; }
        public DateTime? BannedUntil { get; set; }
        public List<AccountCharacter> Characters { get; set; }

        // Login server storage variables
        public DateTime ExpiresOn { get; set; }
        public string OnlineCharacter { get; set; }
    }
}