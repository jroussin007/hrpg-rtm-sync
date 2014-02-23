using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Xml;
using System.Xml.XPath;
using Newtonsoft.Json;

/* TODO:
 * 
 * Add a note in readme.md about how users can create a W7 cron job to synchronize automatically?
 * Come up with an icon.
 * Validate the HRPG user id and HRPG api token through the HRPG api after loading from file.
 * Config file format is fragile, requires value be at correct rows, no blank lines.
 * Minimize to tray and run on schedule.
 * Schedule editor. 
 * Windows installer in order to locate the config file through the registry.
 * On first run after default config file is created, rtmprevsync is set to datetime.now.
 * For release versions, remove the line in the config file that enables "edit and continue" in the debugger.
 * Send StatusUpdate()'s to a logfile?
 * Perform initial check for internet connectivity before anything else.
 * Create a "check for updates" feature.
 * Add an interactive config to write HRPG auth values to .cfg?
 * Add error checking to all web requests.
 * Write a help screen.
 * 
 */

namespace Hrpg_Rtm_Sync
{
    public partial class Form1 : Form
    {
        const string Version = "1.00";
        string RtmApiKey = "e20eefb0675e911b750e031558482cb4",
               RtmSharedSecret = "2df854dd30f2d6ac",
               RtmAuthToken = "",
               HrpgUserId = "",
               HrpgApiToken = "";
        string[] HrpgHabitNames = new string[]                                                                      // Used when creating new habits.
                    { "Completed Low Difficulty RTM Task", "Completed Medium Difficulty RTM Task", "Completed High Difficulty RTM Task" },
                 HrpgHabitIds = new string[] { "", "", "" };                                                          // Hrpg habit ids for difficulty level [0] is "low", [1] is "medium", [2] is "high".
        DateTime RtmPrevSync = new DateTime();

        public Form1()
        {
            InitializeComponent();

            StatusUpdate("Hrpg-Rtm Sync v" + Version + ".");
            StatusUpdate("\nClick \"Sync\" to synchronize data.\n");
        }

        private void btnSync_Click(object sender, EventArgs e)
        {
            StringBuilder Url = new StringBuilder();

            XPathNodeIterator RtmCompletedTasks;

            StatusUpdate("\nLoading settings...");
            LoadSettingsFromFile();

            VerifyHrpgAuthValuesExist();

            // TODO: Should probably localize the time/date...

            string Format = "yyyy-MM-dd HH:mm:ss";
            StatusUpdate("\nPrevious sync was: " + RtmPrevSync.ToString(Format) + ".");

            StatusUpdate("\nVerifying RTM auth token validity...");
            if (RtmAuthTokenIsValid() == false)
            {
                StatusUpdate("\nRTM reports auth token is not valid.");
                StatusUpdate("\nBeginning RTM authentication process...");

                AuthenticateToRtm();
            }

            StatusUpdate("\nRetrieving RTM data...");
            RtmCompletedTasks = GetRtmCompletedTasks();

            StatusUpdate("\nNumber of newly completed RTM tasks: " + RtmCompletedTasks.Count);

            if (RtmCompletedTasks.Count > 0)
            {
                UpdateHrpgForCompletedRtmTasks(RtmCompletedTasks);

                // TODO: Only update RtmPrevSync if UpdateHrpgForCompletedRtmTasks() successfully updates HRPG.

                RtmPrevSync = DateTime.Now;                                                                         // Written to file later, read on next startup.
            }

            StatusUpdate("\nUpdating settings...");

            WriteSettingsToFile();

            StatusUpdate("\n\nFinished synchronizing.\n");
        }

        public void StatusUpdate(string message)
        {
            // Purpose: Updates the richtextbox.

            rtbStatus.AppendText(message);
            rtbStatus.Refresh();
            rtbStatus.ScrollToCaret();
        }

        private void btnExit_Click(object sender, EventArgs e)
        {
            System.Environment.Exit(0);
        }

