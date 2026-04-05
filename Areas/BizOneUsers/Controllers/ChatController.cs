using BizOne.DAL;
using BizOne.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace BizOne.Areas.BizOneUsers.Controllers
{
    public class ChatController : Controller
    {
        private readonly ChatRepository _repo = new ChatRepository();

        public ActionResult GetTeamUsers()
        {
            // Ensure RoleIdsUnderMe is formatted with commas for the SQL LIKE query
            string formattedIds = "," + LoginUser.UserData.RoleIdsUnderMe + ",";

            // Call DAL to execute sp_GetMyTeamUsers
            var userList = _repo.GetTeamMembers(LoginUser.EmpId, LoginUser.UserData.RoleId, formattedIds);

            return Json(userList, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public ActionResult GetChatGroups()
        {
            try
            {
                // LoginUser is your session/base object
                long userId = LoginUser.EmpId;
                long roleId = LoginUser.UserData.RoleId;
                string RoleIdsUnderMe = LoginUser.UserData.RoleIdsUnderMe ?? "";
                


                var groups = _repo.GetChatGroups(userId, roleId, RoleIdsUnderMe, LoginUser.Rights.CAN_CHAT_SINGLE_IN_TEAM,
                LoginUser.Rights.CAN_CHAT_SINGLE_IN_ALL,
                LoginUser.Rights.CAN_CHAT_TEAM,
                LoginUser.Rights.CAN_CHAT_PRIVATE_TEAM,
                LoginUser.Rights.CAN_CHAT_ALL_USERS);
                return Json(groups, JsonRequestBehavior.AllowGet);
            }
            catch
            {
                return Json(new { error = "Failed to load groups" }, JsonRequestBehavior.AllowGet);
            }
        }

        // 2. Called when clicking the Plus (+) Button (Loads Individual Users)
        [HttpGet]
        public ActionResult GetIndividualUsers()
        {
            try
            {
                long userId = LoginUser.EmpId;
                long roleId = LoginUser.UserData.RoleId;
                // Formatting for SQL LIKE logic: ,1,2,3,
                string formattedMyTeamIds = "," + (LoginUser.UserData.RoleIdsUnderMe ?? "") + ",";

                var users = _repo.GetIndividualTeamMembers(userId, roleId, formattedMyTeamIds, 
                    LoginUser.Rights.CAN_CHAT_SINGLE_IN_TEAM, LoginUser.Rights.CAN_CHAT_SINGLE_IN_ALL);
                return Json(users, JsonRequestBehavior.AllowGet);
            }
            catch
            {
                return Json(new List<object>(), JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public ActionResult GetChatHistory(string roomIdentifier)
        {
            var history = _repo.GetChatHistory(roomIdentifier);
            return Json(history, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public ActionResult GetTotalUnreadCount()
        {
            int count = 0;
            long userId = LoginUser.EmpId;
            long roleId = LoginUser.UserData.RoleId;
            string RoleIdsUnderMe = LoginUser.UserData.RoleIdsUnderMe ?? "";

            var groups = _repo.GetChatGroups(userId, roleId, RoleIdsUnderMe,
                LoginUser.Rights.CAN_CHAT_SINGLE_IN_TEAM,
                LoginUser.Rights.CAN_CHAT_SINGLE_IN_ALL,
                LoginUser.Rights.CAN_CHAT_TEAM, 
                LoginUser.Rights.CAN_CHAT_PRIVATE_TEAM,
                LoginUser.Rights.CAN_CHAT_ALL_USERS);

            foreach (var group in groups)
            {
                count += group.UnreadCount;
            }
            //long userId = LoginUser.EmpId;
            //int count = _repo.GetTotalUnreadCount(userId);
            return Json(new { count = count }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public async Task<JsonResult> GetAISuggestions(string text)
        {
            try
            {
                // Use the key from AI Studio
                string apiKey = ConfigurationManager.AppSettings["GoggleAIKey"];
                string apiUrl = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key={apiKey}";

                using (HttpClient client = new HttpClient())
                {
                    var requestBody = new
                    {
                        contents = new[] {
                    new {
                        parts = new[] {
                            new { text = $"Context: User is typing a message in a CRM chat. Fragment: '{text}'. Task: Provide atleast 8 short completions/suggestions for improving text verbaly and gramatically. Format: Suggestion1 | Suggestion2 | Suggestion3" }
                        }
                    }
                }
                    };

                    var jsonContent = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json");

                    // Note: No Authorization header needed when using ?key=
                    var response = await client.PostAsync(apiUrl, jsonContent);
                    var jsonResponse = await response.Content.ReadAsStringAsync();

                    if (response.IsSuccessStatusCode)
                    {
                        dynamic result = JsonConvert.DeserializeObject(jsonResponse);

                        // Safely extract the text
                        string aiText = result.candidates[0].content.parts[0].text;

                        var suggestions = aiText.Split('|')
                                                .Select(s => s.Trim())
                                                .Where(s => !string.IsNullOrEmpty(s))
                                                .Take(3)
                                                .ToList();

                        return Json(new { suggestions = suggestions });
                    }

                    return Json(new { error = "AI API Error", details = jsonResponse });
                }
            }
            catch (Exception ex)
            {
                return Json(new { error = "Server Error", details = ex.Message });
            }
        }

    }
}