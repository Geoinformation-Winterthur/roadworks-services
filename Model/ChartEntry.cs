// <copyright company="Vermessungsamt Winterthur">
//      Author: Edgar Butwilowski
//      Copyright (c) Vermessungsamt Winterthur. All rights reserved.
// </copyright>
namespace roadwork_portal_service.Model;

public class ChartEntry
{
    public string label { get; set; } = "";
    public int? value { get; set; }
    public string errorMessage { get; set; } = "";
}
