// //From IConfiguration
// //This controller demonstrates how to read configuration values using the IConfiguration interface.

// using Microsoft.AspNetCore.Mvc;
// using Microsoft.Extensions.Configuration;

// [ApiController]
// [Route("api/[controller]")]
// public class PaymentController : ControllerBase
// {
//     private readonly IConfiguration _configuration; //declare

//     // IConfiguration is provided automatically by ASP.NET Core
//     public PaymentController(IConfiguration configuration)
//     {
//         _configuration = configuration;   //initialize 
//     }

//     [HttpGet("url")]
//     public IActionResult GetPaymentUrl()
//     {
//         // Use a colon (:) to read nested JSON values
//         string apiUrl = _configuration["ExternalServices:PaymentApiUrl"];
//         string apiKey = _configuration["ExternalServices:ApiKey"];
//         string timeOut = _configuration["ExternalServices:TimeoutSeconds"];
//         string merchantId = _configuration["ExternalServices:MerchantId"];
//         string demoMethod = "IConfiguration";
//         return Ok(new { MerchantId = merchantId, Method = demoMethod, PaymentUrl = apiUrl, ApiKey = apiKey, Timeout = timeOut });
//     }
// }



//From IOptions
// This controller demonstrates how to read 
//configuration values using the IOptions pattern.
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

[ApiController]
[Route("api/[controller]")]
public class PaymentController : ControllerBase
{
    // Store the strongly-typed options instead of IConfiguration
    private readonly ExternalServicesOptions _externalServices; //declare

    // Inject IOptions<T>
    public PaymentController(IOptions<ExternalServicesOptions> options)
    {
        // .Value extracts the actual data from the IOptions wrapper
        _externalServices = options.Value;
    }

    [HttpGet("url")]
    public IActionResult GetPaymentUrl()
    {
        // Now you get auto-completion and compile-time safety
        string apiUrl = _externalServices.PaymentApiUrl;
        string apiKey = _externalServices.ApiKey;
        string timeout = _externalServices.TimeoutSeconds.ToString();
        string merchantId = _externalServices.MerchantId;
        string demoMethod = "IOptions";
        string MerchantName = _externalServices.MerchantName;

        return Ok(new {
            MerchantName = MerchantName, MerchantId = merchantId, Method = demoMethod, PaymentUrl = apiUrl, ApiKey = apiKey, Timeout = timeout });
    }
}