        public void LoadSettingsFromFile()
        {
            // Purpose: Load various settings from settings.cfg.

            // TODO: Make the catch block more robust. Currently it's indiscriminately catching "Exception".
            try
            {
                string Setting;
                StreamReader Reader = new StreamReader(@"hrpg-rtm-sync.cfg");

                HrpgUserId = GetNextSetting(Reader);
                HrpgApiToken = GetNextSetting(Reader);
                RtmAuthToken = GetNextSetting(Reader);

                Setting = GetNextSetting(Reader);
                if (Setting != "")
                {
                    RtmPrevSync = DateTime.Parse(Setting);
                }
                else
                {
                    RtmPrevSync = new DateTime();
                    RtmPrevSync = DateTime.UtcNow;
                }

                HrpgHabitIds[0] = GetNextSetting(Reader);
                HrpgHabitIds[1] = GetNextSetting(Reader);
                HrpgHabitIds[2] = GetNextSetting(Reader);

                Reader.Close();
            }
            catch (Exception)
            {
                DialogResult Result;

                Result = MessageBox.Show("Error encountered in Hrpg-Rtm-Sync.cfg file or file not found.\n\nClick \"Yes\" to create a new config file in the current folder. Click \"No\" to exit.", "Error", MessageBoxButtons.YesNo);

                if (Result == DialogResult.Yes)
                {
                    WriteSettingsToFile();

                    MessageBox.Show("A new Hrpg-Rtm-Sync.cfg file has been created in the current folder. Please edit the file and provide the necessary values, then restart Hrpg-Rtm Sync.");
                }

                System.Environment.Exit(-1);
            }

            return;
        }

        public void WriteSettingsToFile()
        {
            // Purpose: Recreate settings.cfg, saving current values.

            StreamWriter Writer = new StreamWriter(@"hrpg-rtm-sync.cfg");

            Writer.WriteLine("# Settings file for Hrpg-Rtm Sync.");
            Writer.WriteLine("#");
            Writer.WriteLine("# Comments begin with \"#\".");
            Writer.WriteLine("# Do not insert blank lines.");
            Writer.WriteLine("#");
            Writer.WriteLine("# User-supplied values:");
            Writer.WriteLine("#      Please place your HRPG \"User ID\" value on the next line. Find the value at HabitRpg -> Options -> Settings -> API.");
            Writer.WriteLine(HrpgUserId);
            Writer.WriteLine("#");
            Writer.WriteLine("#      Please place your HRPG \"API Token\" value on the next line. Find the value on same screen as your HRPG \"User ID.\"");
            Writer.WriteLine(HrpgApiToken);
            Writer.WriteLine("# End user-supplied values.");
            Writer.WriteLine("#");
            Writer.WriteLine("# RTM Auth Token. If blank, will attempt to authenticate.");
            Writer.WriteLine(RtmAuthToken);
            Writer.WriteLine("#");
            Writer.WriteLine("# RTM Previous Sync.");

            if (RtmPrevSync == new DateTime())                                                                      // If PrevSync is blank -- ie this method was called by WriteDefaultSettingsToFile().
            {
                Writer.WriteLine("");
            }
            else
            {
                Writer.WriteLine(RtmPrevSync.ToString("o"));                                                        // Record datatime in ISO 8601 format, which is what RTM understands.
            }

            Writer.WriteLine("#");
            Writer.WriteLine("# HRPG Habit Id for #d1 RTM task.");
            Writer.WriteLine(HrpgHabitIds[0]);
            Writer.WriteLine("#");
            Writer.WriteLine("# HRPG Habit Id for #d2 RTM task.");
            Writer.WriteLine(HrpgHabitIds[1]);
            Writer.WriteLine("#");
            Writer.WriteLine("# HRPG Habit Id for #d3 RTM task.");
            Writer.WriteLine(HrpgHabitIds[2]);

            Writer.Close();

            return;
        }
        
        public string GetNextSetting(StreamReader s)
        {
            // Purpose: Return the next non-comment line from hrpg-rtm-sync.cfg. Comment lines begin with "#".

            string Setting = "";

            do
            {
                Setting = s.ReadLine().Trim();
            } while (Setting.StartsWith("#"));

            return Setting;
        }

