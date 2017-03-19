using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;

namespace AwsDotnetCsharp
{
    public class Handler
    {
        [LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]
        public async Task<APIGatewayProxyResponse> Hello(APIGatewayProxyRequest request, ILambdaContext context)
        {
            await Task.Delay(0);

            var response = new APIGatewayProxyResponse
            {
                StatusCode = (int)HttpStatusCode.OK,
                Body = "{}",
                Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
            };
            return response;
        }
    }
}
