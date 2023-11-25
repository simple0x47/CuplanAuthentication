namespace Cuplan.Authentication.ServiceModels;

public struct LoginErrorResponse
{
    public string error { get; set; }
    public string error_description { get; set; }
}