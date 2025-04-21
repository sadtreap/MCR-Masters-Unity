using System;
using Newtonsoft.Json;

namespace MCRGame.Net
{
    [Serializable]
    public class AvailableRoomResponse
    {
        [JsonProperty("name")]
        public string name;

        [JsonProperty("room_number")]
        public int room_number;

        [JsonProperty("max_users")]
        public int max_users;

        [JsonProperty("current_users")]
        public int current_users;

        [JsonProperty("host_uid")]
        public string host_uid;

        [JsonProperty("host_nickname")]
        public string host_nickname;

        [JsonProperty("users")]
        public RoomUserData[] users;
    }

    [Serializable]
    public class AvailableRoomResponseList
    {
        [JsonProperty("rooms")]
        public AvailableRoomResponse[] rooms;
    }

    [Serializable]
    public class RoomResponse
    {
        [JsonProperty("name")]
        public string name;

        [JsonProperty("room_number")]
        public int room_number;

        [JsonProperty("slot_index")]
        public int slot_index;
    }

    [Serializable]
    public class CreateRoomResponse
    {
        [JsonProperty("name")]
        public string name;

        [JsonProperty("room_number")]
        public int room_number;

        [JsonProperty("slot_index")]
        public int slot_index;
    }

    [Serializable]
    public class RoomData
    {
        [JsonProperty("roomId")]
        public string roomId;

        [JsonProperty("roomTitle")]
        public string roomTitle;

        [JsonProperty("roomInfo")]
        public string roomInfo;
    }

    [Serializable]
    public class RoomUserData
    {
        
        [JsonProperty("nickname")]
        public string nickname;
        [JsonProperty("uid")]
        public string uid;

        [JsonProperty("is_ready")]
        public bool isReady;

        [JsonProperty("slot_index")]
        public int slot_index;

        
    }

    [Serializable]
    public class RoomUsersResponse
    {
        [JsonProperty("host_uid")]
        public string host_uid;

        [JsonProperty("users")]
        public RoomUserData[] users;
    }
}