        public void VerifyHrpgAuthValuesExist()
        {
            // Purpose: If either HrpgUserId=="" or HrpgApiToken=="" tell the user to edit the .cfg and exit.
            
            if(HrpgUserId.Trim() == "" || HrpgApiToken.Trim() == "")
            {
                MessageBox.Show("Please edit hrpg-rtm-sync.cfg and supply values for HRPG User ID and HRPG API Token before continuing.");
                System.Environment.Exit(1);
            }
        }

        public bool RtmAuthTokenIsValid()
        {
            // Purpose: Ask RTM whether the auth token is valid and return true or false.

            bool Validity = false;
            string RtmApiResponse = "";
            SortedDictionary<string, string> ApiParams = new SortedDictionary<string, string>();
            XPathNavigator ResponseNav, Node;

            ApiParams.Add("api_key", RtmApiKey);
            ApiParams.Add("auth_token", RtmAuthToken);
            ApiParams.Add("method", "rtm.auth.checkToken");

            RtmApiResponse = CallRtmApi(CreateRtmApiCall("rest", ApiParams));

            ResponseNav = GetXPathNav(RtmApiResponse);

            Node = ResponseNav.SelectSingleNode("/rsp");

            if (Node.GetAttribute("stat", "") == "ok")                                                                   // Look for status code "ok" indicating auth token is valid.
                Validity = true;

            return Validity;
        }

        public void AuthenticateToRtm()
        {
            // Purpose: Perform multi-step RTM authentication process.

            /*
             * This method applies the RTM authentication procedure detailed in the "User authentication for desktop applications" section at https://www.rememberthemilk.com/services/api/authentication.rtm.
             * 
             * 3 basic steps:
             * 
             *      1. Get frob from RTM.
             *      2. Load specific RTM page and let user authorize the app, logging in first if necessary.
             *      3. After user authorizes app, get auth token from RTM.
             *      
             */

            string RtmAuthFrob = "";
            SortedDictionary<string, string> ApiParams = new SortedDictionary<string, string>();

            StatusUpdate("\nRequesting RTM auth frob...");

            RtmAuthFrob = GetRtmAuthFrob();                                                                         // Step 1: Get frob.

            // TODO: Verify RtmAuthFrob != "" from error condition in GetRtmAuthFrob().

            GetRtmAuthorizationFromUser(RtmAuthFrob);                                                               // Step 2: Load RTM webpage in default browser and let user authorize the app. Continue when user clicks "Proceed."

            // TODO: Make sure RtmAuthToken != "" from error condition in GetRtmAuthToken().

            StatusUpdate("\nRequesting RTM auth token...");

            GetRtmAuthToken(RtmAuthFrob);                                                                           // Step 3: Get auth token from RTM api.
        }

        public void GetRtmAuthToken(string rtmAuthFrob)
        {
            // Purpose: Get an auth token from RTM and store it in the global string RtmAuthToken.

            string RtmGetAuthTokenUrl,
                   RtmResponse;
            SortedDictionary<string, string> RtmApiParamSet = new SortedDictionary<string, string>();
            XPathNavigator ResponseNav, Node;

            RtmApiParamSet.Add("method", "rtm.auth.getToken");
            RtmApiParamSet.Add("api_key", RtmApiKey);
            RtmApiParamSet.Add("frob", rtmAuthFrob);

            RtmGetAuthTokenUrl = CreateRtmApiCall("rest", RtmApiParamSet);

            RtmResponse = CallRtmApi(RtmGetAuthTokenUrl);

            ResponseNav = GetXPathNav(RtmResponse);

            Node = ResponseNav.SelectSingleNode("/rsp");

            if (Node.GetAttribute("stat", "") == "ok")
            {
                // TODO: Add error checking here.

                Node = ResponseNav.SelectSingleNode("/rsp/auth/token");

                RtmAuthToken = Node.TypedValue.ToString();
            }
            else
            {
                RtmAuthToken = "";
            }

            StatusUpdate("\nRTM auth token obtained.");

            return;
        }

