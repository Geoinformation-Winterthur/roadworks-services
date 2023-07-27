// <copyright company="Geoinformation Winterthur">
//      Author: Edgar Butwilowski
//      Copyright (c) Geoinformation Winterthur. All rights reserved.
// </copyright>
namespace roadwork_portal_service.Model;

public class ManagementArea
{
    public string uuid { get; set; } = "";
    public User manager { get; set; } = new User();
    public User substituteManager { get; set; } = new User();
    public string errorMessage { get; set; } = "";
}
