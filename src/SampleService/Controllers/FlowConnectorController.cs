﻿using FlowTriggerManagingService;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SampleService.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class FlowConnectorController : ControllerBase
    {
        private readonly ITriggerManagingService _triggerService;

        public FlowConnectorController(ITriggerManagingService triggerService)
        {
            _triggerService = triggerService;
        }

        // GET /api/v1/flowconnector/hookIds
        [HttpGet("hookIds")]
        public IEnumerable<FlowTriggerDataContractBase> GetHookIds(string key)
        {
            var hooks = _triggerService.ListHooksByKey(key);
            return hooks.Select(h => new FlowTriggerDataContractBase() { HookId = h.HookId, HookName = h.HookName });
        }

        // GET /api/v1/flowconnector/hookId/{hookId}/Schema
        [HttpGet("hookId/{hookId}/Schema")]
        public async Task<object> GetSchemaAsync(string hookId, string key = null)
        {
            var properties = await _triggerService.GetPropertiesAsync(hookId);
            return GenerateJSONSchema(properties);
        }

        // POST /api/v1/flowconnector/hookId/{hookId}
        [HttpPost("hookId/{hookId}")]
        public async Task<IActionResult> PostAsync(string hookId, [FromBody] ConnectorRegisterParameters parameters, string key = null)
        {
            if (!Uri.TryCreate(parameters.CallbackUrl, UriKind.Absolute, out var callback))
            {
                throw new InvalidOperationException("Callback URI is invalid.");
            }
            await _triggerService.UpdateCallbackAsync(hookId, callback);

            var deleteUrl = GenerateDeleteUri(Request.Scheme, Request.Host.ToString(), hookId);

            // https://docs.microsoft.com/en-us/connectors/custom-connectors/create-webhook-trigger#the-openapi-definition
            Request.HttpContext.Response.Headers.Add("Location", deleteUrl);
            return Ok();
        }

        // Delete /api/v1/flowconnector/hookId/{hookId}
        [HttpDelete("hookId/{hookId}")]
        public async Task DeleteAsync(string hookId, string key = null)
        {
            await _triggerService.DeleteCallbackAsync(hookId);
        }

        private static string GenerateDeleteUri(string scheme, string host, string hookId)
        {
            return $"{scheme}://{host}/api/v1/flowconnector/hookId/{hookId}";
        }

        private static object GenerateJSONSchema(IEnumerable<string> properties)
        {
            if (properties != null && properties.Count() > 0)
            {
                dynamic propertiesObj = new JObject();

                foreach (var prop in properties)
                {
                    dynamic propObj = new JObject();
                    propObj.type = "string";
                    propObj.description = prop;
                    propertiesObj[prop] = propObj;
                }

                dynamic obj = new JObject();
                obj.type = "object";
                obj.properties = propertiesObj;
                return obj;
            }

            return null;
        }
    }
}