        public string GetRtmAuthFrob()
        {
            // Purpose: Get and return an auth frob from RTM.

            string RtmGetFrobUrl,
                   RtmResponse,
                   RtmAuthFrob;
            SortedDictionary<string, string> RtmApiParamSet = new SortedDictionary<string, string>();
            XPathNavigator ResponseNav, Node;

            RtmApiParamSet.Add("api_key", RtmApiKey);
            RtmApiParamSet.Add("method", "rtm.auth.getFrob");

            RtmGetFrobUrl = CreateRtmApiCall("rest", RtmApiParamSet);

            RtmResponse = CallRtmApi(RtmGetFrobUrl);

            ResponseNav = GetXPathNav(RtmResponse);

            Node = ResponseNav.SelectSingleNode("/rsp");

            if (Node.GetAttribute("stat", "") == "ok")
            {
                Node = ResponseNav.SelectSingleNode("/rsp/frob");

                RtmAuthFrob = Node.TypedValue.ToString();
            }
            else
            {
                RtmAuthFrob = "";
            }

            StatusUpdate("\nRTM auth frob obtained.");

            return RtmAuthFrob;
        }

        public void GetRtmAuthorizationFromUser(string rtmAuthFrob)
        {
            // Purpose: Load the RTM app authorization page in the user's default browser which will authorize this app to read data from the user's RTM account.
            //          Prompt the user to interact with the RTM page to authorize this app.
            //          Display a modal dialog and wait for the user to click "Ok" before proceeding.

            StringBuilder RtmAuthorizationByUserUrl = new StringBuilder();
            SortedDictionary<string, string> ApiParams = new SortedDictionary<string, string>();
            DialogResult Result;

            ApiParams.Add("api_key", RtmApiKey);
            ApiParams.Add("perms", "read");
            ApiParams.Add("frob", rtmAuthFrob);

            RtmAuthorizationByUserUrl.Append(CreateRtmApiCall("auth", ApiParams));

            Result = MessageBox.Show("In order to update HabitRPG based on your Remember the Milk (\"RTM\") tasks, this app must be authorized to read your RTM account data.\n\nYour task data will not be stored and will not be shared with anyone.\n\nRTM will now be loaded in your browser. Please log in and follow the prompts to authorize this app.\n\nClick \"Ok\" to continue.", "Authorize App at RTM", MessageBoxButtons.OKCancel);

            // TODO: Make this more robust?
            if (Result == DialogResult.Cancel)
            {
                System.Environment.Exit(0);
            }

            StatusUpdate("\nLoading browser to obtain RTM authorization...");

            System.Diagnostics.Process DefaultBrowser = new System.Diagnostics.Process();                           // Load RTM authorization url in user's default browser.
            DefaultBrowser.StartInfo.FileName = RtmAuthorizationByUserUrl.ToString();
            DefaultBrowser.Start();

            Result = MessageBox.Show("Click \"Ok\" after authorizing the app.", "Hrpg-Rtm Sync", MessageBoxButtons.OKCancel);

            // TODO: Make this more robust?
            if (Result == DialogResult.Cancel)
            {
                StatusUpdate("Exiting...");

                System.Environment.Exit(0);
            }
        }

        public XPathNodeIterator GetRtmCompletedTasks()
        {
            // Purpose: Return an XPathNodeIterator with any completed RTM tasks.

            string RtmApiResponse;
            StringBuilder Url = new StringBuilder();
            SortedDictionary<string, string> ApiParams = new SortedDictionary<string, string>();
            XPathNavigator XPathNav;
            XPathNodeIterator RtmCompletedTasksIterator;

            ApiParams.Add("api_key", RtmApiKey);
            ApiParams.Add("auth_token", RtmAuthToken);
            ApiParams.Add("filter", "status:completed");                                                            // Request only completed tasks.
            ApiParams.Add("last_sync", RtmPrevSync.ToString("o"));                                                  // Request only tasks marked "completed" since RtmPrevSync. Format datetime as ISO 8601 for RTM.
            ApiParams.Add("method", "rtm.tasks.getList");

            Url.Append(CreateRtmApiCall("rest", ApiParams));

            RtmApiResponse = CallRtmApi(Url.ToString());

            XPathNav = GetXPathNav(RtmApiResponse);

            RtmCompletedTasksIterator = XPathNav.SelectDescendants("taskseries", "", false);

            RtmCompletedTasksIterator.MoveNext();                                                                   // Select first task.

            return RtmCompletedTasksIterator;
        }

