using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

using Newtonsoft.Json;

using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;

namespace Item_Manager
{
    static class Oauth
    {
        private static IWebDriver driver = Driver;
        private static WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromMinutes(1));

        private static HttpClient client;

        private const string baseUrl = "https://www.bungie.net";
        private const string loginUrl = "https://us.battle.net/oauth/authorize?response_type=code&client_id={0}&redirect_uri={1}";

        private static Dictionary<string, string> bungieEndpoints = new Dictionary<string, string>
        {
            { "auth", "/en/oauth/authorize?client_id={0}&response_type=code" },
            { "token", "/platform/app/oauth/token/" }
        };
        private static Dictionary<string, string> bungieCreds = new Dictionary<string, string>
        {
            { "clientId", "" },
            { "clientSecret", "" },
            { "redirectUri", "https://www.bungie.net" },
            { "apiKey", "" }
        };
        private static Dictionary<string, string> bnetLogin = new Dictionary<string, string>
        {
            { "email", "" },
            { "passw", "" }
        };
        private static Dictionary<string, string> bnetCreds = new Dictionary<string, string>
        {
            { "clientId", "" },
            { "redirectUri", "https://www.bungie.net/en/User/SignIn/BattleNetId?flowStart=1" }
        };

        private static Timer refreshTimer;

        private static string authCode;
        private static string accessToken;
        private static string refreshToken;

        private static string membershipId;
        
        public static IWebDriver Driver
        {
            get
            {
                var options = new ChromeOptions();
                options.AddArguments(new string[] { "headless", "unsafe-inline", "log-level=3" });
                driver = new ChromeDriver(options);
                return driver;
            }
        }
        
        public static void Start()
        {
            Login();
            //GetAuthCode();
            GetAccessToken();
        }

        private static void Login()
        {
            Console.WriteLine("Logging in...");
            /*
            Console.WriteLine("Using stored cookies");
            var sr = new StreamReader(Directory.GetCurrentDirectory() + @"\cookies.txt");
            var clientCookies = new CookieContainer();
            string line;
            while ((line = sr.ReadLine()) != null)
            {
                var c = line.Split(':');
                clientCookies.Add(new Uri(baseUrl), new System.Net.Cookie(c[0], c[1]));
            }
            */
            // Go to the url for logging into bungie.net through Battle Net account
            Console.WriteLine("Navigating to page");
            driver.Navigate().GoToUrl(string.Format(loginUrl, bnetCreds["clientId"], bnetCreds["redirectUri"]));

            // Wait until login page loads
            wait.Until(ExpectedConditions.ElementIsVisible(By.ClassName("login")));

            // Get the input fields for username and password along with the submit button
            IWebElement emailInput = driver.FindElement(By.Id("accountName"));
            IWebElement passwInput = driver.FindElement(By.Id("password"));
            IWebElement submitButton = driver.FindElement(By.Id("submit"));

            // Fill in username and password and click submit
            Console.WriteLine("Submitting login info");
            emailInput.SendKeys(bnetLogin["email"]);
            passwInput.SendKeys(bnetLogin["passw"]);
            submitButton.Click();

            wait.Until(ExpectedConditions.ElementIsVisible(By.TagName("em")));
            string loginAuthCode = driver.FindElement(By.TagName("em")).Text;
            SMS.SendText("Login Code: " + loginAuthCode);

            // Wait until login is approved by user
            Console.WriteLine("Waiting for user to approve login...");
            wait.Until(ExpectedConditions.ElementIsVisible(By.XPath("//a[@href='#']")));

            // Get the current cookies and copy them to the CookieContainer for the HttpClient
            // This keeps the user logged into bungie.net as the program switches from selenium to an HttpClient
            Console.WriteLine("Copying cookies");
            var bungieCookies = driver.Manage().Cookies.AllCookies;
            var clientCookies = new CookieContainer();
            var sw = new StreamWriter(Directory.GetCurrentDirectory() + @"\cookies.txt");
            foreach(var c in bungieCookies)
            {
                clientCookies.Add(new Uri(baseUrl), new System.Net.Cookie(c.Name, c.Value));
                sw.WriteLine(c.Name + ":" + c.Value);
            }
            
            driver.Close();
            
            // Create HttpClient with the cookies
            var handler = new HttpClientHandler { CookieContainer = clientCookies };
            client = new HttpClient(handler);

            // Add headers bungie needs for oauth
            client.DefaultRequestHeaders.Add("Accept", "application/json");
            client.DefaultRequestHeaders.Add("X-API-Key", bungieCreds["apiKey"]);
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
                    "Basic", System.Convert.ToBase64String(Encoding.UTF8.GetBytes(bungieCreds["clientId"] + ":" + bungieCreds["clientSecret"])));

