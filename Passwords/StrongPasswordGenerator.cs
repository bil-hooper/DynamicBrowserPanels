using System;
using System.Collections.Generic;

namespace DynamicBrowserPanels
{
    public static class StrongPasswordGeneratorForm
    {
        private static string alphaCaps = "QWERTYUIOPASDFGHJKLZXCVBNM";
        private static string alphaLow = "qwertyuiopasdfghjklzxcvbnm";
        private static string numerics = "1234567890";
        private static string special = @"@%+\/'!#$^?:.(){}[]~-_";
        private static Random r = new Random();

        public static string GenerateApplePassword(bool includeSpecial = false, string excludeSpecial = "", int sections = 3, int lengthOfSection = 6, string delimiter = "-")
        {
            string str = "";
            List<String> strings = new List<String>(sections);

            string fullString = alphaCaps + alphaLow + numerics + (includeSpecial ? special : string.Empty);

            if (excludeSpecial != string.Empty)
            {
                string str6 = excludeSpecial;
                int num6 = 0;
                while (true)
                {
                    if (num6 >= str6.Length)
                    {
                        fullString = fullString.Replace(" ", "");
                        break;
                    }
                    char oldChar = str6[num6];
                    fullString = fullString.Replace(oldChar, ' ');
                    num6++;
                }
            }
            if (lengthOfSection < 4)
            {
                throw new Exception("Number of characters should be greater than 4.");
            }

            for (Int32 indexer = 0; indexer < sections; indexer++)
            {
                string posArray = "0123456789";
                if (lengthOfSection < posArray.Length)
                {
                    posArray = posArray.Substring(0, lengthOfSection);
                }
                int num = getRandomPosition(ref posArray);
                int num2 = getRandomPosition(ref posArray);
                int num3 = getRandomPosition(ref posArray);
                int num4 = getRandomPosition(ref posArray);
                int num5 = (int)(lengthOfSection * 0.66);

                str = "";
                int num8 = 0;
                while (true)
                {
                    if (num8 >= lengthOfSection)
                    {
                        break;
                    }
                    bool flag13 = num8 == num;
                    str = !flag13 ? ((num8 != num2) ? ((num8 != num3) ? (!((num8 == num4) & includeSpecial) ? (str + getRandomChar(fullString)) : (str + getRandomChar(special))) : (str + getRandomChar(numerics))) : (str + getRandomChar(alphaLow))) : (str + getRandomChar(alphaCaps));
                    num8++;
                }

                strings.Add(str);
            }

            return String.Join(delimiter, strings);

        }

        public static string GenerateStrongPassword(int length, bool includeSpecial = true, string excludeSpecial = "", bool optimize = false)
        {
            if (optimize)
            {
                excludeSpecial = excludeSpecial + "10OIl";
            }
            string str = "";
            string fullString = alphaCaps + alphaLow + numerics + (includeSpecial ? special : string.Empty);
            if (excludeSpecial != string.Empty)
            {
                string str6 = excludeSpecial;
                int num6 = 0;
                while (true)
                {
                    if (num6 >= str6.Length)
                    {
                        fullString = fullString.Replace(" ", "");
                        break;
                    }
                    char oldChar = str6[num6];
                    fullString = fullString.Replace(oldChar, ' ');
                    num6++;
                }
            }
            if (length < 4)
            {
                throw new Exception("Number of characters should be greater than 4.");
            }
            string posArray = "0123456789";
            if (length < posArray.Length)
            {
                posArray = posArray.Substring(0, length);
            }
            int num = getRandomPosition(ref posArray);
            int num2 = getRandomPosition(ref posArray);
            int num3 = getRandomPosition(ref posArray);
            int num4 = getRandomPosition(ref posArray);
            int num5 = (int)(length * 0.66);
            string str4 = " ";
            string str5 = "";
            if (!optimize)
            {
                int num8 = 0;
                while (true)
                {
                    if (num8 >= length)
                    {
                        break;
                    }
                    bool flag13 = num8 == num;
                    str = !flag13 ? ((num8 != num2) ? ((num8 != num3) ? (!((num8 == num4) & includeSpecial) ? (str + getRandomChar(fullString)) : (str + getRandomChar(special))) : (str + getRandomChar(numerics))) : (str + getRandomChar(alphaLow))) : (str + getRandomChar(alphaCaps));
                    num8++;
                }
            }
            else
            {
                int num7 = 0;
                while (true)
                {
                    if (num7 >= length)
                    {
                        break;
                    }
                    if (num7 == 0)
                    {
                        while (true)
                        {
                            str5 = getRandomChar(alphaCaps);
                            if (str5 != str4)
                            {
                                str = str + str5;
                                str4 = str5;
                                break;
                            }
                        }
                    }
                    else if (num7 <= num5)
                    {
                        while (true)
                        {
                            str5 = getRandomChar(alphaLow);
                            if (str5 != str4)
                            {
                                str = str + str5;
                                str4 = str5;
                                break;
                            }
                        }
                    }
                    else if (num7 > num5)
                    {
                        while (true)
                        {
                            str5 = getRandomChar(numerics);
                            if (str5 != str4)
                            {
                                str = str + str5;
                                str4 = str5;
                                break;
                            }
                        }
                    }
                    num7++;
                }
            }
            return str;
        }

        private static string getRandomChar(string fullString) =>
            fullString.ToCharArray()[(int)Math.Floor((double)(r.NextDouble() * fullString.Length))].ToString();

        private static int getRandomPosition(ref string posArray)
        {
            string s = posArray.ToCharArray()[(int)Math.Floor((double)(r.NextDouble() * posArray.Length))].ToString();
            int num = int.Parse(s);
            posArray = posArray.Replace(s, "");
            return num;
        }
    }
}
