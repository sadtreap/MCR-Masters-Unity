// BaseResponse.cs
using System;

namespace MCRGame.Net
{
    [Serializable]
    public class BaseResponse
    {
        // 서버 JSON의 "message" 필드와 매핑됩니다.
        public string message;
    }
}
