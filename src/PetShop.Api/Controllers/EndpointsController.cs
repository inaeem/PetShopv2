using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using PetShop.Api.Common;
using PetShop.Service.DTOs;
using PetShop.Service.External;

namespace PetShop.Api.Controllers;

/// <summary>
/// Server-renders the configured FHIR endpoints (<c>PetSync:Endpoints</c>) through the
/// shared _Layout master template.
/// </summary>
/// <remarks>
/// Anonymous by design: the API authenticates with JWT bearer tokens, and a browser
/// navigating to a page cannot attach an Authorization header. Serving this page to
/// authenticated users only would require adding a cookie authentication scheme.
/// </remarks>
[AllowAnonymous]
[Route("endpoints")]
[OutputCache(PolicyName = OutputCachePolicies.ConfiguredEndpoints)]
public class EndpointsController : Controller
{
    private readonly IFhirEndpointResolver _endpoints;

    public EndpointsController(IFhirEndpointResolver endpoints) => _endpoints = endpoints;

    /// <summary>
    /// Renders the four-column endpoints table. The rows come from the startup
    /// configuration snapshot and the rendered HTML is held by the output cache, so the
    /// view is executed once rather than on every request.
    /// </summary>
    [HttpGet("")]
    public IActionResult Index()
    {
        var rows = _endpoints.GetAll()
            .Select(e => new FhirEndpointRowDto(e.Name, e.MetaDataUrl, e.SwaggerUrl, e.Url))
            .ToList();

        return View(rows);
    }
}
