using System;
using System.Collections.Generic;

namespace HerhangiOT.ServerLibrary.Database.Model
{
    public class AccountModel
    {
        public uint AccountId { get; set; }
        public string AccountName { get; set; }
        public string Password { get; set; }
        public int AccountType { get; set; }
        public string Email { get; set; }
        public DateTime CreatedOn { get; set; }
        public DateTime? PremiumUntil { get; set; }
        public DateTime? BannedUntil { get; set; }
        public List<AccountCharacterModel> Characters { get; set; }

        // Login server storage variables
        public DateTime ExpiresOn { get; set; }
    }
}
