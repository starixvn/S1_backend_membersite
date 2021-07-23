using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

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

        protected int GetCryptographicRandomNumber(int lBound, int uBound)
        {
            byte[] array = new byte[4];
            if (lBound == uBound - 1)
            {
                return lBound;
            }
            uint num = (uint)(-1 - (int)(uint.MaxValue % (uint)(uBound - lBound)));
            uint num2;
            do
            {
                rng.GetBytes(array);
                num2 = BitConverter.ToUInt32(array, 0);
            }
            while (num2 >= num);
            return (int)((long)num2 % (long)(uBound - lBound)) + lBound;
        }

        protected char GetRandomCharacter()
        {
            int uBound = pwdCharArray.GetUpperBound(0);
            if (ExcludeSymbols)
            {
                uBound = 61;
            }
            int cryptographicRandomNumber = GetCryptographicRandomNumber(pwdCharArray.GetLowerBound(0), uBound);
            return pwdCharArray[cryptographicRandomNumber];
        }

        public string Generate()
        {
            int cryptographicRandomNumber = GetCryptographicRandomNumber(Minimum, Maximum);
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Capacity = Maximum;
            char c;
            char c2 = c = '\n';
            for (int i = 0; i < cryptographicRandomNumber; i++)
            {
                c = GetRandomCharacter();
                if (!ConsecutiveCharacters)
                {
                    while (c2 == c)
                    {
                        c = GetRandomCharacter();
                    }
                }
                if (!RepeatCharacters)
                {
                    string text = stringBuilder.ToString();
                    int num = text.IndexOf(c);
                    while (-1 != num)
                    {
                        c = GetRandomCharacter();
                        num = text.IndexOf(c);
                    }
                }
                if (Exclusions != null)
                {
                    while (-1 != Exclusions.IndexOf(c))
                    {
                        c = GetRandomCharacter();
                    }
                }
                stringBuilder.Append(c);
                c2 = c;
            }
            if (stringBuilder != null)
            {
                return stringBuilder.ToString();
            }
            return string.Empty;
        }
    }

}
