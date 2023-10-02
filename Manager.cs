using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;

namespace AdsPowerManager
{
    /// <summary>
    /// Менеджер для управления браузером.
    /// </summary>
    public  class BrowserManager
    {
        /// <summary>
        /// Инициализирует веб-драйвер браузера.
        /// </summary>
        /// <param name="profileId">Идентификатор профиля.</param>
        /// <returns>Интерфейс веб-драйвера.</returns>
        public  async Task<IWebDriver> InitializeDriver(string profileId)
        {
            string launchUrl = $"http://local.adspower.com:50325/api/v1/browser/start?user_id={profileId}";

            using var httpClient = new HttpClient();
            var response = await httpClient.GetAsync(launchUrl);
            string responseString = await response.Content.ReadAsStringAsync();

            JObject responseDataJson = null;
            try
            {
                responseDataJson = JObject.Parse(responseString);
            }
            catch (JsonReaderException ex)
            {
                Console.WriteLine($"Failed to parse response JSON: {ex.Message}");
                return null;
            }

            string? status = string.Empty;
            string? remoteAddressWithSelenium = string.Empty;
            string? webdriverPath = string.Empty;
            try
            {
                status = (string?)responseDataJson["msg"];
                remoteAddressWithSelenium = (string?)responseDataJson?["data"]?["ws"]?["selenium"];
                webdriverPath = (string?)responseDataJson?["data"]?["webdriver"];
            }
            catch (Exception)
            {
                // Обработка исключения по необходимости
            }

            if (status == "failed")
            {
                return null;
            }

            var options = new ChromeOptions();
            options.AddArguments(
                "start-maximized",
                "enable-automation",
                "--headless",
                "--no-sandbox",
                "--disable-infobars",
                "--disable-dev-shm-usage",
                "--disable-browser-side-navigation",
                "--disable-gpu",
                "--ignore-certificate-errors");

            options.DebuggerAddress = remoteAddressWithSelenium;
            var service = ChromeDriverService.CreateDefaultService();
            string? chromeDriverDirectory = System.IO.Path.GetDirectoryName(webdriverPath);
            service.DriverServicePath = chromeDriverDirectory;
            service.DriverServiceExecutableName = "chromedriver.exe";
            var driver = new ChromeDriver(service, options, TimeSpan.FromMinutes(5));

            return driver;
        }

        /// <summary>
        /// Закрывает браузер по идентификатору пользователя.
        /// </summary>
        /// <param name="userId">Идентификатор пользователя.</param>
        /// <returns>True, если браузер успешно закрыт, иначе false.</returns>
        public  async Task<bool> CloseBrowser(string userId)
        {
            string apiUrl = $"http://local.adspower.com:50325/api/v1/browser/stop?user_id={userId}";

            using var httpClient = new HttpClient();
            var response = await httpClient.GetAsync(apiUrl);

            if (response.IsSuccessStatusCode)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }

    /// <summary>
    /// Менеджер для работы с группами.
    /// </summary>
    public class GroupManager
    {
        /// <summary>
        /// Получает список групп.
        /// </summary>
        /// <returns>Список групп.</returns>
        public  async Task<List<Group>> GetGroups()
        {
            string apiUrl = "http://local.adspower.com:50325/api/v1/group/list?page_size=100";
            var httpClient = new HttpClient();

            var response = await httpClient.GetAsync(apiUrl);
            string responseString = await response.Content.ReadAsStringAsync();

            JObject responseDataJson = JObject.Parse(responseString);

            int code = (int)responseDataJson["code"];
            if (code != 0)
            {
                string errorMsg = (string?)responseDataJson["msg"];
                Console.WriteLine($"Failed to get groups: {errorMsg}");
                return null;
            }

            List<Group> groups = new List<Group>();
            JArray groupsJsonArray = (JArray)responseDataJson["data"]["list"];
            foreach (JToken groupJson in groupsJsonArray)
            {
                Group group = new Group
                {
                    GroupId = (string?)groupJson["group_id"],
                    GroupName = (string?)groupJson["group_name"]
                };

                groups.Add(group);
            }

            return groups;
        }
    }

    /// <summary>
    /// Группа.
    /// </summary>
    public class Group
    {
        public string? GroupId { get; set; }
        public string? GroupName { get; set; }
    }

    /// <summary>
    /// Менеджер для работы с профилями.
    /// </summary>
    public class ProfileManager
    {
        /// <summary>
        /// Получает список профилей.
        /// </summary>
        /// <returns>Список профилей.</returns>
        public async Task<List<Profile>> GetProfiles()
        {
            string apiUrl = "http://local.adspower.com:50325/api/v1/user/list?page_size=100";
            var httpClient = new HttpClient();

            var response = await httpClient.GetAsync(apiUrl);

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Failed to get profiles. Status code: {response.StatusCode}");
                return null;
            }

            var responseString = await response.Content.ReadAsStringAsync();

            JObject responseDataJson = JObject.Parse(responseString);

            int code = (int)responseDataJson["code"];
            if (code != 0)
            {
                string errorMsg = (string)responseDataJson["msg"];
                Console.WriteLine($"Failed to get profiles: {errorMsg}");
                return null;
            }

            var profilesJsonArray = (JArray)responseDataJson["data"]["list"];
            var profiles = new List<Profile>();

            foreach (JToken profileJson in profilesJsonArray)
            {
                Profile profile = new Profile
                {
                    SerialNumber = (string?)profileJson["serial_number"],
                    UserId = (string?)profileJson["user_id"],
                    Name = (string?)profileJson["name"],
                    GroupId = (string?)profileJson["group_id"],
                    GroupName = (string?)profileJson["group_name"],
                    DomainName = (string?)profileJson["domain_name"],
                    Username = (string?)profileJson["username"],
                    Remark = (string?)profileJson["remark"],
                    CreatedTime = DateTimeOffset.FromUnixTimeSeconds((long)profileJson["created_time"]).DateTime,
                    IP = (string?)profileJson["ip"],
                    IPCountry = (string?)profileJson["ip_country"],
                    Password = (string?)profileJson["password"],
                    LastOpenTime = DateTimeOffset.FromUnixTimeSeconds((long)profileJson["last_open_time"]).DateTime
                };

                profiles.Add(profile);
            }

            return profiles;
        }
    }

    /// <summary>
    /// Профиль пользователя.
    /// </summary>
    public class Profile
    {
        public string? SerialNumber { get; set; }
        public string? UserId { get; set; }
        public string? Name { get; set; }
        public string? GroupId { get; set; }
        public string? GroupName { get; set; }
        public string? DomainName { get; set; }
        public string? Username { get; set; }
        public string? Remark { get; set; }
        public DateTime CreatedTime { get; set; }
        public string? IP { get; set; }
        public string? IPCountry { get; set; }
        public string? Password { get; set; }
        public DateTime LastOpenTime { get; set; }
    }
}
