using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

//SP
using ClaimsAuthentication;
using Microsoft.SharePoint.Client;
using Sp = Microsoft.SharePoint.Client;

//EWS
using Microsoft.Exchange.WebServices.Data;
using Microsoft.Exchange.WebServices.Autodiscover;

using AzureConference.Models;

namespace AzureConference.Controllers
{
    public class HomeController : Controller
    {
        private readonly ClaimsWrapper wrapper;

        public HomeController()
        {
            wrapper = new ClaimsWrapper("https://shapingcloud1.sharepoint.com/sites/bikeshow", "conference.user@shapingcloud.com", "(t35t1ng)");
        }

        private ListItemCollection GetItemsFromView(ClientContext cxt, String listName, String viewName)
        {
            var list = cxt.Web.Lists.GetByTitle(listName);
            var view = list.Views.GetByTitle(viewName);
            cxt.Load(view, members => members.ViewFields); //ViewFields is more reliable than ViewQuery
            cxt.ExecuteQuery();
 
            var query = new CamlQuery();
            query.ViewXml = String.Concat("<View><ViewFields>", view.ViewFields.SchemaXml, "</ViewFields></View>");
            var items = list.GetItems(query);
            var viewFields = view.ViewFields.ToList();
            cxt.Load(items);
            
            cxt.ExecuteQuery();
            
            //trim out fields that we didn't explicitly request
            foreach (var item in items)
            {
                var fields = item.FieldValues.ToList();
                foreach (var field in fields)
                {
                    if (!viewFields.Contains(field.Key))
                    {
                        item.FieldValues.Remove(field.Key);
                    }
                    else if (field.Value is FieldUrlValue)
                    {
                        var val = field.Value as FieldUrlValue;
                        item.FieldValues.Remove(field.Key);
                        item.FieldValues.Add(field.Key, val.Url);
                    }
                    else if(field.Value is FieldLookupValue)
                    {
                        var val = field.Value as FieldLookupValue;
                        item.FieldValues.Remove(field.Key);
                        item.FieldValues.Add(field.Key, val.LookupValue);
                    }
                }
            }

            return items;
        }

        static bool RedirectionUrlValidationCallback(string redirectionUrl)
        {
            //// The default for the validation callback is to reject the URL.
            //bool result = false;

            //Uri redirectionUri = new Uri(redirectionUrl);

            //// Validate the contents of the redirection URL. In this simple validation
            //// callback, the redirection URL is considered valid if it is using HTTPS
            //// to encrypt the authentication credentials. 
            //if (redirectionUri.Scheme == "https")
            //{
            //    result = true;
            //}

            //return result;
            // Perform validation.
            return (redirectionUrl == "https://autodiscover-s.outlook.com/autodiscover/autodiscover.xml");
        }

        //
        // GET: /Home/

        public ActionResult Index()
        {
            Sp.ClientContext context = new Sp.ClientContext("https://shapingcloud1.sharepoint.com/sites/bikeshow");
            context.ExecutingWebRequest += wrapper.ClientContextExecutingWebRequest;

            SessionsAndSpeakersView view = new SessionsAndSpeakersView();

            var viewItems = GetItemsFromView(context, "Speakers", "Site");
            view.Speakers = new List<Dictionary<String, Object>>();
            foreach (var item in viewItems)
            {
                var speaker = item.FieldValues;
                view.Speakers.Add(speaker);
            }

            viewItems = GetItemsFromView(context, "Sessions", "Site");
            view.Sessions = new List<Dictionary<String, Object>>();
            foreach(var item in viewItems)
            {
                var session = item.FieldValues;
                view.Sessions.Add(session);
            }

            return View(view);
        }

        public ActionResult SignUp()
        {
            return View();
        }

        [HttpPost]
        public ActionResult SignUp(SignUpView view)
        {
            String smtpAddess = "conference.user@shapingcloud.com";
            String password = "(t35t1ng)";

            var exchangeService = new ExchangeService(ExchangeVersion.Exchange2007_SP1);
            exchangeService.Credentials = new WebCredentials(smtpAddess, password);
            exchangeService.AutodiscoverUrl(smtpAddess, RedirectionUrlValidationCallback);

            Contact contact = new Contact(exchangeService);
            contact.GivenName = view.GivenName;
            contact.Surname = view.Surname;
            contact.FileAsMapping = FileAsMapping.SurnameCommaGivenName;

            contact.EmailAddresses[EmailAddressKey.EmailAddress1] = new EmailAddress(view.Email);

            try
            {
                contact.Save();
            }
            catch(Exception e)
            {
                ViewBag.Message = e.Message;
                return View(view);
            }

            return View("Thanks");
        }
    }
}
