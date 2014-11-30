using System;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace HerhangiOT.ServerLibrary
{
    public class Server
    {
        public static HashAlgorithm PasswordHasher { get; protected set; }

        public Dictionary<string, Action> CommandLineOperations = new Dictionary<string, Action>();
    }
}
