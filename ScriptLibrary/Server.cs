using System;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace HerhangiOT.ScriptLibrary
{
    public class Server
    {
        public static HashAlgorithm PasswordHasher { get; protected set; }

        public Dictionary<string, Action<string[]>> CommandLineOperations = new Dictionary<string, Action<string[]>>();
    }
}
