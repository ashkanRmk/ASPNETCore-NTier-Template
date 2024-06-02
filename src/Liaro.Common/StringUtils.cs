using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Liaro.Common
{
    public static class StringUtils
    {
        public static string GetUniqueKey(int maxSize, bool numeric = false)
        {
            var chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890".ToCharArray();
            var numbers = "1234567890".ToCharArray();
            var data = new byte[1];
            using (var crypto = new RNGCryptoServiceProvider())
            {
                crypto.GetNonZeroBytes(data);
                data = new byte[maxSize];
                crypto.GetNonZeroBytes(data);
            }
            var result = new StringBuilder(maxSize);
            foreach (var b in data)
            {
                if (numeric)
                    result.Append(numbers[b % (numbers.Length)]);
                else
                    result.Append(chars[b % (chars.Length)]);
            }
            return result.ToString();
        }

        public static string GeneratetrackId(int length)
        {
            StringBuilder myGuidLikeString = new StringBuilder();
            while (myGuidLikeString.Length < length)
            {
                myGuidLikeString.Append(Guid.NewGuid().ToString().Replace("-", ""));
            }
            return myGuidLikeString.ToString(0, length);
        }

        public static string SubStringText(string inputText, int startIndex, int length)
        {
            var strText = inputText;

            if (strText.Length > length)
            {
                return strText.Substring(startIndex, length) + " ... ";
            }
            else
            {
                return strText;
            }
        }

        public static string GetRightMobileNumber(string phone)
        {
            if (phone == null) return null;

            var number = phone.Replace("+", "")
                .Replace(" ", "")
                .Replace("(", "")
                .Replace(")", "")
                .Replace("-", "")
                .Replace("۰", "0")
                .Replace("۱", "1")
                .Replace("۲", "2")
                .Replace("۳", "3")
                .Replace("۴", "4")
                .Replace("۵", "5")
                .Replace("۶", "6")
                .Replace("۷", "7")
                .Replace("۸", "8")
                .Replace("۹", "9")
                .TrimStart('0');

            if (number.Length < 10) return null;

            number = number.Substring(number.Length - 10, 10);
            if (number.Substring(0, 1) != "9") return null;

            try
            {
                Int64.Parse(number);
            }
            catch (Exception)
            {
                return null;
            }
            return "0" + number;
        }

        public static bool IsValidEmail(string s)
        {
            if (string.IsNullOrEmpty(s)) return false;
            return Regex.IsMatch(s, @"^(?("")("".+?(?<!\\)""@)|(([0-9a-z]((\.(?!\.))|[-!#\$%&'\*\+/=\?\^`\{\}\|~\w])*)(?<=[0-9a-z])@))" + @"(?(\[)(\[(\d{1,3}\.){3}\d{1,3}\])|(([0-9a-z][-\w]*[0-9a-z]*\.)+[a-z0-9][\-a-z0-9]{0,22}[a-z0-9]))$",
                RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(250));
        }

        public static bool IsValidPhone(string s)
        {
            if (string.IsNullOrEmpty(s)) return false;
            return (!string.IsNullOrEmpty(GetRightMobileNumber(s)));
        }

        public static bool LinkMustBeAUri(string link)
        {
            if (string.IsNullOrWhiteSpace(link)) return false;

            Uri outUri;
            return Uri.TryCreate(link, UriKind.Absolute, out outUri)
                    && (outUri.Scheme == Uri.UriSchemeHttp || outUri.Scheme == Uri.UriSchemeHttps);
        }


        #region CensorText
        public static IList<string> CensoredWords = new List<string>()
        {
            "bad-words"
        };

        public static string CensorText(this string text)
        {
            if (text == null) return null;
            string censoredText = text;
            foreach (var censoredWord in CensoredWords)
            {
                var regularExpression = ToRegexPattern(censoredWord);
                censoredText = Regex.Replace(censoredText, regularExpression, StarCensoredMatch,
                    RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            }

            return censoredText;
        }

        public static bool CensorMatch(this string text, ref string word)
        {
            if (text == null) return false;
            foreach (var censoredWord in CensoredWords)
            {
                if (text.Contains(censoredWord))
                {
                    word = censoredWord;
                    return true;
                }
            }

            return false;
        }

        private static string StarCensoredMatch(Match m)
        {
            var word = m.Captures[0].Value;
            return new string('*', word.Length);
        }

        private static string ToRegexPattern(string wildcardSearch)
        {
            var regexPattern = Regex.Escape(wildcardSearch);
            regexPattern = regexPattern.Replace(@"\*", ".*?");
            regexPattern = regexPattern.Replace(@"\?", ".");
            if (regexPattern.StartsWith(".*?"))
            {
                regexPattern = regexPattern.Substring(3);

                regexPattern = @"(^\b)*?" + regexPattern;
            }
            regexPattern = @"\b" + regexPattern + @"\b";

            return regexPattern;
        }
        #endregion
    }
}
