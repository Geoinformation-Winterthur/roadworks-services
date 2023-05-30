// <copyright company="Vermessungsamt Winterthur">
//      Author: Edgar Butwilowski
//      Copyright (c) Vermessungsamt Winterthur. All rights reserved.
// </copyright>
namespace roadwork_portal_service.Model;

public class ManagementAreaProperties
{
    public string uuid { get; set; } = "";
    public User manager { get; set; } = new User();
    public User substituteManager { get; set; } = new User();
}

