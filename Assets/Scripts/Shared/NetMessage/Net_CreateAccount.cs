[System.Serializable]
public class Net_CreateAccount : NetMsg
{
    public Net_CreateAccount()
    {
        OP = NetOP.CreateAccount;
    }

    public string Username { get; set; }
    public string Password { get; set; }
    public string Email { get; set; }
}
