// <copyright company="Vermessungsamt Winterthur">
//      Author: Edgar Butwilowski
//      Copyright (c) Vermessungsamt Winterthur. All rights reserved.
// </copyright>
namespace roadwork_portal_service.Model;

public class Role
{
    public bool projectmanager { get; set; } = false;
    public bool eventmanager { get; set; } = false;
    public bool orderer { get; set; } = false;
    public bool trafficmanager { get; set; } = false;
    public bool territorymanager { get; set; } = false;
    public bool administrator { get; set; } = false;
    
}

