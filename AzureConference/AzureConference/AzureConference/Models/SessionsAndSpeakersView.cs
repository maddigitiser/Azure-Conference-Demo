using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace AzureConference.Models
{
    public class SessionsAndSpeakersView
    {
        public List<Dictionary<String, Object>> Speakers { get; set; }
        public List<Dictionary<String, Object>> Sessions { get; set; }
    }
}