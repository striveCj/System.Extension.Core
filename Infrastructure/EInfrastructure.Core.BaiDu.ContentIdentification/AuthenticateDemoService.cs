using System.Collections.Generic;
using System.Linq;
using System.Net;
using EInfrastructure.Core.HelpCommon;
using EInfrastructure.Core.HelpCommon.Files;
using EInfrastructure.Core.HelpCommon.Serialization;
using EInfrastructure.Core.Interface.ContentIdentification;
using EInfrastructure.Core.Interface.ContentIdentification.Dto;
using EInfrastructure.Core.Interface.ContentIdentification.Enum;
using EInfrastructure.Core.Interface.IOC;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using RestSharp;

namespace EInfrastructure.Core.BaiDu.ContentIdentification
{
    /// <summary>
    /// 鉴定服务
    /// </summary>
    public class AuthenticateDemoService : IAuthenticateDemoService, IPerRequest
    {
        private readonly JsonCommon _jsonCommon;
        private readonly RestClient _restClient;
        private readonly RestRequest _request;

        public AuthenticateDemoService()
        {
            _jsonCommon = new JsonCommon();
            _restClient = new RestClient("http://ai.baidu.com");
            _request =
                new RestRequest(
                    $"aidemo",
                    Method.POST) {RequestFormat = RestSharp.DataFormat.Json};
            _request.AddHeader("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/71.0.3578.98 Safari/537.36");
            _request.AddHeader("Content-Type", "application/x-www-form-urlencoded");
            _request.AddHeader("Origin", "http://ai.baidu.com");
            _request.AddHeader("Host", "ai.baidu.com");
            _request.AddHeader("Referer", "http://ai.baidu.com/tech/imagecensoring");
            _request.AddParameter("type", "user_defined");
        }

        #region 鉴定图片信息

        #region 鉴定图片信息（根据图片地址）

        /// <summary>
        /// 鉴定图片信息（根据图片地址）
        /// </summary>
        /// <param name="url">图片地址</param>
        /// <param name="webProxy">代理信息</param>
        /// <returns></returns>
        public ContentInfoDto ImgAuthenticateByUrl(string url, WebProxy webProxy = null)
        {
            url.IsNullOrEmptyTip("图片地址不能为空");
            SetProxy(webProxy);
            _request.AddParameter("image_url", url);
            _request.AddParameter("image", "");
            var response = _restClient.Execute(_request);
            var result = response.Content;
            return GetResponse(result);
        }

        #endregion

        #region 鉴定图片信息（根据图片base64,带data:image/jpeg;base64,）

        /// <summary>
        /// 鉴定图片信息（根据图片base64,带data:image/jpeg;base64,）
        /// </summary>
        /// <param name="base64">图片base64</param>
        /// <param name="webProxy">代理信息</param>
        /// <returns></returns>
        public ContentInfoDto ImgAuthenticateByBase64(string base64, WebProxy webProxy = null)
        {
            base64.IsNullOrEmptyTip("图片信息有误");
            SetProxy(webProxy);
            _request.AddParameter("image", base64);
            _request.AddParameter("image_url", "");
            var response = _restClient.Execute(_request);
            var result = response.Content;
            return GetResponse(result);
        }

        #endregion

        #region 鉴定图片信息（根据图片文件信息）

        /// <summary>
        /// 鉴定图片信息（根据图片文件信息）
        /// </summary>
        /// <param name="formFile">文件信息</param>
        /// <param name="webProxy">代理信息</param>
        /// <returns></returns>
        public ContentInfoDto ImgAuthenticateByFile(IFormFile formFile, WebProxy webProxy = null)
        {
            return ImgAuthenticateByBase64(ImageCommon.GetBase64(formFile), webProxy);
        }

        #endregion

        #endregion

        #region private methods

        #region 得到响应信息