        public void UpdateHrpgForCompletedRtmTasks(XPathNodeIterator RtmCompletedTasksIterator)
        {
            // Purpose: Update the HRPG habit of the appropriate difficulty level for each completed RTM task.

            int TaskNum = 0,
                HrpgDifficultyLevel;

            VerifyHrpgHabitsExist();

            // TODO: Error check on the responses from HRPG.

            // For each task:
            //     1. Check if it contains an HRPG difficulty tag: "d1" "d2" or "d3"
            //            - If no HRPG difficulty tag present, default difficulty is "d1"
            //     2. Call the appropriate HRPG habit based on any difficulty tag.            
            do
            {
                HrpgDifficultyLevel = GetTaskDifficultyTag(RtmCompletedTasksIterator.Current);

                TaskNum++;
                StatusUpdate("\nSyncing task " + TaskNum.ToString() + " of " + RtmCompletedTasksIterator.Count.ToString() + "...");

                switch (HrpgDifficultyLevel)
                {
                    case 0:
                        {
                            IncrementHrpgHabit(0);

                            break;
                        }

                    case 1:
                        {
                            IncrementHrpgHabit(1);

                            break;
                        }

                    case 2:
                        {
                            IncrementHrpgHabit(2);

                            break;
                        }
                }
            } while (RtmCompletedTasksIterator.MoveNext());
        }

        public void IncrementHrpgHabit(int difficultyLevel)
        {
            string Url = "https://beta.habitrpg.com:443/api/v2/user/tasks/" + HrpgHabitIds[difficultyLevel] + "/up";

            CallHrpgApi(Url, "POST", "");
        }

        public void VerifyHrpgHabitsExist()
        {
            // Purpose: Get all HRPG habits. Verify that the three habits that correspond to RTM tasks with difficulty levels 1, 2, 3, exist. Get the HRPG internal identifier ("id") for each. Create habits as necessary.

            /* As of 2014-02-04 the following JSON creates a new habit:
             * 
             * Change the "priority" to set the difficulty: 1 for "easy," 1.5 for "medium," 2 for "hard."
             * Not sure what "value" does.
             * 
             * {
             * "text": "habit name",
             * "attribute": "str",
             * "priority": 1,
             * "value": 0,
             * "notes": "notes",
             * "dateCreated": "2014-01-15T01:49:51.182Z",
             * "down": true,
             * "up": true,
             * "history": [],
             * "type": "habit"
             * }
             */

            StatusUpdate("\nRequesting HRPG habit data...");

            if (HrpgHabitExists(HrpgHabitIds[0]) == false)
            {
                StatusUpdate("\nHrpg habit for difficulty level 1 not found.");

                StatusUpdate("\nCreating Hrpg habit for difficulty level 1...");

                CreateHrpgHabit(0);
            }

            if (HrpgHabitExists(HrpgHabitIds[1]) == false)
            {
                StatusUpdate("\nHrpg habit for difficulty level 2 not found.");

                StatusUpdate("\nCreating Hrpg habit for difficulty level 2...");

                CreateHrpgHabit(1);
            }

            if (HrpgHabitExists(HrpgHabitIds[2]) == false)
            {
                StatusUpdate("\nHrpg habit for difficulty level 3 not found.");

                StatusUpdate("\nCreating Hrpg habit for difficulty level 3...");

                CreateHrpgHabit(2);
            }
        }

        public bool HrpgHabitExists(string hrpgHabitId)
        {
            // Purpose: Ask Hrpg whether or not a task exists. Return "true" if yes, "false" if no.

            bool ReturnVal = false;
            string Response;
            StringBuilder GetHrpgTaskUrl = new StringBuilder("https://beta.habitrpg.com:443/api/v2/user/tasks");
            dynamic HrpgResponse;

            if (hrpgHabitId != "")
            {
                GetHrpgTaskUrl.Append("/").Append(hrpgHabitId);

                Response = CallHrpgApi(GetHrpgTaskUrl.ToString(), "GET", "");

                HrpgResponse = JsonConvert.DeserializeObject<dynamic>(Response);

                if (HrpgResponse.ToString().Contains("\"type\": \"habit\""))                                        // Hrpg returns json {"err":"No task found."} if the task does not exist and the task data, including "type", if it does.
                {
                    ReturnVal = true;
                }
            }

            return ReturnVal;
        }

