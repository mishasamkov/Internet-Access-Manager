namespace InternetAccessManager.Api.Models.Requests;

public class ChangeStatusRequest
{
    public string Status { get; set; } = string.Empty; // FullAccess, NoAccess, SafeMode
}

public class BulkChangeStatusRequest
{
    public List<int> Ids { get; set; } = new();
    public string Status { get; set; } = string.Empty;
}