            Console.WriteLine("Login Complete");
        }

        private static void GetAuthCode()
        {
            Console.WriteLine("Retrieving auth code...");

            // Send GET request to auth code endpoint
            var authTask = client.GetAsync(string.Format(baseUrl + bungieEndpoints["auth"], bungieCreds["clientId"]));
            authTask.Wait();
            Console.WriteLine(authTask.Result);
            // Get the auth code from the response's url
            Console.WriteLine("Parsing code from url");
            foreach(var header in authTask.Result.Headers)
            {
                if (header.Key == "X-SelfUrl")
                {
                    var url = header.Value.First();
                    authCode = url.Substring(url.IndexOf('=') + 1);
                }
            }

            if (authCode != null)
                Console.WriteLine("Got Auth Code: " + authCode);
        }

        private static void GetAccessToken()
        {
            Console.WriteLine("Retrieving access token...");

            string tokenFilePath = Directory.GetCurrentDirectory() + @"\token.txt";
            try
            {
                StreamReader reader = new StreamReader(tokenFilePath);
                if (reader.ReadToEnd() != "")
                {
                    Console.WriteLine("Getting token from file");
                    reader.BaseStream.Position = 0;
                    var expire = Convert.ToDateTime(reader.ReadLine());
                    refreshToken = reader.ReadLine();
                    accessToken = reader.ReadLine();
                    reader.Close();
                    if (DateTime.Compare(DateTime.Now, expire) < 0)
                    {
                        refreshTimer = new Timer((expire - DateTime.Now).TotalMilliseconds);
                        refreshTimer.Elapsed += new ElapsedEventHandler((sender, e) => { RefreshToken(); });
                        refreshTimer.Start();
                    }
                    else
                    {
                        RefreshToken();
                    }
                }
                else
                {
                    reader.Close();
                    throw new FileNotFoundException();
                }
            }
            catch (FileNotFoundException ex)
            {
                // Get Auth Code first
                GetAuthCode();

                // Need to set the Authorization header
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
                    "Basic", System.Convert.ToBase64String(Encoding.UTF8.GetBytes(bungieCreds["clientId"] + ":" + bungieCreds["clientSecret"])));

                // Encode POST content
                var content = new Dictionary<string, string>
                {
                    { "code", authCode },
                    { "grant_type", "authorization_code" }
                };
                var encodedContentTask = (new FormUrlEncodedContent(content)).ReadAsStringAsync();
                encodedContentTask.Wait();

                // Send POST request to token endpoint
                var responseTask = client.PostAsync(baseUrl + bungieEndpoints["token"], new StringContent(encodedContentTask.Result, Encoding.UTF8, "application/x-www-form-urlencoded"));
                responseTask.Wait();

                // Get JSON from the response
                // First get response as string
                var responseStringTask = responseTask.Result.Content.ReadAsStringAsync();
                responseStringTask.Wait();
                string jsonString = responseStringTask.Result;
                //Deserialize into dictionary for easy reading
                Dictionary<string, string> jsonResponse = JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonString);

                // Set access token, refresh token, and membership id
                accessToken = jsonResponse["access_token"];
                refreshToken = jsonResponse["refresh_token"];
                membershipId = jsonResponse["membership_id"];

                var expires = Double.Parse(jsonResponse["expires_in"]);
                refreshTimer = new Timer(expires * 1000);
                refreshTimer.Elapsed += new ElapsedEventHandler((sender, e) => { RefreshToken(); });
                refreshTimer.Start();

                StreamWriter writer = new StreamWriter(tokenFilePath);
                writer.WriteLine(DateTime.Now.AddSeconds(expires).ToString());
                writer.WriteLine(refreshToken);
                writer.WriteLine(accessToken);
                writer.Close();
            }
            Console.WriteLine("Got Access Token");
        }

        private static void RefreshToken()
        {
            Console.WriteLine("Refreshing token...");

            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
                    "Basic", System.Convert.ToBase64String(Encoding.UTF8.GetBytes(bungieCreds["clientId"] + ":" + bungieCreds["clientSecret"])));

            // Encode POST content
            var content = new Dictionary<string, string>
            {
                { "grant_type", "refresh_token" },
                { "refresh_token", refreshToken }
            };
            var encodedContentTask = (new FormUrlEncodedContent(content)).ReadAsStringAsync();
            encodedContentTask.Wait();

            // Send POST request to token endpoint
            var responseTask = client.PostAsync(baseUrl + bungieEndpoints["token"], new StringContent(encodedContentTask.Result, Encoding.UTF8, "application/x-www-form-urlencoded"));
            responseTask.Wait();

            // Get JSON from the response
            // First get response as string
            var responseStringTask = responseTask.Result.Content.ReadAsStringAsync();
            responseStringTask.Wait();
            string jsonString = responseStringTask.Result;
            
            //Deserialize into dictionary for easy reading
            Dictionary<string, string> jsonResponse = JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonString);
            
            // Set access token variable to new token
            accessToken = jsonResponse["access_token"];
            var expires = Double.Parse(jsonResponse["expires_in"]);
            
            if (refreshTimer == null)
            {
                refreshTimer = new Timer(expires * 1000);
                refreshTimer.Elapsed += new ElapsedEventHandler((sender, e) => { RefreshToken(); });
                refreshTimer.Start();
            }
            else
            {
                refreshTimer.Interval = expires * 1000;
            }

            StreamWriter sw = new StreamWriter(Directory.GetCurrentDirectory() + @"\token.txt");
            sw.WriteLine(DateTime.Now.AddSeconds(expires).ToString());
            sw.WriteLine(refreshToken);
            sw.WriteLine(accessToken);
            sw.Close();

            Console.WriteLine("Token Refreshed");
        }

        public static dynamic AccessResourceGet(string endpoint)
        {
            var responseStringTask = client.GetStringAsync(baseUrl + "/Platform" + endpoint);
            responseStringTask.Wait();
            string jsonString = responseStringTask.Result;

            return JsonConvert.DeserializeObject<dynamic>(jsonString);
        }

        public static dynamic AccessResourcePost(string endpoint, Dictionary<string, string> content)
        {
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
                    "Bearer", accessToken);

            var encodedContent = JsonConvert.SerializeObject(content);
            var responseTask = client.PostAsync(baseUrl + "/Platform" + endpoint, new StringContent(encodedContent, Encoding.UTF8, "application/json"));
            responseTask.Wait();

            var responseStringTask = responseTask.Result.Content.ReadAsStringAsync();
            responseStringTask.Wait();
            string jsonString = responseStringTask.Result;
            Console.WriteLine(jsonString);
            return JsonConvert.DeserializeObject<dynamic>(jsonString);
        }
    }
}