        public void CreateHrpgHabit(int difficultyLevel)
        {
            // Purpose: Create an Hrpg habit for the difficulty level. 0="low" 1="medium" 2="high" difficulty.

            string HrpgResponse;
            string[] HrpgPriorityLevel = new string[] { "1.0", "1.5", "2.0" };                                      // Hrpg defines "low difficult" tasks having a "priority" value of "1.0", "medium" is "1.5", "difficult" is "2.0".
            dynamic HrpgTask;

            string CreateHabit = "{\"text\": \"" + HrpgHabitNames[difficultyLevel] + "\", \"attribute\": \"str\", \"priority\": " + HrpgPriorityLevel[difficultyLevel] + ", \"value\": 0, \"notes\": \"\", \"dateCreated\": \"" + DateTime.UtcNow.ToString() + "\", \"down\": true, \"up\": true, \"history\": [], \"type\": \"habit\"}";

            HrpgResponse = CallHrpgApi("https://beta.habitrpg.com:443/api/v2/user/tasks", "POST", CreateHabit);

            HrpgTask = JsonConvert.DeserializeObject<dynamic>(HrpgResponse);

            // TODO: Will Hrpg always return json with an "id" element? If not, need to add code to deal with it here.

            HrpgHabitIds[difficultyLevel] = HrpgTask.id.ToString();

            // TODO: Return a response code.
        }

        public int GetTaskDifficultyTag(XPathNavigator rtmCompletedTasksIterator)
        {
            // Purpose: Return 0, 1, or 2, equal to the task's difficulty tag "d1" "d2" or "d3". If the task has no "d#" tag, default to "d1".

            int DifficultyLevel = 0;
            XPathNodeIterator TagIterator = rtmCompletedTasksIterator.SelectDescendants("tag", "", false);

            while (TagIterator.MoveNext())                                                                          // Search through any <Tag> elements. If multiple "d#" tags are present, return the highest value.
            {
                switch (TagIterator.Current.Value.ToString())
                {
                    case "d2":
                        {
                            if (DifficultyLevel == 0)
                            {
                                DifficultyLevel = 1;
                            }

                            break;
                        }
                    case "d3":
                        {
                            DifficultyLevel = 2;
                            break;
                        }
                }
            }

            return DifficultyLevel;
        }

        public string CreateRtmApiCall(string service, SortedDictionary<string, string> apiParamsSet)
        {
            // Purpose: Create a url string to call the RTM api with a specified set of parameters.
            //          See http://www.rememberthemilk.com/services/api/ for documentation of the RTM api.

            string RtmApiBaseUrl = "http://www.rememberthemilk.com/services/";

            StringBuilder RtmApiSigUnhashed = new StringBuilder(),
                          RtmApiSigHashed = new StringBuilder(),
                          Url = new StringBuilder();

            MD5 MakeMd5 = MD5.Create();

            Url.Append(RtmApiBaseUrl).Append(service).Append("?");

            RtmApiSigUnhashed.Append(RtmSharedSecret);

            foreach (KeyValuePair<string, string> Kvp in apiParamsSet)
            {
                RtmApiSigUnhashed.Append(Kvp.Key).Append(Kvp.Value);

                Url.Append(Kvp.Key).Append("=").Append(Kvp.Value).Append("&");
            }

            RtmApiSigHashed.Append(GetMd5Hash(RtmApiSigUnhashed.ToString()));

            Url.Append("api_sig=").Append(RtmApiSigHashed.ToString());

            return Url.ToString();
        }

