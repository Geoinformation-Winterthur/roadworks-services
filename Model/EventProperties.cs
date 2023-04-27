// <copyright company="Vermessungsamt Winterthur">
//      Author: Edgar Butwilowski
//      Copyright (c) Vermessungsamt Winterthur. All rights reserved.
// </copyright>
namespace roadwork_portal_service.Model;

public class EventProperties
{
    public string uuid { get; set; } = "";
    public string name { get; set; } = "";
    public DateTime created { get; set; } = DateTime.MinValue;
    public DateTime lastModified { get; set; } = DateTime.MinValue;
    public DateTime dateFrom { get; set; } = DateTime.MinValue;
    public DateTime dateTo { get; set; } = DateTime.MinValue;
}

