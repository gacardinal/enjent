using System;
using System.Security.Cryptography;

namespace NarcityMedia.Enjent
{
    /// <summary>
    /// Wraps an instance of <see cref="System.Security.Cryptography.RNGCryptoServiceProvider" />
    /// </summary>
    /// <remark>
    /// There would not be any significant performance concern about instantiating an instance of
    /// <see cref="System.Security.Cryptography.RNGCryptoServiceProvider" /> since it is itself
    /// backed by a thread safe static handle to a native source for the crypto but the purpose
    /// of this singleton is rather to try to encapsulate the disposable logic.
    /// </remark>
    internal class CryptoRandomSingleton
    {
        private static readonly RNGCryptoServiceProvider _instance = new RNGCryptoServiceProvider();

        // Explicit static constructor to tell C# compiler
        // not to mark type as beforefieldinit
        static CryptoRandomSingleton()
        {
        }

        private CryptoRandomSingleton()
        {
            // This is purely for good measure, OnProcessExit is
            // not called on explicit process termination
            AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
        }

        private void OnProcessExit(Object sender, EventArgs e)
        {
            try
            {
                _instance.Dispose();
            }
            finally
            { 
                AppDomain.CurrentDomain.ProcessExit -= OnProcessExit;
            }   
        }

        public static RNGCryptoServiceProvider Instance
        {
            get
            {
                return _instance;
            }
        }
    }
}
