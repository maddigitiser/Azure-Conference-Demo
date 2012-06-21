using System;
using System.Net;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using Microsoft.IdentityModel.Protocols.WSTrust;
using Microsoft.SharePoint.Client;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.IO;
using System.Text;
using System.Net.Security;


namespace ClaimsAuthentication
{
    public class ClaimsWrapper
    {
        #region Constants
        private const String m_sOffice365STS = "https://login.microsoftonline.com/extSTS.srf";
        private const String m_sOffice365Login = "https://login.microsoftonline.com/login.srf";
        private const String m_sOffice365Metadata = "https://nexus.microsoftonline-p.com/federationmetadata/2007-06/federationmetadata.xml";
        private const String m_sWsse = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd";
        private const String m_sWsu = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd";
        private const String m_sUserAgent = "Mozilla/5.0 (compatible; MSIE 9.0; Windows NT 6.1; WOW64; Trident/5.0)";
        #endregion

        #region Properties
        private String m_sUsername;
        private String m_sPassword;
        private Uri m_Host; //= new Uri(m_SpUrl); //_vti_bin/ListData.svc /
        private String m_sAuthorityUri = String.Empty;
        private CookieContainer m_CookieContainer = null;
        private DateTime m_Expires = DateTime.MinValue;
        #endregion

        #region Constructor
        public ClaimsWrapper(String spUrl, String username, String password)
        {
            m_Host = new Uri(spUrl);
            m_sUsername = username;
            m_sPassword = password;
        }
        #endregion

        #region Event Handlers
        // Add cookies to CSOM requests
        public void ClientContextExecutingWebRequest(Object sender, WebRequestEventArgs e)
        {
            e.WebRequestExecutor.WebRequest.CookieContainer = GetCookieContainer();
        }

        #endregion

        /// <summary>
        /// Make a call to the Security Token Service to get a token
        /// </summary>
        /// <param name="stsUrl">The address of the STS</param>
        /// <param name="realm">The security realm that teh STS will issue us a token for</param>
        /// <returns>The XML of the returned token</returns>
        public String GetStsResponse(String stsUrl, String realm)
        {
            //bearer token, no crypt
            RequestSecurityToken rst = new RequestSecurityToken();
            rst.RequestType = WSTrustFeb2005Constants.RequestTypes.Issue;
            rst.AppliesTo = new EndpointAddress(realm);
            rst.KeyType = WSTrustFeb2005Constants.KeyTypes.Bearer;
            rst.TokenType = Microsoft.IdentityModel.Tokens.SecurityTokenTypes.Saml11TokenProfile11;

            WSTrustFeb2005RequestSerializer trustSerializer = new WSTrustFeb2005RequestSerializer();

            WSHttpBinding binding = new WSHttpBinding();
            binding.Security.Mode = SecurityMode.TransportWithMessageCredential;
            binding.Security.Message.ClientCredentialType = MessageCredentialType.UserName;
            binding.Security.Message.EstablishSecurityContext = false;
            binding.Security.Message.NegotiateServiceCredential = false;
            binding.Security.Transport.ClientCredentialType = HttpClientCredentialType.None;

            EndpointAddress address = new EndpointAddress(stsUrl);

            using (WSTrustFeb2005ContractClient trustClient = new WSTrustFeb2005ContractClient(binding, address))
            {
                trustClient.ClientCredentials.UserName.UserName = m_sUsername;
                trustClient.ClientCredentials.UserName.Password = m_sPassword;
                Message response = trustClient.EndIssue(
                    trustClient.BeginIssue(
                        Message.CreateMessage(
                            MessageVersion.Default,
                            WSTrustFeb2005Constants.Actions.Issue,
                            new RequestBodyWriter(trustSerializer, rst)
                        ),
                        null,
                        null));
                trustClient.Close();
                using (XmlDictionaryReader reader = response.GetReaderAtBodyContents())
                {
                    return reader.ReadOuterXml();
                }
            }
        }

        /// <summary>
        /// Form a HTTP request that we can use for passive authentication
        /// </summary>
        /// <param name="sUrl">The address of the request</param>
        /// <returns></returns>
        private static HttpWebRequest CreateRequest(String sUrl)
        {
            HttpWebRequest r = HttpWebRequest.Create(sUrl) as HttpWebRequest;
            r.Method = "POST";
            r.ContentType = "application/x-www-form-urlencoded";
            r.CookieContainer = new CookieContainer();
            r.AllowAutoRedirect = false;  //very important!
            r.UserAgent = m_sUserAgent;
            return r;
        }

