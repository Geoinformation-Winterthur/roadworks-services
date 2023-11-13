// <copyright company="Vermessungsamt Winterthur">
//      Author: Edgar Butwilowski
//      Copyright (c) Vermessungsamt Winterthur. All rights reserved.
// </copyright>
namespace roadwork_portal_service.Model;

public class ConsultationInput
{
    public string uuid { get; set; } = "";
    public DateTime? lastEdit { get; set; }
    public string typeOfInput { get; set; } = "";
    public User? inputBy { get; set; }
    public bool decline { get; set; } = false;
    public string inputText { get; set; } = "";
    public string errorMessage { get; set; } = "";
}
