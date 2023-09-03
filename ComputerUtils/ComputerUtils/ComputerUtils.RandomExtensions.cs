using System;
using System.Collections.Generic;

namespace ComputerUtils.RandomExtensions
{
    public class RandomExtension
    {
        public static Random random = new Random();
        public static string tokenChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz1234567890_-";

        public bool NextBool()
        {
            return random.NextDouble() <= 0.5;
        }

        public static String Pick(List<String> array)
        {
            return array[random.Next(array.Count)];
        }

        public static String Pick(String[] array)
        {
            return array[random.Next(array.Length)];
        }

        public static T Pick<T>(List<T> array)
        {
            return array[random.Next(array.Count)];
        }
        public static T Pick<T>(T[] array)
        {
            return array[random.Next(array.Length)];
        }

        public static string CreateToken(int length = 100)
        {
            return CreateRandom(length, tokenChars);
        }

        public static string CreateRandom(int length, string characterSet)
        {
            string token = "";
            for (int i = 0; i < length; i++) token += characterSet[random.Next(characterSet.Length)];
            return token;
        }
    }

    public class EightBall
    {
        public static List<String> responses = new List<String>() { "It is certain.", "It is decidedly so.", "Without a doubt.", "Yes – definitely.", "You may rely on it.", "As I see it, yes.", "Most likely.", "Outlook good.", "Yes.", "Signs point to yes.", "Reply hazy, try again.", "Ask again later.", "Better not tell you now.", "Cannot predict now.", "Concentrate and ask again.", "Don’t count on it.", "My reply is no.", "My sources say no.", "Outlook not so good.", "Very doubtful." };

        public static String returnMsg()
        {
            return RandomExtension.Pick(responses);
        }
    }
}