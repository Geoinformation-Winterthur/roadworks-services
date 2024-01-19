// <copyright company="Vermessungsamt Winterthur">
//      Author: Edgar Butwilowski
//      Copyright (c) Vermessungsamt Winterthur. All rights reserved.
// </copyright>
namespace roadwork_portal_service.Model;

public class ConfigurationData
{
    public int minAreaSize { get; set; } = 0;
    public int maxAreaSize { get; set; } = 0;

    public DateTime? dateSks1 { get; set; }
    public DateTime? dateSks2 { get; set; }
    public DateTime? dateSks3 { get; set; }
    public DateTime? dateSks4 { get; set; }

    public DateTime? dateKap1 { get; set; }
    public DateTime? dateKap2 { get; set; }

    public DateTime? dateOks1 { get; set; }
    public DateTime? dateOks2 { get; set; }
    public DateTime? dateOks3 { get; set; }
    public DateTime? dateOks4 { get; set; }
    public DateTime? dateOks5 { get; set; }
    public DateTime? dateOks6 { get; set; }
    public DateTime? dateOks7 { get; set; }
    public DateTime? dateOks8 { get; set; }
    public DateTime? dateOks9 { get; set; }
    public DateTime? dateOks10 { get; set; }
    public DateTime? dateOks11 { get; set; }

    public string errorMessage { get; set; } = "";
}