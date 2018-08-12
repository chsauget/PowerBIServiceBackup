using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace PowerBIServiceBackup
{
    public static class Start
    {
        [FunctionName("HttpStart")]
        public static async Task<HttpResponseMessage> Run(
            [HttpTrigger(AuthorizationLevel.Function, methods: "get", Route = "orchestrators/{functionName}")] HttpRequestMessage req,
            [OrchestrationClient] DurableOrchestrationClientBase starter,
            string functionName,
            ILogger log)
        {
            // Function input comes from the request content.
            dynamic eventData = await req.Content.ReadAsAsync<object>();
            string instanceId = await starter.StartNewAsync(functionName, eventData);

            log.LogInformation($"Started orchestration with ID = '{instanceId}'.");

            var res = starter.CreateCheckStatusResponse(req, instanceId);
            res.Headers.RetryAfter = new RetryConditionHeaderValue(TimeSpan.FromSeconds(10));
            return res;
        }
        [FunctionName("ScheduledStart")]
        public static async Task RunScheduled(
        [TimerTrigger("0 00 19 * * *")] TimerInfo timerInfo,
        [OrchestrationClient] DurableOrchestrationClient starter,
        ILogger log)
        {
            string instanceId = await starter.StartNewAsync("RetrievePowerBIReports", null);
            log.LogInformation($"Started orchestration with ID = '{instanceId}'.");
        }
    }
}
