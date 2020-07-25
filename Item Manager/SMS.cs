using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Mail;
using System.Net;

namespace Item_Manager
{
    static class SMS
    {
        private static SmtpClient smtp = new SmtpClient("smtp.gmail.com")
        {
            EnableSsl = true,
            Port = 587,
            DeliveryMethod = SmtpDeliveryMethod.Network,
            Credentials = new NetworkCredential("email", "pass")
        };
        private static string phoneNum = "num@vtext.com";

        public static void SendText(string msgBody)
        {
            MailMessage msg = new MailMessage();
            msg.To.Add(phoneNum);
            msg.From = new MailAddress("discordbot42@gmail.com");
            msg.Body = msgBody;

            smtp.Send(msg);
        }
    }
}
