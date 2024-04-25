// <copyright company="Vermessungsamt Winterthur">
//      Author: Edgar Butwilowski
//      Copyright (c) Vermessungsamt Winterthur. All rights reserved.
// </copyright>
namespace roadwork_portal_service.Model;

public class ConsultationInput
{
    public string uuid { get; set; } = "";
    public DateTime? lastEdit { get; set; }
    public User? inputBy { get; set; }
    public string ordererFeedback { get; set; } = "";
    public string? managerFeedback { get; set; }
    public bool decline { get; set; } = false;
    public int valuation { get; set; } = 0;
    public string feedbackPhase { get; set; } = "";
    public string errorMessage { get; set; } = "";
}
