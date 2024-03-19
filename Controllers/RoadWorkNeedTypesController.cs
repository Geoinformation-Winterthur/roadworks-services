// <copyright company="Vermessungsamt Winterthur">
//      Author: Edgar Butwilowski
//      Copyright (c) Vermessungsamt Winterthur. All rights reserved.
// </copyright>
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using roadwork_portal_service.Model;
using roadwork_portal_service.Configuration;

namespace roadwork_portal_service.Controllers;

[ApiController]
[Route("[controller]")]
public class RoadWorkNeedTypesController : ControllerBase
{
    private readonly ILogger<RoadWorkNeedTypesController> _logger;

    public RoadWorkNeedTypesController(ILogger<RoadWorkNeedTypesController> logger)
    {
        _logger = logger;
    }

}

