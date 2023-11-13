// <copyright company="Vermessungsamt Winterthur">
//      Author: Edgar Butwilowski
//      Copyright (c) Vermessungsamt Winterthur. All rights reserved.
// </copyright>
namespace roadwork_portal_service.Model;

public class ActivityHistoryItem
{
    public string uuid { get; set; } = "";
    public DateTime? changeDate { get; set; }
    public string who { get; set; } = "";
    public string what { get; set; } = "";
    public string userComment { get; set; } = "";
}
