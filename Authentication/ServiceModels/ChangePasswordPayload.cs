namespace Cuplan.Authentication.ServiceModels;

public struct ChangePasswordPayload
{
    public string client_id { get; set; }
    public string email { get; set; }
    public string connection { get; set; }
}