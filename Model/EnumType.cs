// <copyright company="Vermessungsamt Winterthur">
//      Author: Edgar Butwilowski
//      Copyright (c) Vermessungsamt Winterthur. All rights reserved.
// </copyright>
namespace roadwork_portal_service.Model;

public class EnumType
{
    public string code { get; set; } = "";
    public string name { get; set; } = "";

    // Minor Work - Marks if this enum is for normal needs/activities (value: false), small work (Kleinmassnahmen, value: true) or both (value: null) #695
    // Is never null for project types
    public bool? isMinorWork { get; set; }
}
