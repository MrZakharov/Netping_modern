using NetPing_modern.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using System.Net.Mail;
using System.Net.Mime;
using System.Resources;
using Base32;

namespace NetPing_modern.Controllers
{
    public class CartController : Controller
    {
        //
        // GET: /Cart/
        public ActionResult Index()
        {
            return View();
        }

        public Boolean? SendCartMail(Cart cart)
        {
            try
            {
                var mail = new MailMessage();
                var client = new SmtpClient("smtpcorp.com", 2525) //Port 8025, 587 and 25 can also be used.
                {
                    Credentials = new NetworkCredential("sp@netping.ru", "JKWEop349f"),
                    EnableSsl = true
                };

                var timeStamp = DateTime.UtcNow.Subtract(new DateTime(2015, 6, 1)).TotalMilliseconds.ToString("R");

                var cartId = timeStamp.Contains(',') ? timeStamp.Remove(timeStamp.IndexOf(','), 1) : timeStamp;
                cartId = cartId.Contains('.') ? cartId.Remove(timeStamp.IndexOf('.'), 1) : cartId;

                mail.From = new MailAddress("shop@netping.ru");
                mail.To.Add("sales@netping.ru");
                mail.ReplyToList.Add(cart.EMail);
                mail.Subject = "Заказ № \"" + cartId + "\"";
                string items = "";
                var sum = 0;
                foreach (var cartitem in cart.Data)
                {
                    var itemrow = "";
                    var itemprice = Int32.Parse(((string)cartitem["price"]));
                    var itemcount = Int32.Parse(((string)cartitem["count"]));
                    itemrow = String.Format(@"<tr><td><img src='{0}' width=200/></td>
                        <td>{1}</td>
                        <td>{2} руб.</td>
                        <td>{3}</td>
                        <td>{4} руб.</td></tr>",
                            cartitem["photoURL"], cartitem["name"], cartitem["price"], cartitem["count"], itemprice * itemcount);
                    items += itemrow;
                    sum += itemprice * itemcount;
                }
                var cont = String.Format(@"<html><h2>{0}</h2>
                        <h2>Адрес доставки: {1}</h2>
                        <h2>Способ доставки: {2}</h2>
                        <h2>Телефон: {3}</h2>
                        <h2>Реквизиты: {4}</h2>
                        <h2>Сумма заказа: {5} руб.</h2>
                        <h2>Состав заказа</h2>
                        <table border=1 style='border: 1px solid #000; width: 100%; border-collapse: collapse'>
                        <tr><th></th><th>Наименование</th><th>Цена</th><th>Количество</th><th>Стоимость</th></tr>
                        {6}</table>
                        </html>", cart.Name, cart.Address, "Бесплатная доставка", cart.Phone, cart.Requisites, sum, items);
                var htmlView = AlternateView.CreateAlternateViewFromString(cont, null, "text/html");
                mail.AlternateViews.Add(htmlView);
                client.Send(mail);



                mail.To.Clear();
                mail.To.Add(cart.EMail);
                mail.ReplyToList.Clear();
                mail.ReplyToList.Add("sales@netping.ru");

                cont = String.Format(@"<html><h2>Ваш заказ был получен, ждите ответа менеджера нашей компании</h2>
                        <h2>{0}</h2>
                        <h2>Адрес доставки: {1}</h2>
                        <h2>Способ доставки: {2}</h2>
                        <h2>Телефон: {3}</h2>
                        <h2>Реквизиты: {4}</h2>
                        <h2>Сумма заказа: {5} руб.</h2>
                        <h2>Состав заказа</h2>
                        <table border=1 style='border: 1px solid #000; width: 100%; border-collapse: collapse'>
                        <tr><th></th><th>Наименование</th><th>Цена</th><th>Количество</th><th>Стоимость</th></tr>
                        {6}</table>
                        </html>", cart.Name, cart.Address, cart.Shipping, cart.Phone, cart.Requisites, sum, items);
                htmlView = AlternateView.CreateAlternateViewFromString(cont, null, "text/html");
                mail.AlternateViews.Clear();
                mail.AlternateViews.Add(htmlView);
                client.Send(mail);


                /*

                                using (var wb = new WebClient())
                                {
                                    var data = new NameValueCollection();
                                    data["authuser"] = "sp@netping.ru";
                                    data["authpass"] = "NqIx8Fov";
                                    data["from"] = cart.EMail;
                                    data["subject"] = "Заказ";
                                    data["to"] = "sales@netping.ru";
                                    data["content"] = cart.Name + cart.Shipping + cart.Requisites + cart.Address;
                                    string items = "";
                                    var sum = 0;
                                    foreach (var cartitem in cart.Data)
                                    {
                                        var itemrow = "";
                                        var itemprice = Int32.Parse(((string)cartitem["price"]));
                                        var itemcount = Int32.Parse(((string)cartitem["count"]));
                                        itemrow = String.Format(@"<tr><td><img src='{0}' width=200/></td>
                                        <td>{1}</td>
                                        <td>{2} руб.</td>
                                        <td>{3}</td>
                                        <td>{4} руб.</td></tr>",
                                                cartitem["photoURL"], cartitem["name"], cartitem["price"], cartitem["count"], itemprice * itemcount);
                                        items += itemrow;
                                        sum += itemprice * itemcount;
                                    }

                                    data["html_content"] = String.Format(@"<html><h2>{0}</h2>
                                        <h2>Адрес доставки: {1}</h2>
                                        <h2>Способ доставки: {2}</h2>
                                        <h2>Телефон: {3}</h2>
                                        <h2>Реквизиты: {4}</h2>
                                        <h2>Сумма заказа: {5} руб.</h2>
                                        <h2>Состав заказа</h2>
                                        <table border=1 style='border: 1px solid #000; width: 100%; border-collapse: collapse'>
                                        <tr><th></th><th>Наименование</th><th>Цена</th><th>Количество</th><th>Стоимость</th></tr>
                                        {6}</table>
                                        </html>", cart.Name, cart.Address, cart.Shipping, cart.Phone, cart.Requisites, sum, items);
                                    var response = wb.UploadValues("https://api.turbo-smtp.com/api/mail/send", "POST", data);
                 */
                return null;
                //              }
            }
            catch (Exception e) //post failure
            {
                return false;
            }
        }
    }
}