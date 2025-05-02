using System;
using Newtonsoft.Json;

namespace MCRGame.Net
{
    [Serializable]
    public class RoomInfo
    {
        [JsonProperty("name")] public string name;
        [JsonProperty("room_number")] public int room_number;
        [JsonProperty("current_users")] public int current_users;
        [JsonProperty("max_users")] public int max_users;
        [JsonProperty("host_uid")] public string host_uid;
        [JsonProperty("host_nickname")] public string host_nickname;
    }

    [Serializable]
    public class RoomJoinedInfo
    {
        [JsonProperty("name")] public string name;
        [JsonProperty("room_number")] public int room_number;
        [JsonProperty("slot_index")] public int slot_index;
    }

    [Serializable]
    public class RoomUserInfo
    {
        [JsonProperty("nickname")] public string nickname;
        [JsonProperty("user_uid")] public string uid;
        [JsonProperty("is_ready")] public bool is_ready;
        [JsonProperty("slot_index")] public int slot_index;
        [JsonProperty("current_character")] public CharacterResponse current_character;
    }

    [Serializable]
    public class RoomUsersResponse
    {
        [JsonProperty("host_uid")] public string host_uid;
        [JsonProperty("users")] public RoomUserInfo[] users;
        
    }
}