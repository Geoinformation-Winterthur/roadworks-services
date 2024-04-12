// <copyright company="Vermessungsamt Winterthur">
//      Author: Edgar Butwilowski
//      Copyright (c) Vermessungsamt Winterthur. All rights reserved.
// </copyright>
namespace roadwork_portal_service.Model;

public class OrganisationalUnit
{
    public string uuid { get; set; } = "";
    public string name { get; set; } = "";
    public string abbreviation { get; set; } = "";
    public string contactPerson { get; set; } = "";
    public bool isCivilEngineering { get; set; } = false;
    public string errorMessage { get; set; } = "";
}

