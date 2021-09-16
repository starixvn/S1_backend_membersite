using System.Security.Cryptography;

namespace MemberSite.Service.Library
{
    public class CodeGenerator
    {
        private const int DefaultMaximum = 10;

        private const int DefaultMinimum = 6;

        private const int UBoundDigit = 61;

        private string exclusionSet;

        private bool hasConsecutive;

        private bool hasRepeating;

        private bool hasSymbols;

        private int maxSize;

        private int minSize;

        private readonly char[] pwdCharArray = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789".ToCharArray();

        private readonly RNGCryptoServiceProvider rng;

        public bool ConsecutiveCharacters
        {
            get => hasConsecutive;
            set => hasConsecutive = value;
        }

        public bool ExcludeSymbols
        {
            get => hasSymbols;
            set => hasSymbols = value;
        }

        public string Exclusions
        {
            get => exclusionSet;
            set => exclusionSet = value;
        }

        public int Maximum
        {
            get => maxSize;
            set
            {
                maxSize = value;
                if (minSize >= maxSize)
                {
                    maxSize = 10;
                }
            }
        }

        public int Minimum
        {
            get => minSize;
            set
            {
                minSize = value;
                if (6 > minSize)
                {
                    minSize = 6;
                }
            }
        }

        public bool RepeatCharacters
        {
            get => hasRepeating;
            set => hasRepeating = value;
        }

        public CodeGenerator()
        {
            Minimum = 6;
            Maximum = 10;
            ConsecutiveCharacters = false;
            RepeatCharacters = true;
            ExcludeSymbols = false;
            Exclusions = null;
            rng = new RNGCryptoServiceProvider();
        }
    }

}
