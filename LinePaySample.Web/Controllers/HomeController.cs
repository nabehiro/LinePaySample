using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace LinePaySample.Web.Controllers
{
    public class HomeController : Controller
    {
        private static Dictionary<string, object> OrderTransactions = new Dictionary<string, object>();

        public ActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<ActionResult> Reserve(bool capture = true)
        {
            var param = new
            {
                productName = "Sample Product",
                productImageUrl = "http://livedoor.blogimg.jp/bouzup/imgs/6/a/6a963d7b-s.jpg",
                amount = 100,
                currency = "JPY",
                confirmUrl = "http://localhost:2391/home/confirm",
                orderId = $"ORDER-{DateTime.Now:yyyyMMdd_HHmmss}",
                capture = capture
            };

            // *** call RESERVE API
            var responseJson = await RequestGateway("request", param);
            dynamic responseObj = JObject.Parse(responseJson);
            ViewBag.ResponseJson = JsonConvert.SerializeObject(responseObj, Formatting.Indented);

            if (responseObj.returnCode == "0000")
            {
                OrderTransactions[responseObj.info.transactionId.ToString()] = param;
                ViewBag.PaymentUrl = responseObj.info.paymentUrl.web;
            }

            return View();
        }

        [HttpGet]
        public async Task<ActionResult> Confirm(string transactionId)
        {
            dynamic transaction = OrderTransactions[transactionId];

            var param = new
            {
                amount = transaction.amount,
                currency = transaction.currency
            };

            // *** call CONFIRM API
            var responseJson = await RequestGateway($"{transactionId}/confirm", param);
            dynamic responseObj = JObject.Parse(responseJson);
            ViewBag.ResponseJson = JsonConvert.SerializeObject(responseObj, Formatting.Indented);

            if (responseObj.returnCode == "0000")
            {
                ViewBag.TransactionId = transactionId;
                ViewBag.Captured = transaction.capture;
            }

            return View();
        }

        [HttpPost]
        public async Task<ActionResult> Capture(string transactionId)
        {
            dynamic transaction = OrderTransactions[transactionId];

            var param = new
            {
                amount = transaction.amount,
                currency = transaction.currency
            };

            // *** call CAPTURE API
            var responseJson = await RequestGateway($"authorizations/{transactionId}/capture", param);
            dynamic responseObj = JObject.Parse(responseJson);
            ViewBag.ResponseJson = JsonConvert.SerializeObject(responseObj, Formatting.Indented);

            return View();
        }

        [HttpPost]
        public async Task<ActionResult> Void(string transactionId)
        {
            dynamic transaction = OrderTransactions[transactionId];

            var param = new
            {
                // no parameter
            };

            // *** call VOID API
            var responseJson = await RequestGateway($"authorizations/{transactionId}/void", param);
            dynamic responseObj = JObject.Parse(responseJson);
            ViewBag.ResponseJson = JsonConvert.SerializeObject(responseObj, Formatting.Indented);

            return View();
        }

        [HttpPost]
        public async Task<ActionResult> Refund(string transactionId)
        {
            dynamic transaction = OrderTransactions[transactionId];

            var param = new
            {
                //refundAmount = transaction.amount
            };

            // *** call REFUND API
            var responseJson = await RequestGateway($"{transactionId}/refund", param);
            dynamic responseObj = JObject.Parse(responseJson);
            ViewBag.ResponseJson = JsonConvert.SerializeObject(responseObj, Formatting.Indented);

            return View();
        }

        private async Task<string> RequestGateway(string path, object param)
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("X-LINE-ChannelId", AppSettings.ChannelId);
                client.DefaultRequestHeaders.Add("X-LINE-ChannelSecret", AppSettings.ChannelSecret);

                var content = new StringContent(JsonConvert.SerializeObject(param), Encoding.UTF8, "application/json");

                var response = await client.PostAsync(AppSettings.GatewayRootUrl + path, content);
                return await response.Content.ReadAsStringAsync();
            }
        }
    }
}