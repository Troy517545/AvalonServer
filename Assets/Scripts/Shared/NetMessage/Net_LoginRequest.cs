[System.Serializable]
public class Net_LoginRequest : NetMsg
{
    public Net_LoginRequest()
    {
        OP = NetOP.LoginRequest;
    }

    public string UsernameOrEmail { get; set; }
    public string Password { get; set; }
}
