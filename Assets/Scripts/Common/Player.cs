using System;
using MCRGame.Net;
using Newtonsoft.Json;

namespace MCRGame.Common
{
    [Serializable]
    public class Player
    {
        [JsonProperty("uid")]
        public string Uid { get; set; }

        [JsonProperty("nickname")]
        public string Nickname { get; set; }

        [JsonProperty("index")]
        public int Index { get; set; }

        [JsonProperty("score")]
        public int Score { get; set; }
        [JsonProperty("current_character")]
        public CharacterResponse CurrentCharacter { get; set; }

        public Player() { }

        public Player(string uid, string nickname, int index, int score)
        {
            Uid = uid;
            Nickname = nickname;
            Index = index;
            Score = score;
        }

        public override string ToString()
        {
            return $"Player [Uid: {Uid}, Nickname: {Nickname}, Index: {Index}, Score: {Score}]";
        }
    }
}
