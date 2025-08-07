using BVFG_Web.Models.AdminModel;
using BVFG_Web.Models.Dtos.AdminDto;
using BVFG_Web.Services.ApiUrls;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text;

namespace BVFG_Web.Services.AdminService
{
    public class AdminService: IAdminService
    {
        private readonly HttpClient _httpClient;
        public AdminService()
        {
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(ApiEndpoints.BaseApiUrl)
            };

        }

        public async Task<ResponseModle> AddMember(Admin member)
        {
            ResponseModle response = new ResponseModle();
            try
            {
                var json = JsonConvert.SerializeObject(member);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var url = $"{Endpoints.AddMember}";
                var result = await _httpClient.PostAsync(url, content);
                var responseString = await result.Content.ReadAsStringAsync();
                if (result.IsSuccessStatusCode)
                {
                    response = JsonConvert.DeserializeObject<ResponseModle>(responseString);
                }
                else
                {
                    response.Status = "Failed";
                    response.Message = $"API Error: {result.StatusCode}";
                    response.Data = null;
                }
            }
            catch(Exception ex)
            {
                response.Status = "Failed";
                response.Message = ex.Message;
                response.Data = null;
            }

            return response;
        }

        public async Task<ResponseModle> AdminLogin(AdminLoginDto adminLogin)
        {
            ResponseModle response=new ResponseModle();
            
            try
            {
                var json = JsonConvert.SerializeObject(adminLogin);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var url = $"{Endpoints.AdminLogin}";
                var result = await _httpClient.PostAsync(url, content);

                var responseString = await result.Content.ReadAsStringAsync();
                if (result.IsSuccessStatusCode)
                {
                    response = JsonConvert.DeserializeObject<ResponseModle>(responseString);
                   
                    if (response.Data is Newtonsoft.Json.Linq.JObject jObj)
                    {
                        response.Data = jObj.ToObject<AdminLogin>();
                    }
                }
                else
                {
                    response.Status = "Failed";
                    response.Message = $"API Error: {result.StatusCode}";
                    response.Data = null;
                }
            }
            catch (Exception ex)
            {
                response.Status = "Failed";
                response.Message = ex.Message;
                response.Data = null;
            }

            return response;
        }

        public async Task<ResponseModle> GetAllMember()
        {
            ResponseModle response = new ResponseModle();
            try
            {
                var url = $"{Endpoints.GetAllMember}";
                var result = await _httpClient.GetAsync(url);
                var responseString = await result.Content.ReadAsStringAsync();

                if (result.IsSuccessStatusCode)
                {
                   
                    var jsonObject = JObject.Parse(responseString);

                    
                    response.Status = jsonObject["status"]?.ToString();
                    response.Message = jsonObject["message"]?.ToString();

                    var dataArray = jsonObject["data"]?.ToObject<List<MstMember_Edit>>();
                    response.Data = dataArray;
                }
                else
                {
                    response.Status = "Failed";
                    response.Message = $"API Error: {result.StatusCode}";
                    response.Data = null;
                }
            }
            catch (Exception ex)
            {
                response.Status = "Failed";
                response.Message = ex.Message;
                response.Data = null;
            }

            return response;
        }

        public async Task<ResponseModle> GetMemberById(long id)
        {
            ResponseModle response=new ResponseModle();
            try
            {
                var url = $"{Endpoints.GetMemberById}?MemberId={id}";
                var result = await _httpClient.GetAsync(url);
                var responseString = await result.Content.ReadAsStringAsync();
                if (result.IsSuccessStatusCode)
                {

                    var jsonObject = JObject.Parse(responseString);


                    response.Status = jsonObject["status"]?.ToString();
                    response.Message = jsonObject["message"]?.ToString();

                    var dataArray = jsonObject["data"]?.ToObject<List<EditedMemberChange>>();
                    response.Data = dataArray;
                }
                else
                {
                    response.Status = "Failed";
                    response.Message = $"API Error: {result.StatusCode}";
                    response.Data = null;
                }
            }
            catch (Exception ex)
            {
                response.Status = "Failed";
                response.Message = ex.Message;
                response.Data = null;
            }

            return response;
            
        }
    }
}
