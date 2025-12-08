using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Classes
{
    public class Client
    {
        public string Token { get; set; }
        public DateTime DateConnect { get; set; }
        public string Username { get; set; }

        public Client()
        {
            this.Token = GenerateToken();
            DateConnect = DateTime.Now;
        }

        public static string GenerateToken()
        {
            Random rnd = new Random();
            string Chars = "qwertyuiopasdfghjklzxcvbnmQWERTYUIOPASDFGHJKLZXCVBNM1234567890";
            string Token = new string(Enumerable.Repeat(Chars, 15).Select(x => x[rnd.Next(Chars.Length)]).ToArray());
            return Token;
        }
    }
}
