﻿using NetPing.DAL;
using NetPing.Models;
using NetPing_modern.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Web;
using System.Web.Mvc;
using System.Configuration;
using System.Collections.Specialized;

namespace NetPing_modern.Controllers
{
    public class PreorderController : Controller
    {
        private readonly IRepository _repository;

        public PreorderController(IRepository repository)
        {
            _repository = repository;
        }


        [HttpPost]
        public ActionResult SubmitPreorder(PreorderModel model)
        {
            var device = (Device)TempData["Device"];
            if (device != null)
            {
                SendPreorder(model, device);
                ViewData["Message"] = "Ваш предварительный заказ размещён";
                return Redirect("/products/" + device.Url);
            }
            else
                return Redirect("/development");
        }



        [HttpPost]
        public ActionResult SubmitPreorderAjax(PreorderModel model)
        {
            var device = (Device)TempData["Device"];
            if (device != null)
            {
                SendPreorder(model, device);
                ViewData["Message"] = "Ваш предварительный заказ размещён";
                return View("PreorderForm", new PreorderModel { Device = device });
            }
            else
                return Redirect("/development");
        }

        private void SendPreorder(PreorderModel model, Device device)
        {
            try
            {
                NameValueCollection settings = (NameValueCollection)ConfigurationManager.GetSection("EmailSettings");
                string server, login, password;
                int port = 2525;
                server = settings["Server"];
                login = settings["Login"];
                password = settings["Password"];
                int.TryParse(settings["Port"], out port);

                var mail = new MailMessage();
                var client = new SmtpClient(server, port) //Port 8025, 587 and 25 can also be used.
                {
                    Credentials = new NetworkCredential(login, password),
                    EnableSsl = true
                };

                var timeStamp = DateTime.UtcNow.Subtract(new DateTime(2015, 6, 1)).TotalMilliseconds.ToString("R");

                var cartId = timeStamp.Contains(',') ? timeStamp.Remove(timeStamp.IndexOf(','), 1) : timeStamp;
                cartId = cartId.Contains('.') ? cartId.Remove(timeStamp.IndexOf('.'), 1) : cartId;

                mail.From = new MailAddress("shop_dev@netping.ru");
                mail.To.Add("sales@netping.ru");
                mail.ReplyToList.Add(model.Email);
                mail.Subject = "DEV предварительный заказ № \"" + cartId + "\" на устройство \"" + device.Name.Name + "\"";
                var cont = String.Format(@"
                        <h2>Название устройства: {0}</h2>
                        <h2>Для: {1} {2}</h2>
                        <h2>Дата заказа: {3}</h2>
                        <h2>Комментарий: {4}</h2>
                        </html>", device.Name.Name, model.Name, model.Email, DateTime.Now.ToShortDateString(), model.Comment);
                var htmlView = AlternateView.CreateAlternateViewFromString(cont, null, "text/html");
                mail.AlternateViews.Add(htmlView);
                client.Send(mail);

                mail.To.Clear();
                mail.To.Add(model.Email);
                mail.ReplyToList.Clear();
                mail.ReplyToList.Add("sales@netping.ru");

                cont = String.Format(@"<html><h2>Ваш предварительный заказ размещён!</h2>
                        <h2>Название устройства: {0}</h2>
                        <h2>Для: {1} {2}</h2>
                        <h2>Дата заказа: {3}</h2>
                        <h2>Комментарий: {4}</h2>
                        </html>", device.Name.Name, model.Name, model.Email, DateTime.Now.ToShortDateString(), model.Comment);
                htmlView = AlternateView.CreateAlternateViewFromString(cont, null, "text/html");
                mail.AlternateViews.Clear();
                mail.AlternateViews.Add(htmlView);
                client.Send(mail);

            }
            catch (Exception e) //post failure
            {
                ViewData["Message"] = "При отправке заказа произошла ошибка";
            }
        }

    }
}