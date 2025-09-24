// <copyright company="Vermessungsamt Winterthur">
//      Author: Edgar Butwilowski
//      Copyright (c) Vermessungsamt Winterthur. All rights reserved.
// </copyright>
namespace roadwork_portal_service.Model;

public class User
{
    public string uuid { get; set; } = "";
    public string mailAddress { get; set; } = "";
    public string passPhrase { get; set; } = "";
    public string lastName { get; set; } = "";
    public string firstName { get; set; } = "";
    public bool active { get; set; } = false;
    public bool prefTableView { get; set; } = false;
    public OrganisationalUnit organisationalUnit { get; set; } = new OrganisationalUnit();
    public DateTime? lastLoginAttempt { get; set; }
    public DateTime? databaseTime { get; set; }
    public Role grantedRoles { get; set; } = new Role();
    public string? chosenRole { get; set; }
    public string errorMessage { get; set; } = "";
    public bool isDistributionList { get; set; } = false;
    public bool isParticipantList { get; set; } = false;

    internal void setRole(string role)
    {
        string clearRole = role.Trim().ToLower();
        if (clearRole == "view") this.grantedRoles.view = true;
        else if (clearRole == "projectmanager") this.grantedRoles.projectmanager = true;
        else if (clearRole == "eventmanager") this.grantedRoles.eventmanager = true;
        else if (clearRole == "orderer") this.grantedRoles.orderer = true;
        else if (clearRole == "trafficmanager") this.grantedRoles.trafficmanager = true;
        else if (clearRole == "territorymanager") this.grantedRoles.territorymanager = true;
        else if (clearRole == "administrator") this.grantedRoles.administrator = true;
    }

    internal bool hasRole(string role)
    {
        string clearRole = role.Trim().ToLower();
        if (clearRole == "view") return this.grantedRoles.view;
        if (clearRole == "projectmanager") return this.grantedRoles.projectmanager;
        if (clearRole == "eventmanager") return this.grantedRoles.eventmanager;
        if (clearRole == "orderer") return this.grantedRoles.orderer;
        if (clearRole == "trafficmanager") return this.grantedRoles.trafficmanager;
        if (clearRole == "territorymanager") return this.grantedRoles.territorymanager;
        if (clearRole == "administrator") return this.grantedRoles.administrator;
        return false;
    }
}

