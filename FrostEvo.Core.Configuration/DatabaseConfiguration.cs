namespace FrostEvo.Core.Configurations;

public class DatabaseConfiguration
{
    public string Host { get; set; }
    public string Username { get; set; }
    public string Password { get; set; }
    public string PasswordCryptKey { get; set; }
    public string WorldSchema { get; set; }
    public string UserSchema { get; set; }
    public string WebSchema { get; set; }
    public string Extra { get; set; }
    public string LogSchema { get; set; }
}