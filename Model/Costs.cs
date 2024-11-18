// <copyright company="Geoinformation Winterthur">
//      Author: Edgar Butwilowski
//      Copyright (c) Geoinformation Winterthur. All rights reserved.
// </copyright>
namespace roadwork_portal_service.Model;

public class Costs
{
    public string? uuid { get; set; }
    public decimal? costs { get; set; }
    public string? workTitle { get; set; }
    public string? projectType { get; set; }
    public string? costsComment { get; set; }
    public string? errorMessage { get; set; }
}