        /// <summary>
        /// 得到响应信息
        /// </summary>
        /// <param name="response">响应信息</param>
        /// <returns></returns>
        private ContentInfoDto GetResponse(string response)
        {
            if (string.IsNullOrEmpty(response))
            {
                return new ContentInfoDto()
                {
                    Success = false,
                    Msg = "lose"
                };
            }

            var data = _jsonCommon.Deserialize<dynamic>(response);
            if (data == null || data["errno"] != 0)
            {
                return new ContentInfoDto()
                {
                    Success = false,
                    Msg = "lose"
                };
            }

            ContentInfoDto contentInfo = new ContentInfoDto()
            {
                Msg = data["data"]["conclusion"],
                Success = true,
                Data = new List<ContentInfoDto.DataDto>()
            };
            if (data["data"]["conclusionType"] == 1)
            {
                contentInfo.Data.Add(new ContentInfoDto.DataDto()
                {
                    Msg = "合规",
                    Rating = ContentRatingEnum.Normal,
                    SubRating = SubContentRatingEnum.Normal
                });
            }
            else if (data["data"]["conclusionType"] == 2)
            {
                List<ImageResponse> responseList = _jsonCommon.Deserialize<List<ImageResponse>>(data["data"]);
                foreach (var item in responseList)
                {
                    if (item.Star != null)
                    {
                        contentInfo.Data.Add(new ContentInfoDto.DataDto()
                        {
                            Msg = item.Msg,
                            Probability = item.Probability,
                            Rating = GetRating(item.Type),
                            Star = item.Star.Select(x => new ContentInfoDto.PersonDto()
                            {
                                Name = x.Name,
                                Probability = x.Probability
                            }).ToList()
                        });
                    }
                }
            }

            return null;
        }

        #endregion

        #region 得到分级

        #region 得到分级

        /// <summary>
        /// 得到分级
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        private ContentRatingEnum GetRating(int type)
        {
            switch (type)
            {
                case 1:
                    return ContentRatingEnum.Porn;
                case 2:
                    return ContentRatingEnum.Sexy;
                case 3:
                    return ContentRatingEnum.Violence;
                case 4:
                    return ContentRatingEnum.Nausea;
                case 5:
                    //水印
                    return ContentRatingEnum.Normal;
                case 6:
                    //二维码
                    return ContentRatingEnum.Normal;
                case 7:
                    //条形码
                    return ContentRatingEnum.Normal;
                case 8:
                    return ContentRatingEnum.Politically;
                case 9:
                    return ContentRatingEnum.SensitiveWords;
                case 10:
                    return ContentRatingEnum.CustomerSensitiveWords;
                default:
                    return ContentRatingEnum.Normal;
            }
        }

        #endregion

        #region 得到详细分级

        /// <summary>
        /// 得到详细分级
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        private SubContentRatingEnum GetSubRating(int type)
        {
            return SubContentRatingEnum.Normal;
        }

        #endregion

        #endregion

        #region 设置代理信息

        /// <summary>
        /// 设置代理信息
        /// </summary>
        /// <param name="webProxy">代理信息</param>
        private void SetProxy(WebProxy webProxy)
        {
            if (webProxy != null)
            {
                _restClient.Proxy = webProxy;
            }
        }

        #endregion

        #endregion
    }

    /// <summary>
    /// 图片响应信息
    /// </summary>
    internal class ImageResponse
    {
        /// <summary>
        /// 提示信息
        /// </summary>
        [JsonProperty(PropertyName = "msg")]
        public string Msg { get; set; }

        /// <summary>
        /// 类型
        /// </summary>
        [JsonProperty(PropertyName = "type")]
        public int Type { get; set; }

        /// <summary>
        /// 相似度
        /// </summary>
        [JsonProperty(PropertyName = "probability", NullValueHandling = NullValueHandling.Ignore)]
        public decimal? Probability { get; set; }

        /// <summary>
        /// 人物信息
        /// </summary>
        [JsonProperty(PropertyName = "star", NullValueHandling = NullValueHandling.Ignore)]
        public List<StarResponse> Star { get; set; } = null;

        /// <summary>
        /// 关注
        /// </summary>
        public class StarResponse
        {
            /// <summary>
            /// 相似度
            /// </summary>
            [JsonProperty(PropertyName = "probability", NullValueHandling = NullValueHandling.Ignore)]
            public decimal? Probability { get; set; }

            /// <summary>
            /// 姓名
            /// </summary>
            [JsonProperty(PropertyName = "name")]
            public string Name { get; set; }
        }
    }
}