        public string CallRtmApi(string url)
        {
            // Purpose: Call the RTM api with at a specified url and return the server's response.

            string ResponseFromServer;
            Stream DataStream;
            StreamReader Reader;
            HttpWebRequest Request;
            HttpWebResponse Response;

            Request = (HttpWebRequest)WebRequest.Create(url);                                                       // Create a request for the URL. 		

            Request.Credentials = CredentialCache.DefaultCredentials;                                               // Set the credentials. Not sure if this is necessary.
            Request.Host = "www.rememberthemilk.com";                                                               // Set header.

            Response = (HttpWebResponse)Request.GetResponse();                                                      // Get the response.

            DataStream = Response.GetResponseStream();                                                              // Get the stream containing content returned by the server.

            Reader = new StreamReader(DataStream);                                                                  // Open the stream using a StreamReader for easy access.

            ResponseFromServer = Reader.ReadToEnd();                                                                // Read the content. 

            Reader.Close();                                                                                         // Cleanup the streams and the response.
            DataStream.Close();
            Response.Close();

            return ResponseFromServer;
        }

        public string CallHrpgApi(string url, string method, string json)
        {
            // Purpose: Call the HRPG api at a specified url and return the server's response.

            string ResponseFromServer;
            HttpWebRequest Request = (HttpWebRequest)WebRequest.Create(url.ToString());
            HttpWebResponse Response;
            Stream DataStream;
            StreamReader Reader;

            Request.Credentials = CredentialCache.DefaultCredentials;                                               // Set the credentials. Not sure if this is necessary.

            Request.Method = method;
            Request.Headers.Add("x-api-key:" + HrpgApiToken);
            Request.Headers.Add("x-api-user:" + HrpgUserId);

            if (json != "")
            {
                byte[] Utf8Json;
                Stream HrpgStream;

                Request.Method = "POST";

                Utf8Json = Encoding.UTF8.GetBytes(json);

                Request.ContentType = "application/json";
                Request.ContentLength = Utf8Json.Length;

                // TODO: Put this in a try/catch block.

                HrpgStream = Request.GetRequestStream();
                HrpgStream.Write(Utf8Json, 0, Utf8Json.Length);
                HrpgStream.Flush();
                HrpgStream.Close();
            }

            try                                                                                                     // Get the response.
            {
                Response = (HttpWebResponse)Request.GetResponse();

                DataStream = Response.GetResponseStream();

                Reader = new StreamReader(DataStream);

                ResponseFromServer = Reader.ReadToEnd();

                Reader.Close();
                DataStream.Close();
                Response.Close();

                return ResponseFromServer;
            }
            catch (System.Net.WebException e)
            {
                // So, hrpg returns an http 404 along with the json {"err":"No task found."} for a call to the /tasks method of the api with a habit id that doesn't exist. HttpWebResponse.GetResponse() throws exceptions for 404s. This code, from SO (http://stackoverflow.com/a/1857595/2484571) works around that.

                Response = e.Response as HttpWebResponse;

                DataStream = Response.GetResponseStream();

                Reader = new StreamReader(DataStream);

                ResponseFromServer = Reader.ReadToEnd();

                Reader.Close();
                DataStream.Close();
                Response.Close();

                return ResponseFromServer;
            }
        }

        public XPathNavigator GetXPathNav(string s)
        {
            // Purpose: Create and return an XPathNavigator for the specified string.

            StringReader SReader;
            XmlReader Reader;
            XPathDocument XPathDoc;
            XPathNavigator XPathNav;

            SReader = new StringReader(s);
            Reader = XmlReader.Create(SReader);
            XPathDoc = new XPathDocument(Reader);
            XPathNav = XPathDoc.CreateNavigator();

            return XPathNav;
        }

        public string GetMd5Hash(string plainText)                                                                  // HT http://stackoverflow.com/a/13806183
        {
            // Purpose: Create and return an MD5 hash from the specified string.

            MD5 Md5Hash = MD5.Create();

            byte[] Hashed = Md5Hash.ComputeHash(Encoding.UTF8.GetBytes(plainText));                                 // Convert the input string to a byte array and compute the hash.

            StringBuilder CipherText = new StringBuilder();

            for (int i = 0; i < Hashed.Length; i++)
            {
                CipherText.Append(Hashed[i].ToString("x2"));                                                        // "x2" specifies hexadecimal format.
            }

            return CipherText.ToString();
        }
    }
}
