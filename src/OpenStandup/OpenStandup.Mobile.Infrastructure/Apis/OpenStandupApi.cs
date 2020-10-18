﻿using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using OpenStandup.Core.Domain.Entities;
using OpenStandup.Core.Dto;
using OpenStandup.Core.Dto.Api;
using OpenStandup.Core.Interfaces;
using OpenStandup.Core.Interfaces.Apis;
using OpenStandup.SharedKernel;
using Newtonsoft.Json;


namespace OpenStandup.Mobile.Infrastructure.Apis
{
    public class OpenStandupApi : IOpenStandupApi
    {
        private readonly HttpClient _httpClient;
        private readonly IAppSettings _appSettings;
        private readonly IMapper _mapper;

        public OpenStandupApi(HttpClient httpClient, IAppSettings appSettings, IMapper mapper)
        {
            _httpClient = httpClient;
            _appSettings = appSettings;
            _mapper = mapper;
        }

        public async Task<HttpOperationResponse<string>> SaveProfile(GitHubUser gitHubUser)
        {
            var userDto = _mapper.Map<UserDto>(gitHubUser);

            var request = new HttpRequestMessage(HttpMethod.Post, $"{_appSettings.ApiEndpoint}/users")
            {
                Content = new StringContent(JsonConvert.SerializeObject(userDto), Encoding.UTF8, "application/json")
            };

            return new HttpOperationResponse<string>((await Policies.AttemptAndRetryPolicy(() => _httpClient.SendAsync(request)).ConfigureAwait(false)).StatusCode, null);
        }

        public async Task<HttpOperationResponse<AppConfigDto>> GetConfiguration()
        {
            var response = await Policies.AttemptAndRetryPolicy(() => _httpClient.GetAsync($"{_appSettings.ApiEndpoint}/configuration")).ConfigureAwait(false);

            return !response.IsSuccessStatusCode ?
                new HttpOperationResponse<AppConfigDto>(response.StatusCode, null) :
                new HttpOperationResponse<AppConfigDto>(response.StatusCode, JsonConvert.DeserializeObject<AppConfigDto>(await response.Content.ReadAsStringAsync()));
        }

        public async Task<HttpOperationResponse<string>> ValidateGitHubAccessToken(string token)
        {
            return new HttpOperationResponse<string>((await Policies.AttemptAndRetryPolicy(() => _httpClient.GetAsync($"{_appSettings.ApiEndpoint}/users/ValidateGitHubAccessToken?token={token}")).ConfigureAwait(false)).StatusCode, null);
        }
    }
}