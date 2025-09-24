// <copyright company="Vermessungsamt Winterthur">
//      Author: Edgar Butwilowski
//      Copyright (c) Vermessungsamt Winterthur. All rights reserved.
// </copyright>
namespace roadwork_portal_service.Model;

public class ConfigurationData
{
    public int minAreaSize { get; set; } = 0;
    public int maxAreaSize { get; set; } = 0;

    public DateTime?[] plannedDatesSks { get; set; } = new DateTime?[0];
    public DateTime?[] plannedDatesKap { get; set; } = new DateTime?[0];
    public DateTime?[] plannedDatesOks { get; set; } = new DateTime?[0];

    public long?[] sksNos { get; set; } = new long?[0];

    public string errorMessage { get; set; } = "";
}