        /// <summary>
        /// Get a SAML token from the SPO STS and forms a set of cookies that we can use for autheticated requests
        /// </summary>
        /// <returns>A structure of cookies that we can use for federated auth</returns>
        private Cookies GetSamlToken()
        {
            Cookies cookies = new Cookies();

            try
            {
                var sharepointSite = new
                {
                    Wctx = m_sOffice365Login,
                    Wreply = m_Host.GetLeftPart(UriPartial.Authority) + "/_forms/default.aspx?wa=wsignin1.0",
                };

                //get token from the STS and parse it
                String sStsResponse = GetStsResponse(m_sOffice365STS, sharepointSite.Wreply);
                XDocument xDoc = XDocument.Parse(sStsResponse);

                var crypt = xDoc.Descendants().Where(d => d.Name == XName.Get("BinarySecurityToken", m_sWsse)); //get the token itself

                var expires = xDoc.Descendants().Where(d => d.Name == XName.Get("Expires", m_sWsu)); //get the expiration of the token
                cookies.Expires = Convert.ToDateTime(expires.First().Value);

                // Generate a call to the SP site with our SAML (ticket-granting) token and get back 
                // a FedAuth cookie (service-granting) that we can use to call other services in the site
                HttpWebRequest sharepointRequest = CreateRequest(sharepointSite.Wreply);
                Byte[] aData = Encoding.UTF8.GetBytes(crypt.FirstOrDefault().Value);
                using (Stream stream = sharepointRequest.GetRequestStream())
                {
                    stream.Write(aData, 0, aData.Length);
                    stream.Close();

                    using (HttpWebResponse webResponse = sharepointRequest.GetResponse() as HttpWebResponse)
                    {
                        // May be redirected by office365 professional subs
                        if (webResponse.StatusCode == HttpStatusCode.MovedPermanently)
                        {
                            HttpWebRequest redirectedReq = CreateRequest(webResponse.Headers["Location"]);
                            using (Stream redirectedStream = redirectedReq.GetRequestStream())
                            {
                                redirectedStream.Write(aData, 0, aData.Length);
                                redirectedStream.Close();

                                using (HttpWebResponse redirectedResponse = redirectedReq.GetResponse() as HttpWebResponse)
                                {
                                    cookies.FedAuth = redirectedResponse.Cookies["FedAuth"].Value;
                                    cookies.rtFa = redirectedResponse.Cookies["rtFa"].Value;
                                    cookies.Host = redirectedResponse.ResponseUri;
                                }
                            }
                        }
                        else
                        {
                            cookies.FedAuth = webResponse.Cookies["FedAuth"].Value;
                            cookies.rtFa = webResponse.Cookies["rtFa"].Value;
                            cookies.Host = sharepointRequest.RequestUri;
                        }
                    }
                }
            }
            catch (Exception)
            {
                return null;
            }

            return cookies;
        }

        /// <summary>
        /// Get authentication cookies for this request, authenticate if we've not already done so
        /// </summary>
        /// <returns>Authentication cookies that can be sent along with the request</returns>
        CookieContainer GetCookieContainer()
        {
            if (m_CookieContainer == null || DateTime.Now > m_Expires)
            {
                //use passive authentication SAML token from SPO STS (via MSO STS)
                Cookies cookies = GetSamlToken();

                if (!String.IsNullOrEmpty(cookies.FedAuth))
                {
                    //create cookie collection with SAML token
                    m_Expires = cookies.Expires;
                    CookieContainer container = new CookieContainer();

                    //set the fedauth cookie
                    Cookie fedAuth = new Cookie("FedAuth", cookies.FedAuth)
                    {
                        Expires = cookies.Expires,
                        Path = "/",
                        Secure = cookies.Host.Scheme == "https",
                        HttpOnly = true,
                        Domain = cookies.Host.Host
                    };
                    container.Add(fedAuth);

                    Cookie rtFa = new Cookie("rtFa", cookies.rtFa)
                    {
                        Expires = cookies.Expires,
                        Path = "/",
                        Secure = cookies.Host.Scheme == "https",
                        HttpOnly = true,
                        Domain = cookies.Host.Host
                    };
                    container.Add(rtFa);

                    m_CookieContainer = container;
                    return m_CookieContainer;
                }
                return null;
            }
            return m_CookieContainer;
        }
    }

    /// <summary>
    /// STS service contract
    /// </summary>
    [ServiceContract]
    public interface IWSTrustFeb2005Contract
    {
        [OperationContract(ProtectionLevel = ProtectionLevel.EncryptAndSign,
            Action = "http://schemas.xmlsoap.org/ws/2005/02/trust/RST/Issue",
            ReplyAction = "http://schemas.xmlsoap.org/ws/2005/02/trust/RSTR/Issue",
            AsyncPattern = true)]
        IAsyncResult BeginIssue(System.ServiceModel.Channels.Message request, AsyncCallback callback, Object oState);
        System.ServiceModel.Channels.Message EndIssue(IAsyncResult asyncResult);
    }

    /// <summary>
    /// Helper for interacting with the STS
    /// </summary>
    public partial class WSTrustFeb2005ContractClient : ClientBase<IWSTrustFeb2005Contract>, IWSTrustFeb2005Contract
    {
        public WSTrustFeb2005ContractClient(Binding binding, EndpointAddress remoteAddress)
            : base(binding, remoteAddress)
        {
        }

        public IAsyncResult BeginIssue(System.ServiceModel.Channels.Message request, AsyncCallback callback, object oState)
        {
            return base.Channel.BeginIssue(request, callback, oState);
        }

        public System.ServiceModel.Channels.Message EndIssue(IAsyncResult asyncResult)
        {
            return base.Channel.EndIssue(asyncResult);
        }
    }

    /// <summary>
    /// Searializer for out tokens
    /// </summary>
    class RequestBodyWriter : BodyWriter
    {
        WSTrustRequestSerializer m_Serializer;
        RequestSecurityToken m_Rst;

        public RequestBodyWriter(WSTrustRequestSerializer serializer, RequestSecurityToken rst)
            : base(false)
        {
            if (serializer == null)
                throw new ArgumentNullException("serializer");

            m_Serializer = serializer;
            m_Rst = rst;
        }

        protected override void OnWriteBodyContents(System.Xml.XmlDictionaryWriter writer)
        {
            m_Serializer.WriteXml(m_Rst, writer, new WSTrustSerializationContext());
        }
    }

    /// <summary>
    /// Set of auth cookies for a particular host
    /// </summary>
    class Cookies
    {
        public String FedAuth { get; set; } //standard SP STS cookie
        public String rtFa { get; set; } //logout cookie
        public DateTime Expires { get; set; }
        public Uri Host { get; set; }
    }
}