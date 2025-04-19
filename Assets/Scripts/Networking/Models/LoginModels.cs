namespace MCRGame.Net
{
    [System.Serializable]
    public class AuthUrlResponse
    {
        public string auth_url;
        public string session_id;
    }

    [System.Serializable]
    public class TokenResponse
    {
        public string access_token;
        public string refresh_token;
        public bool is_new_user;
        public string token_type;
    }

}